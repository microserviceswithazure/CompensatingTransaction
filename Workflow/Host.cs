using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow
{
    using System.IO;
    using System.Threading;

    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    using Newtonsoft.Json;

    public class Host
    {
        const string SagaQueuePathPrefix = "sagas/1";
        const string BookHotelQueueName = SagaQueuePathPrefix + "/Bh";
        const string CancelHotelQueueName = SagaQueuePathPrefix + "/Ch";
        const string BookFlightQueueName = SagaQueuePathPrefix + "/Bf";
        const string CancelFlightQueueName = SagaQueuePathPrefix + "/Cf";
        const string SagaResultQueueName = SagaQueuePathPrefix + "/result";
        const string SagaInputQueueName = SagaQueuePathPrefix + "/input";
        static int pendingTransactions;

        private IEnumerable<QueueDescription> queues;

        private NamespaceManager namespaceManager;

        private MessagingFactory senderMessagingFactory;

        public async Task Run(
            string namespaceAddress,
            string manageKeyName,
            string manageKey)
        {
            this.namespaceManager = new NamespaceManager(
               namespaceAddress,
               TokenProvider.CreateSharedAccessSignatureTokenProvider(manageKeyName, manageKey));

            this.queues = await this.SetupSagaTopologyAsync(namespaceManager);
            this.senderMessagingFactory = await MessagingFactory.CreateAsync(
               namespaceAddress,
               TokenProvider.CreateSharedAccessSignatureTokenProvider(manageKeyName, manageKey));
        }

        async Task<IEnumerable<QueueDescription>> SetupSagaTopologyAsync(NamespaceManager nm)
        {
            return new List<QueueDescription>
            {
                await nm.QueueExistsAsync(SagaResultQueueName)
                    ? await nm.GetQueueAsync(SagaResultQueueName)
                    : await nm.CreateQueueAsync(SagaResultQueueName),
                await nm.QueueExistsAsync(CancelFlightQueueName)
                    ? await nm.GetQueueAsync(CancelFlightQueueName)
                    : await nm.CreateQueueAsync(new QueueDescription(CancelFlightQueueName)),
                await nm.QueueExistsAsync(BookFlightQueueName)
                    ? await nm.GetQueueAsync(BookFlightQueueName)
                    : await nm.CreateQueueAsync(
                        new QueueDescription(BookFlightQueueName)
                        {
                            // on failure, we move deadletter messages off to the flight 
                            // booking compensator's queue
                            EnableDeadLetteringOnMessageExpiration = true,
                            ForwardDeadLetteredMessagesTo = CancelFlightQueueName
                        }),
                await nm.QueueExistsAsync(CancelHotelQueueName)
                    ? await nm.GetQueueAsync(CancelHotelQueueName)
                    : await nm.CreateQueueAsync(new QueueDescription(CancelHotelQueueName)),
                await nm.QueueExistsAsync(BookHotelQueueName)
                    ? await nm.GetQueueAsync(BookHotelQueueName)
                    : await nm.CreateQueueAsync(
                        new QueueDescription(BookHotelQueueName)
                        {
                            // on failure, we move deadletter messages off to the hotel 
                            // booking compensator's queue
                            EnableDeadLetteringOnMessageExpiration = true,
                            ForwardDeadLetteredMessagesTo = CancelHotelQueueName
                        }),

                await nm.QueueExistsAsync(SagaInputQueueName)
                    ? await nm.GetQueueAsync(SagaInputQueueName)
                    : await nm.CreateQueueAsync(
                        new QueueDescription(SagaInputQueueName)
                        {
                            ForwardTo = BookHotelQueueName
                        })
            };
        }

        static SagaTaskManager RunSaga(MessagingFactory workersMessageFactory, CancellationTokenSource terminator)
        {
            var saga = new SagaTaskManager(workersMessageFactory, terminator.Token)
            {
                {BookHotelQueueName, TravelBookingHandlers.BookHotel, BookFlightQueueName, CancelHotelQueueName},
                {CancelHotelQueueName, TravelBookingHandlers.CancelHotel, SagaResultQueueName, string.Empty},
                {BookFlightQueueName, TravelBookingHandlers.BookFlight, SagaResultQueueName, CancelFlightQueueName},
                {CancelFlightQueueName, TravelBookingHandlers.CancelFlight, CancelHotelQueueName, string.Empty}
            };
            return saga;
        }

        public async Task BookTravel(Booking booking)
        {
            var sender = await senderMessagingFactory.CreateMessageSenderAsync(SagaInputQueueName);
            var sagaTerminator = new CancellationTokenSource();
            var saga = RunSaga(senderMessagingFactory, sagaTerminator);

            var message = new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(booking))))
            {
                ContentType = "application/json",
                Label = "TravelBooking",
                TimeToLive = TimeSpan.FromMinutes(15)
            };
            await sender.SendAsync(message);
            Console.WriteLine("Sending booking message. Press any key after the workflow completes.");
            //var iter = saga.GetEnumerator();
            //while (iter.MoveNext())
            //{
            //    var task = (Task)iter.Current;
            //    task.RunSynchronously();
            //    task.Wait();
            //}

            ////Console.ReadKey();
            //sagaTerminator.Cancel();
            //await saga.Task;
        }

        public async Task DeleteQueues()
        {
            if (queues != null)
            {
                foreach (var queueDescription in queues.Reverse())
                {
                    await this.namespaceManager.DeleteQueueAsync(queueDescription.Path);
                }
            }
        }
    }
}
