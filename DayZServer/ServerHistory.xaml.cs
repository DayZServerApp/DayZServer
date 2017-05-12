using System;
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
using System.Threading.Tasks;


namespace DayZServer
{

    public partial class ServerHistory : Window
    {
        private static System.Timers.Timer checkProfileForNewServerTimer;
        public string selectedIP;
        public static DataManager dm = new DataManager();


        public ServerHistory()
        {
            InitializeComponent();
            //checkProfileForNewServerTimer = new System.Timers.Timer(8000);
            //checkProfileForNewServerTimer.Elapsed += OnNewServerTimedEvent;
            //checkProfileForNewServerTimer.Enabled = true;
            steamLogin.Visibility = Visibility.Hidden;
            browse_dialog.Visibility = Visibility.Hidden;
            getServerHistory();
            dm.Servers.PropertyChanged += updateData;
            dm.writeServerHistoryList();
            //dm.startDataManager();
            //updateServerList();

            //DataManager.Server currentServer = dm.getCurrentServerList();

            //if (currentServer != null) { selectedIP = currentServer.IP_Address; }

        }


        


        private async void getServerHistory()
        {
            await Task.Run(() => startDataManager());
            DataManager.Server currentServer = dm.getCurrentServerList();
            if (currentServer != null) { selectedIP = currentServer.IP_Address; }
            serverList.ItemsSource = dm.Servers.Values;
            updateServerList();
            // Update the UI with results
        }

        private async Task startDataManager()
        {
            dm.startDataManager();
            //updateServerList();

        }


        private async void updateServerList()
        {
            await Task.Run(() => Dispatch());

            serverList.ItemsSource = dm.Servers.Values;

            // Update the UI with results
        }

        private async Task Dispatch()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {

                string dgSortDescription = null;
                ListSortDirection? dgSortDirection = null;
                int columnIndex = 0;

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

                

                DataManager.Server currentServer = dm.getCurrentServerList();
                if (currentServer != null)
                {
                    if (selectedIP == currentServer.IP_Address)
                    {
                        updateUserList(dm.userList(currentServer.IP_Address));
                    }
                    else
                    {
                        updateUserList(dm.userList(selectedIP));
                    }
                }

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
            //Get the clicked MenuItem
            var menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            var contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            var item = (DataGrid)contextMenu.PlacementTarget;

            //Get the underlying item, that you cast to your object that is bound
            //to the DataGrid (and has subject and state as property)
            var server = (DayZServer.DataManager.Server)item.SelectedCells[0].Item;

            var fullAddress = String.Format("{0}{1}", server.IP_Address, (server.QueryPort > 0 ? String.Format(":{0}", server.QueryPort) : ""));
            Debug.WriteLine("Copy server - " + fullAddress);
            Clipboard.SetText(fullAddress);
        }

        //public void updateServerList()
        //{
           
        //}

        public void checkProfileForNewServer()
        {
            dm.writeServerHistoryList();
        }

        void OnServerListTimedEvent(Object source, ElapsedEventArgs e)
        {

        }

        void OnNewServerTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                checkProfileForNewServer();
            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }

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

            SteamAccess.Login(arr1);
        }

        private void cancelLogin_Click(object sender, RoutedEventArgs e)
        {
            steamLogin.Visibility = Visibility.Hidden;
            userId.Foreground = Brushes.LightGray;
            userId.Text = "UserID";
        }

        private void favorite_Click(object sender, RoutedEventArgs e)
        {

            this.Dispatcher.Invoke((Action)(() =>
            {
                DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;
                string favoriteServer = obj.FullIP_Address;
                dm.updateFavorite(favoriteServer);


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
            string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string path = System.IO.Path.Combine(appDataPath, "DayZServer");
            DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;

            selectedIP = obj.IP_Address;
            updateUserList(obj);
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
                                    if(obj != null) { userList.ItemsSource = obj.playersList; }
                                   
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