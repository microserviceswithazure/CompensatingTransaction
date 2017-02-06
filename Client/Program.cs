using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    using System.Configuration;
    using System.Threading;

    using Nito.AsyncEx;

    using Workflow;

    class Program
    {
        private static string namespaceAddress = ConfigurationManager.AppSettings[nameof(namespaceAddress)];
        private static string manageKeyName = ConfigurationManager.AppSettings[nameof(manageKeyName)];
        private static string manageKey = ConfigurationManager.AppSettings[nameof(manageKey)];

        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
            Console.ReadKey();
        }

        private static async Task MainAsync(string[] args)
        {
            var host = new Host();
            var cache = new CacheLogger();

            cache.DeleteKey("logs");
            cache.DeleteKey("walletBalance");
            await host.Run(namespaceAddress, manageKeyName, manageKey);
            var booking = new Booking
                {
                    TravellerName = ReadInputForMessage("Enter traveler's name:"),
                    Destination = ReadInputForMessage("Enter destination:"),
                    CreditLimit = Convert.ToDecimal(ReadInputForMessage("Enter credit limit:"))
                };
            cache.WriteLog("walletBalance", booking.CreditLimit);
            cache.WriteLog("logs", new List<string> { $"Booking process initiated for traveler {booking.TravellerName}" });
            await host.BookTravel(booking);
            Console.WriteLine("Clearing workflow. Please wait.");
            await host.DeleteQueues();
            Console.WriteLine("Booking completed. Press any key to exit.");
        }

        private static string ReadInputForMessage(string command)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(command);
            Console.ResetColor();
            return Console.ReadLine();
        }
    }
}
