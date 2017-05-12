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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Timers;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DayZServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static Timer aTimer;
        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(HandleRef hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr WindowHandle);
        public const int SW_RESTORE = 9;

        public MainWindow()
        {
            InitializeComponent();

            aTimer = new System.Timers.Timer(200000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.Enabled = true;
            remove_button.Visibility = Visibility.Hidden;
            join_button.Visibility = Visibility.Hidden;
            browse_dialog.Visibility = Visibility.Hidden;

        }

        void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString();
                string[] dirs = Directory.GetFiles(defaultPath + @"\DayZ", "*.DayZProfile");
                string[] dirsexclude = Directory.GetFiles(defaultPath + @"\DayZ", "*.vars*");
                dirs = dirs.Where(w => w != dirs[1]).ToArray();
                string configpath = dirs[0];
                Console.WriteLine("config", configpath);
                string json = System.IO.File.ReadAllText(configpath);
                string servername = json.Split(new string[] { "lastMPServerName=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
                Console.WriteLine("Server Name" + servername);
                string IPAddress = json.Split(new string[] { "lastMPServer=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
                Console.WriteLine("IP Address" + IPAddress);
                string version = json.Split(new string[] { "version=" }, StringSplitOptions.None)[1].Split(new string[] { ";" }, StringSplitOptions.None)[0].Trim();
                Console.WriteLine("Version" + version);
                string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                string path = System.IO.Path.Combine(appDataPath, "DayZServer");
                //string steam = "\\Steam\\SteamApps\\common\\DayZ\\";
                string dayzapppath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzapppath.txt");


                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);

                }
                if (!File.Exists(dayzapppath))
                {
                    string dayzpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "steam", "SteamApps", "common", "DayZ", "DayZ.exe");
                    writeAppPath(dayzpath);
                }




                this.Dispatcher.Invoke((Action)(() =>
                {

                    string serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
                    string currentserverpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzserver.txt");
                    string currentserver = readCurrentServer(currentserverpath);
                    string currentserverNormalized = Regex.Replace(currentserver, @"\s+", String.Empty);
                    string servernameNormalized = Regex.Replace(servername + "\r\nIP Address: " + IPAddress, @"\s+", String.Empty);

                    if (currentserverNormalized != servernameNormalized)
                    {
                        if (currentserver != "No Server History Available")
                        {
                            writeServerHistoryList(serverhistorypath, currentserver);
                        }
                        else
                        {
                            serverList.Text = "No Server History Available";
                            remove_button.Visibility = Visibility.Hidden;
                        }

                        writeCurrentServer(currentserverpath, servername + "\r\nIP Address: " + IPAddress);

                    }
                    else
                    {
                        //writeCurrentServer(currentserverpath, servername);
                        serverTextBox.Text = currentserver;
                        readServerHistoryList(serverhistorypath);

                    }

                    versionlabel.Content = version;
                    DoubleAnimation animation = new DoubleAnimation(0, TimeSpan.FromSeconds(2));
                    c.BeginAnimation(Canvas.OpacityProperty, animation);

                }));

            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }
            Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        }


        public void writeServerHistoryList(string serverhistorypath, string server)
        {


            if (File.Exists(serverhistorypath))
            {
                remove_button.Visibility = Visibility.Visible;
                string temphistory;

                try
                {

                    using (StreamReader sreader = new StreamReader(serverhistorypath))
                    {
                        temphistory = sreader.ReadToEnd();
                    }

                    File.Delete(serverhistorypath);

                    using (StreamWriter sw = File.AppendText(serverhistorypath))
                    {
                        temphistory = server + Environment.NewLine + temphistory;
                        sw.Write(temphistory);
                        sw.Close();
                        readServerHistoryList(serverhistorypath);


                    }


                }
                catch (Exception)
                {

                }
            }
            else
            {
                remove_button.Visibility = Visibility.Visible;
                try
                {

                    using (StreamWriter sw = File.CreateText(serverhistorypath))
                    {

                        if (sw.BaseStream != null)
                        {

                            sw.BaseStream.Seek(0, SeekOrigin.End);
                            sw.WriteLine(server);
                            sw.Close();
                            readServerHistoryList(serverhistorypath);

                        }

                    }

                }
                catch (Exception)
                {

                }

            }
        }

        public void readServerHistoryList(string serverhistorypath)
        {

            if (File.Exists(serverhistorypath))
            {
                try
                {
                    // Create an instance of StreamReader to read from a file. 
                    // The using statement also closes the StreamReader. 
                    using (StreamReader sw = new StreamReader(serverhistorypath))
                    {
                        String line = sw.ReadToEnd();
                        serverList.Text = line;
                    }

                    remove_button.Visibility = Visibility.Visible;
                }
                catch (Exception e)
                {
                    // Let the user know what went wrong.
                    Console.WriteLine("The file could not be read:");
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                //writeServerHistoryList(serverhistorypath, "No Server History");
                serverList.Text = "No Server History Available";
            }

        }

        String readCurrentServer(string currentserverpath)
        {
            if (File.Exists(currentserverpath))
            {
                join_button.Visibility = Visibility.Visible;
                try
                {
                    // Create an instance of StreamReader to read from a file. 
                    // The using statement also closes the StreamReader. 
                    using (StreamReader sw = new StreamReader(currentserverpath))
                    {
                        String line = sw.ReadToEnd();
                        serverTextBox.Text = line;
                        return line;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return "Saving history requires Windows Administrator Privilages";
                }
            }
            else
            {
                return "No Server History Available";
            }

        }



        public void writeCurrentServer(string currentserverpath, string server)
        {
            try
            {

                using (StreamWriter sw = File.CreateText(currentserverpath))
                {

                    if (sw.BaseStream != null)
                    {
                        sw.WriteLine(server);
                        sw.Close();
                        readCurrentServer(currentserverpath);
                    }

                }

            }
            catch (Exception)
            {

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




        private void ClearHistory(object sender, RoutedEventArgs e)
        {
            string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string path = System.IO.Path.Combine(appDataPath, "DayZServer");
            string serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
            if (File.Exists(serverhistorypath))
            {
                File.Delete(serverhistorypath);

            }

            serverList.Text = "No Server History Available";
            remove_button.Visibility = Visibility.Hidden;

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
            string latestserverpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzserver.txt");

            if (File.Exists(latestserverpath))
            {
                var lines = File.ReadAllLines(latestserverpath);
                string server = lines[1].ToString();
                server = server.Remove(0, 12);

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
                                startInf.Arguments = "-connect=" + server;
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
                                    startInf.Arguments = "-connect=" + server;
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


    }
}


