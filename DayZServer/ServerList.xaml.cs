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
using Steam;
using System.Data;
using System.ComponentModel;


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
            aTimer = new System.Timers.Timer(4000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.Enabled = true;
            steamLogin.Visibility = Visibility.Hidden;
        }

        public class RootObject
        {
            public List<Server> Server { get; set; }
        }

        public class Server
        {
            public string ServerName { get; set; }
            public string IP_Address { get; set; }
            public DateTime Date { get; set; }
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
                        string dgSortDescription = null;
                        ListSortDirection? dgSortDirection = null;
                        int columnIndex = 0;
                        System.Collections.ObjectModel.ObservableCollection<DataGrid> itemsColl = null;

                        foreach (DataGridColumn column in severList.Columns)
                        {
                            columnIndex++;

                            if (column.SortDirection != null)
                            {
                                dgSortDirection = column.SortDirection;
                                dgSortDescription = column.SortMemberPath;

                                break;
                            }
                        }

                        severList.ItemsSource = dm.getServerList(dm.serverhistorypath);

                        if (!string.IsNullOrEmpty(dgSortDescription) && dgSortDirection != null)
                        {
                            SortDescription s = new SortDescription(dgSortDescription, dgSortDirection.Value);
                            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(severList.ItemsSource);
                            view.SortDescriptions.Add(s);
                            severList.Columns[columnIndex - 1].SortDirection = dgSortDirection;
                        }
                    }

                }));
            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }
            
            //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
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

            string[] arr1 = new string[] { steamid, steampassword, steamAuthCode, steamAuthCode };

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