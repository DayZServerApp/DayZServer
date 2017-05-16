using System;
using System.Collections.Concurrent;
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
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using System.Windows.Threading;

namespace DayZServer
{

    public partial class ServerHistory : Window
    {

      //*****ToDo Try to wire up observable concurrent dictionary correctly

        //public ObservableConcurrentDictionary<string, DataManager.Server> Servers
        //{
        //    get { return this.Servers; }
        //}


        Notifier copynotifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopLeft,
                offsetX: 550,
                offsetY: 29);

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
        public static DataManager.Server selectedServer = new DataManager.Server();
        private TimeSpan _measureGap = TimeSpan.FromSeconds(8);
       


        DispatcherTimer _timer;
        TimeSpan _time;
        TimeSpan _timeset;

        public ServerHistory()
        {
            InitializeComponent();
           
            //checkProfileForNewServerTimer = new System.Timers.Timer(8000);
            //checkProfileForNewServerTimer.Elapsed += OnNewServerTimedEvent;
            //checkProfileForNewServerTimer.Enabled = true;

            //steamLogin.Visibility = Visibility.Hidden;
            browse_dialog.Visibility = Visibility.Hidden;
            //dm.Servers.PropertyChanged += updateData;
            dm.startDataManager();
            //serverList.RowDetailsVisibilityChanged += serverList_RowDetailsVisibilityChanged;

            //serverList.DataContext = dm.Servers;

            serverList.ItemsSource = dm.Servers.Values;
            DataContext = this;

            _time = _measureGap;
            
            //_timeset = TimeSpan.FromSeconds(20);
            _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Background, delegate
            {
                //this.dateText.Text = DateTime.UtcNow.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture);
                this.dateText.Text = _time.ToString(@"ss");

                Console.WriteLine(_time.ToString());

                if (_time == TimeSpan.Zero)
                {
                    _timer.Stop();
                    _time = _measureGap;
                    string dgSortDescription = null;
                    string dgRowDescription = null;
                    ListSortDirection? dgSortDirection = null;
                    Visibility? dgVisibility = Visibility.Hidden;
                    int columnIndex = 0;
                    int rowIndex = 0;

                    foreach (DataGridColumn column in serverList.Columns)
                    {
                        columnIndex++;

                        if (column.SortDirection != null)
                        {
                            dgSortDirection = column.SortDirection;
                            dgSortDescription = column.SortMemberPath;

                            break;
                        }
                    }

                    serverList.ItemsSource = dm.Servers.Values;
                    if (selectedServer.ServerName == null)
                    {
                        selectedServer = dm.Servers.Values.FirstOrDefault(x => x.Current == "1");

                    }
                    updateUserList(selectedServer);

                    if (!string.IsNullOrEmpty(dgSortDescription) && dgSortDirection != null)
                    {
                        SortDescription s = new SortDescription(dgSortDescription, dgSortDirection.Value);
                        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(serverList.ItemsSource);
                        view.SortDescriptions.Add(s);
                        serverList.Columns[columnIndex - 1].SortDirection = dgSortDirection;
                    }
                    _timer.Start();
                }
                _time = _time.Add(TimeSpan.FromSeconds(-1));
            }, this.Dispatcher);
            _timer.Start();


        }



        public double MeasureGap
        {
            get { return _measureGap.TotalSeconds; }
            set
            {
                _measureGap = TimeSpan.FromSeconds(value);

                OnPropertyChanged(value);
            }
            //set { DataManager.PingTimer.Interval = TimeSpan.FromMilliseconds(value).TotalMilliseconds; }
            
        }

     

        public void OnPropertyChanged(double propertyName)
        {
            DataManager.PingTimer.Interval = propertyName * 1000;
            _time = TimeSpan.FromSeconds(propertyName);
            
        }


        //private async void getServerHistory()
        //{
        //    await Task.Run(() => startDataManager());
        //    DataManager.Server currentServer = dm.getCurrentServerList();
        //    if (currentServer != null) { selectedIP = currentServer.IP_Address; }

        //    updateServerList();
        //    // Update the UI with results
        //}

        //private async Task startDataManager()
        //{
        //    dm.startDataManager();
        //    //updateServerList();

        //}


        private async void updateServerList()
        {
            await Task.Run(() => Dispatch());

            //serverList.ItemsSource = dm.Servers.Values;

            // Update the UI with results
        }

        private async Task Dispatch()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {

                string dgSortDescription = null;
                string dgRowDescription = null;
                ListSortDirection? dgSortDirection = null;
                Visibility? dgVisibility = Visibility.Hidden;
                int columnIndex = 0;
                int rowIndex = 0;

                foreach (DataGridColumn column in serverList.Columns)
                {
                    columnIndex++;

                    if (column.SortDirection != null)
                    {
                        dgSortDirection = column.SortDirection;
                        dgSortDescription = column.SortMemberPath;

                        break;
                    }
                }

                serverList.ItemsSource = dm.Servers.Values;
                if (selectedServer.ServerName == null)
                {
                   selectedServer = dm.Servers.Values.FirstOrDefault(x => x.Current == "1");
                   
                }
                updateUserList(selectedServer);

                if (!string.IsNullOrEmpty(dgSortDescription) && dgSortDirection != null)
                {
                    SortDescription s = new SortDescription(dgSortDescription, dgSortDirection.Value);
                    CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(serverList.ItemsSource);
                    view.SortDescriptions.Add(s);
                    serverList.Columns[columnIndex - 1].SortDirection = dgSortDirection;
                }
               
            }));

        }




        private void updateData(object sender, EventArgs e)
        {

            updateServerList();
        }

        public void Copy_ServerIP(object sender, RoutedEventArgs e)
        {
            ////Get the clicked MenuItem
            //var menuItem = (MenuItem)sender;

            ////Get the ContextMenu to which the menuItem belongs
            //var contextMenu = (ContextMenu)menuItem.Parent;

            ////Find the placementTarget
            //var item = (DataGrid)contextMenu.PlacementTarget;

            ////Get the underlying item, that you cast to your object that is bound
            ////to the DataGrid (and has subject and state as property)
            //var server = (DayZServer.DataManager.Server)item.SelectedCells[0].Item;

            //var fullAddress = String.Format("{0}{1}", server.IP_Address, (server.QueryPort > 0 ? String.Format(":{0}", server.QueryPort) : ""));
            //Debug.WriteLine("Copy server - " + fullAddress);
            //Clipboard.SetText(fullAddress);

            DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;
            string fullAddress = obj.FullIP_Address;
            Clipboard.SetText(fullAddress);
            copynotifier.ShowSuccess("Copied: " + fullAddress);

        }


        //private void DataGrid_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        //{
        //    var menuItem = (MenuItem)sender;
        //    var contextMenu = (ContextMenu)menuItem.Parent;
        //    var item = (DataGrid)contextMenu.PlacementTarget;
        //    var currentCell = e.ClipboardRowContent[serverList.CurrentCell.Column.DisplayIndex].item;
        //    e.ClipboardRowContent.Clear();
        //    e.ClipboardRowContent.Add(currentCell);
        //}

        //public void updateServerList()
        //{

        //}

        //public void checkProfileForNewServer()
        //{
        //    dm.writeServerHistoryList();
        //}

        //void OnServerListTimedEvent(Object source, ElapsedEventArgs e)
        //{

        //}

        //void OnNewServerTimedEvent(Object source, ElapsedEventArgs e)
        //{
        //    try
        //    {
        //        checkProfileForNewServer();
        //    }
        //    catch (Exception err)
        //    {
        //        Console.WriteLine("The process failed: {0}", err.ToString());
        //    }

        //}

        private void GT_Click(object sender, RoutedEventArgs e)
        {
            dm.getGTList();
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            string URL = ((Button)sender).Tag.ToString();
            System.Diagnostics.Process.Start(URL);
        }

        private void player_click(object sender, RoutedEventArgs e)
        {


            string IP = ((Button)sender).Tag as string;
            string Name = ((Button)sender).Content as string;
            string URL = "http://www.gametracker.com/player/" + Name + "/" + IP + "/";


            System.Diagnostics.Process.Start(URL);
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
                string favoriteServer = obj.FullIP_Address;
                await dm.updateFavorite(favoriteServer);


            }));
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;
                string deleteServer = obj.ServerName;
                dm.deleteServer(deleteServer);

            }));

        }

        private void ClearHistory(object sender, RoutedEventArgs e)
        {
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
                    string dayzpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "steam", "SteamApps", "common", "DayZ", "DayZ.exe");
                    return dayzpath;
                }
            }
            else
            {
                string dayzpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "steam", "SteamApps", "common", "DayZ", "DayZ.exe");
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
                            startInf.Arguments = "-connect=" + serverIP;
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
                                startInf.Arguments = "-connect=" + serverIP;
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
            



            //for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
            //    if (vis is DataGridRow)
            //    {
            //        var row = (DataGridRow)vis;
            //        row.DetailsVisibility =
            //            row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            //        break;
            //    }
        }

        //private void server_Details(object sender, RoutedEventArgs e)
        //{
        //    string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        //    string path = System.IO.Path.Combine(appDataPath, "DayZServer");
        //    DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;



        //    //selectedIP = obj.IP_Address;
        //    //updateUserList(obj);

        //    for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
        //        if (vis is DataGridRow)
        //        {
        //            var row = (DataGridRow)vis;

        //            if (row.DetailsVisibility == Visibility.Visible)
        //            {
        //                obj.Details = false;
        //                dm.Servers.UpdateWithNotification(obj.IP_Address, obj);
        //                dm.UpdateHistory();
        //            }
        //            else
        //            {
        //                obj.Details = true;
        //                dm.Servers.UpdateWithNotification(obj.IP_Address, obj);
        //                dm.UpdateHistory();
        //            }
        //            break;

        //        }
        //}





        //void serverList_RowDetailsVisibilityChanged(object sender, DataGridRowDetailsEventArgs e)
        //{


        //    innerDataGrid = e.DetailsElement as DataGrid;
        //    DataManager.Server obj = e.Row.DataContext as DataManager.Server;
        //    if (obj != null && innerDataGrid != null)
        //    {
        //        UpdateUserList(innerDataGrid, obj);
        //    }


        //}


        //public void UpdateUserList(DataGrid detailsDataGrid, DataManager.Server obj)
        //{
        //    this.Dispatcher.Invoke((Action)(() =>
        //    {
        //        string dgSortDescriptionUser = null;
        //        ListSortDirection? dgSortDirectionUser = null;
        //        int columnIndexUser = 0;


        //        foreach (DataGridColumn column in detailsDataGrid.Columns)
        //        {
        //            columnIndexUser++;

        //            if (column.SortDirection != null)
        //            {
        //                dgSortDirectionUser = column.SortDirection;
        //                dgSortDescriptionUser = column.SortMemberPath;

        //                break;
        //            }
        //        }
        //        try
        //        {
        //            if (obj != null) { detailsDataGrid.ItemsSource = obj.playersList; }

        //        }
        //        catch (Exception err)
        //        {
        //            Console.WriteLine(err.Message);
        //        }


        //        if (!string.IsNullOrEmpty(dgSortDescriptionUser) && dgSortDirectionUser != null)
        //        {
        //            SortDescription sUser = new SortDescription(dgSortDescriptionUser, dgSortDirectionUser.Value);
        //            CollectionView viewUser = (CollectionView)CollectionViewSource.GetDefaultView(detailsDataGrid.ItemsSource);
        //            try
        //            {

        //            }
        //            catch (Exception err)
        //            {
        //                Console.WriteLine(err.Message);

        //            }


        //        }

        //    }));
        //}
     




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
                                        //int p = (int)previousServer.UserCount + 1;
                                        //int c = (int) obj.UserCount;

                                        //if (userList.Items.Count > obj.playersList.Count)
                                        //{
                                        //    playernotifier.ShowError("Players Joining DayZ :");
                                        //}else if (userList.Items.Count < obj.playersList.Count)
                                        //{
                                        //    playernotifier.ShowInformation("Players Leaving DayZ");
                                        //}
                                        ActiveServerName.Text = obj.ServerName;
                                        userList.ItemsSource = obj.playersList;
                                        //if (copynotifier != null)
                                        //{
                                           // copynotifier.ShowSuccess("Players Joining DayZ");
                                        //}
                                       
                                        //layernotifier.ShowError("Players Joining DayZ :");
                                        //if (previousServer.FullIP_Address != null) { 
                                        //if (obj.FullIP_Address == previousServer.FullIP_Address &&
                                        //    c >= p)
                                        //{
                                        //    playernotifier.ShowError("Players Joining DayZ :");
                                        //    previousServer = obj;
                                        //}
                                        //    else if (obj.FullIP_Address == previousServer.FullIP_Address &&
                                        //             c <= p)
                                        //{
                                        //    playernotifier.ShowInformation("Players Leaving DayZ");
                                        //    previousServer = obj;
                                        //    }
                                        //}


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