using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Timers;
using Steam;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Documents;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using System.Windows.Threading;
using System.Windows.Navigation;
using System.Windows.Media.Imaging;


namespace DayZServer
{
    public partial class ServerHistory : Window
    {

        Notifier copynotifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.BottomLeft,
                offsetX: 500,
                offsetY: 16);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(5));

            cfg.Dispatcher = Application.Current.Dispatcher;
        });

        Notifier playernotifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 20,
                offsetY: 80);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(5));

            cfg.Dispatcher = Application.Current.Dispatcher;
        });

        public string selectedIP;
        public static DataManager dm = new DataManager();
        public static DataGrid innerDataGrid = new DataGrid();
        public DataManager.Server selectedServer = new DataManager.Server();
        //public ObservableCollection<DataManager.Server> Servers = new ObservableCollection<DataManager.Server>();

        private TimeSpan _measureGap = TimeSpan.FromSeconds(7);
        DispatcherTimer _timer;
        TimeSpan _time;

        //private void intList_Changed(object sender, PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == "Current")
        //    {
                
        //        selectedServer = dm.Servers.FirstOrDefault(x => x.Current == "1");
        //        updateUserList(selectedServer);
        //    }
           
        //}
        public void items_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                //your code
                selectedServer = dm.Servers.FirstOrDefault(x => x.Current == "1");
                updateUserList(selectedServer);
            }
            if(e.OldItems != null) { 
            
            foreach (INotifyPropertyChanged item in e.OldItems)
                item.PropertyChanged -= new
                    PropertyChangedEventHandler(item_PropertyChanged);
            
        }
            if (e.NewItems != null)
            {

                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged +=
                        new PropertyChangedEventHandler(item_PropertyChanged);
            }
        }

        public void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Current")
               {

                   selectedServer = dm.Servers.FirstOrDefault(x => x.Current == "1");
                   updateUserList(selectedServer);
                }
            //if (e.PropertyName == "PingSpeed")
            //{

                
            //}
        }
        public ServerHistory()
        {
            InitializeComponent();
            ActiveServerName.MouseLeftButtonDown += new MouseButtonEventHandler(Hyperlink_RequestNavigate);
            browse_dialog.Visibility = Visibility.Hidden;
            dm.startDataManager();
            //dm.Servers.CollectionChanged += CollectionChangedMethod;
            //((INotifyPropertyChanged)Servers).PropertyChanged +=
            //    new PropertyChangedEventHandler(intList_Changed);
            


            DataContext = this;
            serverList.ItemsSource = dm.Servers;
            ObservableCollection<INotifyPropertyChanged> items =
                new ObservableCollection<INotifyPropertyChanged>();
            dm.Servers.CollectionChanged +=
                new System.Collections.Specialized.NotifyCollectionChangedEventHandler(
                    items_CollectionChanged);
            //selectedServer = dm.Servers.FirstOrDefault(x => x.Current == "1");
            _time = _measureGap;
            _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Background, delegate
            {
                if (string.IsNullOrEmpty(selectedServer.ServerName))
                {
                    selectedServer = dm.Servers.FirstOrDefault(x => x.Current == "1");
                    
                }
                updateUserList(selectedServer);
                

                if (_time == TimeSpan.Zero)
            {
                _timer.Stop();
                _time = _measureGap;


                    _timer.Start();
                }
                _time = _time.Add(TimeSpan.FromSeconds(-1));
            }, this.Dispatcher);
           _timer.Start();


        }

       




        //private void CollectionChangedMethod(object sender, NotifyCollectionChangedEventArgs e)
        //{


        //    //different kind of changes that may have occurred in collection
        //    if (e.Action == NotifyCollectionChangedAction.Add)
        //    {
        //        //your code
        //        selectedServer = dm.Servers.FirstOrDefault(x => x.Current == "1");
        //        updateUserList(selectedServer);
        //    }
        //    if (e.Action == NotifyCollectionChangedAction.Replace)
        //    {
        //        //your code
        //    }
        //    if (e.Action == NotifyCollectionChangedAction.Remove)
        //    {
        //        //your code
        //    }
        //    if (e.Action == NotifyCollectionChangedAction.Move)
        //    {
        //        //your code
        //    }
        //}

        public double MeasureGap
        {
            get { return _measureGap.TotalSeconds; }
            set
            {
                _measureGap = TimeSpan.FromSeconds(value);

                OnPropertyChanged(value);
            }

        }



        public void OnPropertyChanged(double propertyName)
        {
            DataManager.PingTimer.Interval = propertyName * 1000;
            _time = TimeSpan.FromSeconds(propertyName);

        }


        public void updateServerList(object sender, EventArgs e)
        {
            Dispatch();

        }

        private void Dispatch()
        {
            
            //Visibility? dgVisibility = Visibility.Hidden;
            //int columnIndex = 0;
            //int rowIndex = 0;

           //foreach (DataGridColumn column in serverList.Columns)
            //{
               //columnIndex++;

            //    if (column.SortDirection != null)
            //    {
            //        dgSortDirection = column.SortDirection;
            //        dgSortDescription = column.SortMemberPath;

            //        break;
            //    }
           //}

            ////serverList.ItemsSource = dm.Servers;
            ////if (selectedServer.ServerName == null)
            ////{
            ////    selectedServer = dm.Servers.FirstOrDefault(x => x.Current == "1");

            ////}
            ////updateUserList(selectedServer);

            //if (!string.IsNullOrEmpty(dgSortDescription) && dgSortDirection != null)
            //{
            //    CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(serverList.ItemsSource);
            //    view.SortDescriptions.Add(s);
            //    serverList.Columns[columnIndex - 1].SortDirection = dgSortDirection;
            //}

        }






        public void Copy_ServerIP(object sender, RoutedEventArgs e)
        {
            DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;
            string fullAddress = obj.FullIP_Address;
            Clipboard.SetText(fullAddress);
            copynotifier.ShowSuccess("Copied: " + fullAddress);
        }

        private void GT_Click(object sender, RoutedEventArgs e)
        {
            dm.getGTList();
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            string URL = ((Button)sender).Tag.ToString();
            System.Diagnostics.Process.Start(URL);
        }

        private void Map_Click(object sender, RoutedEventArgs e)
        {
            Map map = new Map();
            map.ShowDialog();
        }

        private void player_click(object sender, RoutedEventArgs e)
        {
            string IP = ((Button)sender).Tag as string;
            string Name = ((Button)sender).Content as string;
            string URL = "http://www.gametracker.com/player/" + Name + "/" + IP + "/";
            Process.Start(URL);
        }

        private void Join_Click(object sender, RoutedEventArgs e)
        {
            string IPAddress = ((Button)sender).Tag.ToString();
        }

        private void steam_click(object sender, RoutedEventArgs e)
        {
            //steamLogin.Visibility = Visibility.Visible;
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
            //string steamid = userId.Text;
            //string steampassword = password.Password;
            //string steamAuthCode = authCodeBox.Text;

            //string[] arr1 = new string[] { steamid, steampassword, steamAuthCode, steamAuthCode };

            //SteamAccess.Login(arr1);
        }

        private void cancelLogin_Click(object sender, RoutedEventArgs e)
        {
            //steamLogin.Visibility = Visibility.Hidden;
            //userId.Foreground = Brushes.LightGray;
            //userId.Text = "UserID";
        }

        private void favorite_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(async () =>
            {
                DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;
                Button fButton = sender as Button;
                ImageBrush checkon = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "images/checkon.png")));
                ImageBrush checkoff = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "images/checkoff.png")));

                //fButton.Background = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/DayZServer;images/checkon.png")));

                if (obj.Favorite == "0")
                {
                    fButton.Background = checkon;
                }
                else
                {
                    fButton.Background = checkoff;
                }

                string favoriteServer = obj.FullIP_Address;
                await dm.updateFavorite(favoriteServer);
            }));
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;
                string deleteServer = obj.IP_Address;
                dm.deleteServer(deleteServer);

            }));
        }

        private void ClearHistory(object sender, RoutedEventArgs e)
        {
            selectedServer = dm.Servers.FirstOrDefault(x => x.Current == "1");
            updateUserList(selectedServer);
            dm.deleteServerHistory();
        }

        public void writeAppPath(string dayzpath)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                string path = System.IO.Path.Combine(appDataPath, "DayZServer");
                string dayzapppath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzapppath.txt");

                using (StreamWriter sw = File.CreateText(dayzapppath))
                {
                    if (sw.BaseStream != null)
                    {
                        sw.WriteLine(dayzpath);
                        sw.Close();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        String readAppPath(string dayzapppath)
        {
            if (File.Exists(dayzapppath))
            {
                try
                {
                    // Create an instance of StreamReader to read from a file. 
                    // The using statement also closes the StreamReader. 
                    using (StreamReader sw = new StreamReader(dayzapppath))
                    {
                        String line = sw.ReadToEnd();
                        return line;
                    }
                }
                catch (Exception e)
                {
                    // Let the user know what went wrong.
                    string dayzpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "steam", "SteamApps", "common", "DayZ", "DayZ.exe");
                    return dayzpath;
                }
            }
            else
            {
                string dayzpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "steam", "SteamApps", "common", "DayZ", "DayZ.exe");
                writeAppPath(dayzpath);
                return dayzpath;
            }
        }

        public bool IsProcessOpen(string name)
        {
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Equals(name))
                {
                    return true;
                }
            }
            return false;
        }

        private void JoinServer(object sender, RoutedEventArgs e)
        {
            string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string path = System.IO.Path.Combine(appDataPath, "DayZServer");
            DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;
            string serverIP = obj.IP_Address;
            string gamePort = obj.Game_Port;

            // start the game seperated from this process.
            using (Process game = new Process())
            {
                string dayzapppath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzapppath.txt");
                string dayzpath = readAppPath(dayzapppath);
                dayzpath = dayzpath.TrimEnd('\r', '\n');
                if (File.Exists(dayzpath))
                {
                    try
                    {
                        Process thisProc = Process.GetCurrentProcess();
                        if (IsProcessOpen("DayZ") == false)
                        {
                            ProcessStartInfo startInf = new ProcessStartInfo(dayzpath);
                            startInf.Arguments = "-cpuCount=2 -noSplash -exThreads=3 -mod=@cars -connect=" + serverIP + " -port=" + gamePort + "";
                            game.StartInfo = startInf;
                            game.Start();
                        }
                        else
                        {
                            Process[] objProcesses = System.Diagnostics.Process.GetProcessesByName("DayZ");
                            if (objProcesses.Length > 0)
                            {
                                objProcesses[0].Kill();
                                ProcessStartInfo startInf = new ProcessStartInfo(dayzpath);
                                startInf.Arguments = "-cpuCount=2 -noSplash -exThreads=3 -mod=@cars -connect=" + serverIP + " -port=" + gamePort + "";
                                game.StartInfo = startInf;
                                game.Start();

                            }
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                    }
                }
                else
                {
                    browse_dialog.Visibility = Visibility.Visible;
                }
            }
        }

        private void cancel_click(object sender, RoutedEventArgs e)
        {
            browse_dialog.Visibility = Visibility.Hidden;
        }

        private void browse_click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".exe";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                browse_dialog.Visibility = Visibility.Hidden;
                writeAppPath(filename);
            }
        }


        private void server_Details(object sender, RoutedEventArgs e)
        {
            DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;

            selectedIP = obj.IP_Address;
            updateUserList(obj);
            selectedServer = obj;
        }

       
        private void Hyperlink_RequestNavigate(object sender, MouseButtonEventArgs e)
        {
            string FullIP_Address = selectedServer.FullIP_Address;
            string URL = "https://www.gametracker.com/server_info/" + FullIP_Address + "/";
            Process.Start(URL);
        }

        private void ServerHyperlink_RequestNavigate(object sender, MouseButtonEventArgs e)
        {
            DataManager.Server obj = ((TextBlock)sender).Tag as DataManager.Server;
            string FullIP_Address = obj.FullIP_Address;
            string URL = "https://www.gametracker.com/server_info/" + FullIP_Address + "/";
            Process.Start(URL);
        }



        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            DataManager.Server obj = ((Hyperlink)sender).Tag as DataManager.Server;
            string FullIP_Address = obj.FullIP_Address;
            string URL = "https://www.gametracker.com/server_info/" + FullIP_Address + "/";
            Process.Start(URL);
        }



        public void updateUserList(DataManager.Server obj)
        {

            this.Dispatcher.Invoke((Action)(() =>
            {
                string dgSortDescriptionUser = null;
                ListSortDirection? dgSortDirectionUser = null;
                int columnIndexUser = 0;

                foreach (DataGridColumn column in userList.Columns)
                {
                    columnIndexUser++;

                    if (column.SortDirection != null)
                    {
                        dgSortDirectionUser = column.SortDirection;
                        dgSortDescriptionUser = column.SortMemberPath;

                        break;
                    }
                }
                try
                {
                    if (obj != null)
                    {
                        ActiveServerName.Text = obj.ServerName;
                        userList.ItemsSource = obj.playersList;
                    }

                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }


                if (!string.IsNullOrEmpty(dgSortDescriptionUser) && dgSortDirectionUser != null)
                {
                    SortDescription sUser = new SortDescription(dgSortDescriptionUser, dgSortDirectionUser.Value);
                    CollectionView viewUser = (CollectionView)CollectionViewSource.GetDefaultView(userList.ItemsSource);
                    try
                    {
                        viewUser.SortDescriptions.Add(sUser);
                        userList.Columns[columnIndexUser - 1].SortDirection = dgSortDirectionUser;
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);

                    }


                }

            }));


        }
    }
}