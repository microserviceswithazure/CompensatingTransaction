namespace Workflow
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using System.Transactions;
    using Microsoft.ServiceBus.Messaging;
    using Newtonsoft.Json;

    static class TravelBookingHandlers
    {
        const string ContentTypeApplicationJson = "application/json";
        const string TravelBookingLabel = "TravelBooking";

        public static async Task BookFlight(BrokeredMessage message, MessageSender nextStepQueue, MessageSender compensatorQueue)
        {
            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var via = (message.Properties.ContainsKey("Via")
                        ? ((string)message.Properties["Via"] + ",")
                        : string.Empty) +
                              "bookflight";

                    if (message.Label != null &&
                        message.ContentType != null &&
                        message.Label.Equals(TravelBookingLabel, StringComparison.InvariantCultureIgnoreCase) &&
                        message.ContentType.Equals(ContentTypeApplicationJson, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var body = message.GetBody<Stream>();
                        var travelBooking = DeserializeTravelBooking(body);

                        lock (Console.Out)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("Booking Flight");
                            Console.ResetColor();
                        }

                        // now we're going to simulate the work of booking a flight,
                        // which usually involves a call to a third party
                        if (travelBooking.CreditLimit < 200)
                        {
                            await message.DeadLetterAsync(
                                new Dictionary<string, object>
                                {
                                        {"DeadLetterReason", "TransactionError"},
                                        {"DeadLetterErrorDescription", "Failed to perform flight reservation due to insufficient funds"},
                                        {"Via", via}
                                });
                        }
                        else
                        {
                            // let's pretend we booked something
                            travelBooking.FlightReservationId = "A1B2C3";
                            travelBooking.CreditLimit -= 200;
                            await nextStepQueue.SendAsync(CreateForwardMessage(message, travelBooking, via));
                            // done with this job
                            await message.CompleteAsync();
                        }

                    }
                    else
                    {
                        await message.DeadLetterAsync(
                           new Dictionary<string, object>
                                    {
                                        {"DeadLetterReason", "BadMessage"},
                                        {"DeadLetterErrorDescription", "Unrecognized input message"},
                                        {"Via", via}
                                    });
                    }
                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                await message.AbandonAsync();
            }
        }

        public static async Task BookHotel(BrokeredMessage message, MessageSender nextStepQueue, MessageSender compensatorQueue)
        {
            try
            {
                var via = (message.Properties.ContainsKey("Via")
                    ? ((string)message.Properties["Via"] + ",")
                    : string.Empty) +
                          "bookhotel";

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (message.Label != null &&
                        message.ContentType != null &&
                        message.Label.Equals(TravelBookingLabel, StringComparison.InvariantCultureIgnoreCase) &&
                        message.ContentType.Equals(ContentTypeApplicationJson, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var body = message.GetBody<Stream>();
                        var travelBooking = DeserializeTravelBooking(body);
                        lock (Console.Out)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("Booking Hotel");
                            Console.ResetColor();
                        }

                        // If credit limit is low. Fail op.
                        if (travelBooking.CreditLimit < 100)
                        {
                            await message.DeadLetterAsync(
                               new Dictionary<string, object>
                                {
                                        {"DeadLetterReason", "TransactionError"},
                                        {"DeadLetterErrorDescription", "Failed to perform hotel reservation due to insufficient funds"},
                                        {"Via", via}
                                });
                        }
                        else
                        {
                            // let's pretend we booked something
                            travelBooking.HotelReservationId = "5676891234321";
                            travelBooking.CreditLimit -= 100;
                            await nextStepQueue.SendAsync(CreateForwardMessage(message, travelBooking, via));
                            // done with this job
                            await message.CompleteAsync();
                        }
                    }
                    else
                    {
                        await message.DeadLetterAsync(
                            new Dictionary<string, object>
                                    {
                                        {"DeadLetterReason", "BadMessage"},
                                        {"DeadLetterErrorDescription", "Unrecognized input message"},
                                        {"Via", via}
                                    });
                    }
                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                await message.AbandonAsync();
            }
        }

        public static async Task CancelFlight(BrokeredMessage message, MessageSender nextStepQueue, MessageSender compensatorQueue)
        {
            try
            {
                var via = (message.Properties.ContainsKey("Via")
                    ? ((string)message.Properties["Via"] + ",")
                    : string.Empty) +
                          "cancelflight";

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (message.Label != null &&
                        message.ContentType != null &&
                        message.Label.Equals(TravelBookingLabel, StringComparison.InvariantCultureIgnoreCase) &&
                        message.ContentType.Equals(ContentTypeApplicationJson, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var body = message.GetBody<Stream>();
                        Booking travelBooking = DeserializeTravelBooking(body);

                        // do we want to book a flight? No? Let's just forward the message to
                        // the next destination via transfer queue
                        if (!string.IsNullOrEmpty(travelBooking.FlightReservationId))
                        {
                            lock (Console.Out)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Cancelling Flight");
                                Console.ResetColor();
                            }

                            // reset the id
                            travelBooking.FlightReservationId = string.Empty;
                            travelBooking.CreditLimit += 200;
                        }

                        // forward
                        await nextStepQueue.SendAsync(CreateForwardMessage(message, travelBooking, via));

                        // done with this job
                        await message.CompleteAsync();
                    }
                    else
                    {
                        await message.DeadLetterAsync(
                            new Dictionary<string, object>
                                    {
                                        {"DeadLetterReason", "BadMessage"},
                                        {"DeadLetterErrorDescription", "Unrecognized input message"},
                                        {"Via", via}
                                    });
                    }
                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                await message.AbandonAsync();
            }
        }

        public static async Task CancelHotel(BrokeredMessage message, MessageSender nextStepQueue, MessageSender compensatorQueue)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cancelling Hotel");
                Console.ResetColor();
            }

            try
            {
                var via = (message.Properties.ContainsKey("Via")
                    ? ((string)message.Properties["Via"] + ",")
                    : string.Empty) +
                          "cancelhotel";

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (message.Label != null &&
                        message.ContentType != null &&
                        message.Label.Equals(TravelBookingLabel, StringComparison.InvariantCultureIgnoreCase) &&
                        message.ContentType.Equals(ContentTypeApplicationJson, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var body = message.GetBody<Stream>();
                        var travelBooking = DeserializeTravelBooking(body);

                        if (!string.IsNullOrEmpty(travelBooking.HotelReservationId))
                        {

                            travelBooking.HotelReservationId = string.Empty;
                            travelBooking.CreditLimit += 100;

                        }
                        await nextStepQueue.SendAsync(CreateForwardMessage(message, travelBooking, via));

                        // done with this job
                        await message.CompleteAsync();
                    }
                    else
                    {
                        await message.DeadLetterAsync(
                            new Dictionary<string, object>
                                    {
                                        {"DeadLetterReason", "BadMessage"},
                                        {"DeadLetterErrorDescription", "Unrecognized input message"},
                                        {"Via", via}
                                    });
                    }
                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                await message.AbandonAsync();
            }
        }

        static BrokeredMessage CreateForwardMessage(BrokeredMessage message, dynamic travelBooking, string via)
        {
            var brokeredMessage = new BrokeredMessage(SerializeTravelBooking(travelBooking))
            {
                ContentType = ContentTypeApplicationJson,
                Label = message.Label,
                TimeToLive = message.ExpiresAtUtc - DateTime.UtcNow
            };
            foreach (var prop in message.Properties)
            {
                brokeredMessage.Properties[prop.Key] = prop.Value;
            }
            brokeredMessage.Properties["Via"] = via;
            return brokeredMessage;
        }

        static MemoryStream SerializeTravelBooking(dynamic travelBooking)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(travelBooking)));
        }

        static Booking DeserializeTravelBooking(Stream body)
        {
            return JsonConvert.DeserializeObject<Booking>(new StreamReader(body, true).ReadToEnd());
        }
    }
}