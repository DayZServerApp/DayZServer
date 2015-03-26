using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SteamKit2;
using DayZServer;
using System.IO;
using System.Threading;
using System.Timers;




namespace DayZServer
{
    /// <summary>

    /// Interaction logic for Window1.xaml

    /// </summary>
    /// 

    public partial class Window1 : Window
    {
        private static System.Timers.Timer aTimer;
        
        public Window1()
        {

            InitializeComponent();
            //List<Server> items = new List<Server>();
            //items.Add(new Server() { ServerName = "Server 1 test dont go there because its ...", IPAddress = "192.168.0.456", Date = "01-22-2015 : 10:30 AM", Favorite = "1" });
            //items.Add(new Server() { ServerName = "Off the Cloud Server Death Zone 5", IPAddress = "192.168.0.275", Date = "01-22-2015 : 10:30 AM", Favorite = "4" });
            //items.Add(new Server() { ServerName = "Joe's Jimmy Joe SAW Only Bambies Welcome Joe's Jimmy Joe SAW Only Bambies Welcome Joe's Jimmy Joe SAW Only Bambies Welcome", IPAddress = "192.168.0.756", Date = "01-22-2015 : 10:30 AM", Favorite = "5" });
            //severList.ItemsSource = items;

            aTimer = new System.Timers.Timer(4000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.Enabled = true;
            

           
            

            steamLogin.Visibility = Visibility.Hidden;
            //authCodeBox.Visibility = Visibility.Hidden;

        }

       

        public class Server
        {

            public string ServerName { get; set; }



            public string IPAddress { get; set; }



            public string Date { get; set; }



            public string Favorite { get; set; }

            
            

        }

        void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    DataManager dm = new DataManager();

                    dm.writeServerHistoryList();
                    if (dm.getServerList(dm.serverhistorypath) != null)
                    {
                        severList.ItemsSource = dm.getServerList(dm.serverhistorypath);
                    }
                    
                   
                   
                }));
            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }
            Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        }


        private void Link_Click(object sender, RoutedEventArgs e)
        {
            string URL = ((Button)sender).Tag.ToString();
            System.Diagnostics.Process.Start(URL);
        }

        private void Join_Click(object sender, RoutedEventArgs e)
        {
            string IPAddress = ((Button)sender).Tag.ToString();
        }

       

        private void steam_click(object sender, RoutedEventArgs e)
        {
            steamLogin.Visibility = Visibility.Visible;
        }

        private void login_close()
        {


        }


        private void userIdGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox source = e.Source as TextBox;
            if (source != null)
            {
                source.Foreground = Brushes.Gray;
                source.Clear();
            }
        }
        private void userIdLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox source = e.Source as TextBox;
            string x = source.Text;
            if (x.Equals(""))
            {
                source.Foreground = Brushes.LightGray;
                source.Text = "UserID";
            }
        }
        

        private void login_click(object sender, RoutedEventArgs e)
        {
            string steamid = userId.Text;
            string steampassword = password.Password;
            string steamAuthCode = authCodeBox.Text;

            string[] arr1 = new string[] { steamid, steampassword, steamAuthCode, steamAuthCode};

             SteamAccess v1 = new SteamAccess();
             SteamAccess.Login(arr1);

           
        }

        private void cancelLogin_Click(object sender, RoutedEventArgs e)
        {
            steamLogin.Visibility = Visibility.Hidden;
            userId.Foreground = Brushes.LightGray;
            userId.Text = "UserID";
        }



    }

}