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
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Net;


namespace DayZServer
{
    /// <summary>

    /// Interaction logic for Window1.xaml

    /// </summary>
    /// 

    public partial class Window1 : Window
    {
        private static System.Timers.Timer updateServerListTimer;
        private static System.Timers.Timer checkProfileForNewServerTimer;
        public string selectedIP;
        public static DataManager dm = new DataManager();

        public Window1()
        {
            InitializeComponent();
            updateServerListTimer = new System.Timers.Timer(1000);
            updateServerListTimer.Elapsed += OnServerListTimedEvent;
            updateServerListTimer.Enabled = true;
            checkProfileForNewServerTimer = new System.Timers.Timer(4000);
            checkProfileForNewServerTimer.Elapsed += OnNewServerTimedEvent;
            checkProfileForNewServerTimer.Enabled = true;
            steamLogin.Visibility = Visibility.Hidden;
            browse_dialog.Visibility = Visibility.Hidden;
            dm.startDataManager();
        }

        public void updateServerList()
        {
            this.Dispatcher.Invoke((Action)(() =>
                {
                    if (dm.getList() != null)
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
                        serverList.ItemsSource = dm.getList();
                        //serverList.ItemsSource = dm.getServerList();

                        //if (userList.Items.Count == 0)
                        //{
                        //    DataManager.Server serverobj = dm.getCurrentServerList();
                        //    updateUserList(serverobj);
                        //    selectedIP = serverobj.IP_Address;

                        //}
                        //else
                        //{

                        //    DataManager.Server serverobj = dm.getServerByIP(selectedIP);
                        //    updateUserList(serverobj);
                        //}

                        if (!string.IsNullOrEmpty(dgSortDescription) && dgSortDirection != null)
                        {
                            SortDescription s = new SortDescription(dgSortDescription, dgSortDirection.Value);
                            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(serverList.ItemsSource);
                            view.SortDescriptions.Add(s);
                            serverList.Columns[columnIndex - 1].SortDirection = dgSortDirection;
                        }
                    }

                }));
        }


        public void checkProfileForNewServer()
        {
            dm.writeServerHistoryList();
        }

        void OnServerListTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                updateServerList();
            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }
            //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
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
                updateServerList();

            }));
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
            DataManager.Server obj = ((Button)sender).Tag as DataManager.Server;
            string deleteServer = obj.ServerName;
            dm.deleteServer(deleteServer);
            updateServerList();
            }));
            //if (obj.IP_Address == selectedIP)
            //{
            //    DataManager.Server serverobj = dm.getCurrentServerList();
            //    updateUserList(serverobj);
            //    selectedIP = serverobj.IP_Address;
            //}
        }

        private void ClearHistory(object sender, RoutedEventArgs e)
        {
            string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string path = System.IO.Path.Combine(appDataPath, "DayZServer");
            string serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
            if (File.Exists(serverhistorypath))
            {
                File.Delete(serverhistorypath);

            }

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
                    //clsProcess.Kill();
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

                                //******Works to bring the app to the foreground but cant pass the server Argument so just commented this. 
                                //IntPtr hWnd = IntPtr.Zero;
                                //hWnd = objProcesses[0].MainWindowHandle;
                                //ShowWindowAsync(new HandleRef(null,hWnd), SW_RESTORE);
                                //SetForegroundWindow(objProcesses[0].MainWindowHandle);

                                //******could not get Steam to pass the server Argument so just killed the app and start it up. 

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


            //if (userList.Items.Count != 0)
            //{
            string dgSortDescriptionUser = null;
            ListSortDirection? dgSortDirectionUser = null;
            int columnIndexUser = 0;
            //System.Collections.ObjectModel.ObservableCollection<DataGrid> itemsColl = null;

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

            userList.ItemsSource = obj.playersList;

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

            //}
            //else
            //{
            //    userList.ItemsSource = obj.linkItem;
            //}


        }

    }
}

