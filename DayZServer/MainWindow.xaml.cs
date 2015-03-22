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

namespace DayZServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static Timer aTimer;
        
        public MainWindow()
        {
            InitializeComponent();

            aTimer = new System.Timers.Timer(4000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.Enabled = true;


        }

        void OnImageButtonClick(object sender, RoutedEventArgs e)
        {
           
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
                string version = json.Split(new string[] { "version=" }, StringSplitOptions.None)[1].Split(new string[] { ";" }, StringSplitOptions.None)[0].Trim();
                Console.WriteLine("Server Name" + version);
                string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                string path = System.IO.Path.Combine(appDataPath, "DayZServer");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                   
                this.Dispatcher.Invoke((Action)(() =>
                {

                    
                    
                    string serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
                    string currentserverpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzserver.txt");
                    string currentserver = readCurrentServer(currentserverpath);
                    //readServerHistoryList(serverhistorypath);
                    //string previousserver = serverTextBox.Text;

                    string currentserverNormalized = Regex.Replace(currentserver, @"\s+", String.Empty);
                    string servernameNormalized = Regex.Replace(servername, @"\s+", String.Empty);

                    if (currentserverNormalized != servernameNormalized)
                    {
                        if (currentserver != "No Server History Available") 
                        { 
                        writeServerHistoryList(serverhistorypath, currentserver);
                        }
                        else
                        {
                            serverList.Text = "No Server History Available";
                        }
                        
                        writeCurrentServer(currentserverpath, servername);
                        //serverTextBox.Text = servername;
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

            string temphistory;
            string lastLine = File.ReadLines(serverhistorypath).Last();
            if (File.Exists(serverhistorypath))
            {

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
           }

       }

       String readCurrentServer(string currentserverpath)
        {

            if (File.Exists(currentserverpath))
            {
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
                    // Let the user know what went wrong.
                    return "Saving history requires Windows Administrator Privilages";
                }
            }
            else
            {
                //writeServerHistoryList(currentserverpath, "No Server History Available");
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


   }






}


