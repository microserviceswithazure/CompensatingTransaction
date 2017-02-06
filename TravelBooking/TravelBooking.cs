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
        Host host;
        private async Task BookIt()
        {

            host = new Host();
            var cache = new CacheLogger();

            cache.DeleteKey("logs");
            cache.DeleteKey("walletBalance");
            await host.Run(namespaceAddress, manageKeyName, manageKey);
            var booking = new Booking
            {
                TravellerName = "rahul",
                Destination = "delhi",
                CreditLimit = 200
            };
            cache.WriteLog("walletBalance", booking.CreditLimit);
            cache.WriteLog("logs", new List<string> { $"Booking process initiated for traveler {booking.TravellerName}" });
            await host.BookTravel(booking);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var wallet = cache.ReadLog<string>("walletBalance");
            var log = cache.ReadLog<List<string>>("logs");
            textBox1.Text = "";
            if (log != null)
            {
                foreach (var logentry in log)
                {
                    textBox1.Text += logentry;
                    textBox1.Text += Environment.NewLine;
                    wallet.ToString();
                    textBox1.Text += Environment.NewLine;
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await host.DeleteQueues();
        }
    }
}
