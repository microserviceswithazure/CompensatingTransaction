using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Threading;
using Workflow;

namespace TravelBooking
{
    public partial class TravelBooking : Form
    {
        private static string namespaceAddress = ConfigurationManager.AppSettings[nameof(namespaceAddress)];
        private static string manageKeyName = ConfigurationManager.AppSettings[nameof(manageKeyName)];
        private static string manageKey = ConfigurationManager.AppSettings[nameof(manageKey)];
        private CacheLogger cache;

        public TravelBooking()
        {
            InitializeComponent();
        }

        private void TravelBooking_Load(object sender, EventArgs e)
        {
            cache = new CacheLogger();            
            BookIt();
            timer1.Enabled = true;
        }

        private async Task BookIt()
        {
            var host = new Host();

            cache.DeleteKey("logs");
            cache.DeleteKey("walletBalance");
            cache.WriteLog("logs", new List<string> { $"Started applciation" });
            

            await host.Run(namespaceAddress, manageKeyName, manageKey);
            var booking = new Booking
            {
                TravellerName = "Bla",
                Destination = "New york",
                CreditLimit = 100M
            };
            cache.WriteLog("walletBalance", booking.CreditLimit);
            cache.WriteLog("logs", new List<string> { $"Booking process initiated for traveler {booking.TravellerName}" });
            await host.BookTravel(booking);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var wallet = cache.ReadLog<string>("walletBalance");
            var log = cache.ReadLog <List<string>>("logs");
            textBox1.Text = "";
            foreach (var logentry in log)
            {
                textBox1.Text += logentry;
                textBox1.Text += Environment.NewLine;
                wallet.ToString();
                textBox1.Text += Environment.NewLine;
            }
        }
    }
}
