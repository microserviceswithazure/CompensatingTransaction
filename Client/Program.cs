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
            await host.Run(namespaceAddress, manageKeyName, manageKey);
            await host.BookTravel(new Booking() { CreditLimit = 250, TravellerName = "X" });

        }
    }
}
