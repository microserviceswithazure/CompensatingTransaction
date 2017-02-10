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
        private bool bookingon = false;
        private decimal valBal = 0;

        public TravelBooking()
        {
            InitializeComponent();
        }

        private void TravelBooking_Load(object sender, EventArgs e)
        {
            cache = new CacheLogger();
            timer1.Enabled = true;
            pic1.Visible = false;
            pic2.Visible = false;
            pic3.Visible = false;
        }
        Host host;

        private void timer1_Tick(object sender, EventArgs e)
        {
            var wallet = cache.ReadLog<string>("walletBalance");
            var log = cache.ReadLog<List<string>>("logs");
            txtLog.Text = "";

            if(log!=null && log.Count>0)
            {
                pic01.Visible = true;
                if (log.Count > 1)
                {
                    path1.Visible = true;
                    if (log.Count > 2)
                    {
                        pic02.Visible = true;
                        if (log.Count > 3)
                        {
                            if (log[3].Contains("cancellation"))
                            {
                                pic02.Visible = false;
                            }
                            else
                            {
                                path2.Visible = true;
                            }
                            
                            if (log.Count > 4)
                            {
                                if (log[3].Contains("cancellation"))
                                {
                                    path1.Visible = false;
                                }
                                else
                                {
                                    pic03.Visible = true;
                                    path3.Visible = true;
                                    pic04.Visible = true;
                                }
                            }
                        }
                    }
                }
            }


            if (log != null)
            {
                foreach (var logentry in log)
                {
                    txtLog.Text += $"{logentry}:{wallet}";
                    txtLog.Text += Environment.NewLine;
                    wallet.ToString();
                    txtLog.Text += Environment.NewLine;
                }
            }
            if(bookingon)
            txtAmo.Text = wallet;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await host.DeleteQueues();
            txtLog.Text += "done";

        }

        private async void button2_Click(object sender, EventArgs e)
        {
            host = new Host();
            var cache = new CacheLogger();
            await host.DeleteQueues();
            Thread.Sleep(TimeSpan.FromSeconds(15));
            cache.DeleteKey("logs");
            cache.DeleteKey("walletBalance");
            await host.Run(namespaceAddress, manageKeyName, manageKey);
            txtLog.Text += "Start Booking Now";
            bookingon = false;
            pic01.Visible = false;
            pic02.Visible = false;
            pic03.Visible = false;
            pic04.Visible = false;
            path1.Visible = false;
            path2.Visible = false;
            path3.Visible = false;
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            bookingon = true;
            var booking = new Workflow.Booking
            {
                TravellerName = txtName.Text,
                Destination = comboBox1.SelectedText,
                CreditLimit = decimal.Parse(txtAmo.Text)
            };
            
            cache.WriteLog("walletBalance", booking.CreditLimit);
            cache.WriteLog("logs", new List<string> { $"Booking process initiated for traveler {booking.TravellerName}" });
            await host.BookTravel(booking);
            pic01.Visible = true;
            valBal = decimal.Parse(txtAmo.Text);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pic1.Visible = false;
            pic2.Visible = false;
            pic3.Visible = false;
            if (comboBox1.SelectedIndex == 0)
            {
                pic1.Visible = true;
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                pic2.Visible = true;
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                pic3.Visible = true;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }
    }
}
