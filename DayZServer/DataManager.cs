using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Threading;
using System.Net;
using Microsoft;




namespace DayZServer
{

    public class DataManager
    {
        public string defaultPath;
        public string[] dirs;
        public string json;
        public string servername;
        public string FullIPAddress;
        public string IPAddress;
        public string version;
        public string appDataPath;
        public string path;
        public string dayzapppath;
        public string dayzpath;
        public string serverhistorypath;
        public string currentserverpath;
        public int pingIndex;
        public List<Server> server_list;

        public DataManager()
        {
            defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString();
            dirs = Directory.GetFiles(defaultPath + @"\DayZ", "*.DayZProfile");
            dirs = dirs.Where(w => w != dirs[1]).ToArray();
            string configpath = dirs[0];
            //Console.WriteLine("config", configpath);
            json = System.IO.File.ReadAllText(configpath);
            servername = json.Split(new string[] { "lastMPServerName=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
            FullIPAddress = json.Split(new string[] { "lastMPServer=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
            IPAddress = FullIPAddress.Substring(0, FullIPAddress.LastIndexOf(":"));
            version = json.Split(new string[] { "version=" }, StringSplitOptions.None)[1].Split(new string[] { ";" }, StringSplitOptions.None)[0].Trim();
            appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            path = System.IO.Path.Combine(appDataPath, "DayZServer");
            dayzapppath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzapppath.txt");
            dayzpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "steam", "SteamApps", "common", "DayZ", "DayZ.exe");
            serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
            currentserverpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzserver.txt");
            Console.WriteLine("Server History Path: " + serverhistorypath);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                if (!File.Exists(dayzapppath))
                {
                    writeAppPath(dayzpath);
                }
            }
        }

        public class Server
        {
            public string ServerName { get; set; }
            public string IP_Address { get; set; }
            public string FullIP_Address { get; set; }
            public DateTime Date { get; set; }
            public string Favorite { get; set; }
            public string Current { get; set; }
            public string PingSpeed { get; set; }
            public string UserCount { get; set; }
            public List<LinkItem> linkItem { get; set; }
        }

        public class LinkItem
        {
            public string Href { get; set; }
            public string UserName { get; set; }
        }


        public void writeServerHistoryList()
        {
            if (File.Exists(serverhistorypath))
            {
                string temphistory;
                try
                {
                    using (StreamReader sreader = new StreamReader(serverhistorypath))
                    {
                        temphistory = sreader.ReadToEnd();
                        sreader.Close();
                        string matchIdToFindServer = servername;
                        string matchIdToFindIPAddress = IPAddress;
                        List<Server> customData = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                        Server match = customData.FirstOrDefault(x => x.ServerName == servername);
                        int index = customData.FindIndex(x => x.ServerName == servername);

                        foreach (Server element in customData)
                        {
                            Console.WriteLine(element);
                            string[] arr1 = new string[] { element.IP_Address };
                            int currentItem = customData.IndexOf(element);
                            Pinger(arr1, currentItem);
                            pingIndex = customData.IndexOf(element);
                        }

                        if (match != null)
                        {
                            match.Date = DateTime.Now;
                            match.IP_Address = IPAddress;
                            match.FullIP_Address = FullIPAddress;
                            match.Current = "1";
                            Server replacement = match;
                            customData[index] = replacement;
                            File.Delete(serverhistorypath);
                            string listjson = JsonConvert.SerializeObject(customData.ToArray());
                            using (StreamWriter sw = File.CreateText(serverhistorypath))
                            {
                                sw.Write(listjson);
                                sw.Close();
                            }
                        }
                        else
                        {
                            Server matchCurrent = customData.FirstOrDefault(x => x.Current == "1");
                            int indexCurrent = customData.FindIndex(x => x.Current == "1");
                            if (matchCurrent != null)
                            {
                                matchCurrent.Current = "0";
                                Server replacementCurrent = matchCurrent;
                                customData[indexCurrent] = replacementCurrent;
                            }

                            customData.Add(new Server()
                            {
                                ServerName = servername,
                                IP_Address = IPAddress,
                                FullIP_Address = FullIPAddress,
                                Date = DateTime.Now,
                                Favorite = "0",
                                Current = "1",
                                PingSpeed = "Accessing...",
                            });
                            File.Delete(serverhistorypath);
                            string listjson = JsonConvert.SerializeObject(customData.ToArray());
                            using (StreamWriter sw = File.CreateText(serverhistorypath))
                            {
                                sw.Write(listjson);
                                sw.Close();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }
            else
            {
                try
                {
                    using (StreamWriter sw = File.CreateText(serverhistorypath))
                    {
                        var server_items = new List<Server>();
                        server_items.Add(new Server()
                        {
                            ServerName = servername,
                            IP_Address = IPAddress,
                            FullIP_Address = FullIPAddress,
                            Date = DateTime.Now,
                            Favorite = "0",
                            Current = "1",
                            PingSpeed = "Accessing...",
                        });

                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(sw, server_items);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }
        }

        public List<Server> getServerList(string serverhistorypath)
        {
            if (File.Exists(serverhistorypath))
            {
                string temphistory;
                try
                {
                    using (StreamReader sreader = new StreamReader(serverhistorypath))
                    {
                        temphistory = sreader.ReadToEnd();
                        server_list = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                        return server_list;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }



        public Server getCurrentServerList()
        {
            string temphistory;
            try
            {
                using (StreamReader sreader = new StreamReader(serverhistorypath))
                {


                    temphistory = sreader.ReadToEnd();
                    sreader.Close();
                    string matchIdToFindServer = servername;
                    string matchIdToFindIPAddress = IPAddress;
                    List<Server> customData = JsonConvert.DeserializeObject<List<Server>>(temphistory);

                    Server matchCurrent = customData.FirstOrDefault(x => x.Current == "1");
                    int indexCurrent = customData.FindIndex(x => x.Current == "1");
                    if (matchCurrent != null)
                    {
                        return matchCurrent;
                    }
                    else
                    {
                        return null;
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception" + e);
                return null;
            }
        }

        public Server getServerByIP(string ip)
        {
            string temphistory;
            try
            {
                using (StreamReader sreader = new StreamReader(serverhistorypath))
                {
                    temphistory = sreader.ReadToEnd();
                    sreader.Close();
                    string matchIdToFindServer = servername;
                    string matchIdToFindIPAddress = IPAddress;
                    List<Server> customData = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                    Server matchCurrent = customData.FirstOrDefault(x => x.IP_Address == ip);
                    if (matchCurrent != null)
                    {
                        return matchCurrent;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception" + e);
                return null;
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
                    using (StreamReader sw = new StreamReader(dayzapppath))
                    {
                        String line = sw.ReadToEnd();
                        return line;
                    }
                }
                catch (Exception e)
                {
                    return dayzpath;
                }
            }
            else
            {
                writeAppPath(dayzpath);
                return dayzpath;
            }
        }

        public void updateFavorite(string favoriteServer)
        {
            if (File.Exists(serverhistorypath))
            {
                string temphistory;
                try
                {
                    using (StreamReader sreader = new StreamReader(serverhistorypath))
                    {
                        temphistory = sreader.ReadToEnd();
                        sreader.Close();
                        string matchIdToFindServer = favoriteServer;
                        string matchIdToFindIPAddress = IPAddress;
                        List<Server> customData = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                        Server match = customData.FirstOrDefault(x => x.ServerName == favoriteServer);
                        int index = customData.FindIndex(x => x.ServerName == favoriteServer);


                        if (match != null)
                        {
                            if (match.Favorite == "1")
                            {
                                match.Favorite = "0";
                            }
                            else if (match.Favorite == "0")
                            {
                                match.Favorite = "1";
                            }

                            Server replacement = match;
                            customData[index] = replacement;
                            File.Delete(serverhistorypath);
                            string listjson = JsonConvert.SerializeObject(customData.ToArray());
                            using (StreamWriter sw = File.CreateText(serverhistorypath))
                            {
                                sw.Write(listjson);
                                sw.Close();
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }
            else
            {
                try
                {
                    using (StreamWriter sw = File.CreateText(serverhistorypath))
                    {
                        var server_items = new List<Server>();
                        server_items.Add(new Server()
                        {
                            ServerName = servername,
                            IP_Address = IPAddress,
                            Date = DateTime.Now,
                            Favorite = "0",
                            Current = "1"
                        });

                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(sw, server_items);
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public void deleteServer(string deleteServer)
        {
            if (File.Exists(serverhistorypath))
            {
                string temphistory;
                try
                {
                    using (StreamReader sreader = new StreamReader(serverhistorypath))
                    {
                        temphistory = sreader.ReadToEnd();
                        sreader.Close();
                        string matchIdToFindServer = deleteServer;
                        string matchIdToFindIPAddress = IPAddress;
                        List<Server> customData = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                        Server match = customData.FirstOrDefault(x => x.ServerName == deleteServer);
                        int index = customData.FindIndex(x => x.ServerName == deleteServer);


                        if (match != null)
                        {

                            customData.RemoveAt(index);
                            File.Delete(serverhistorypath);
                            string listjson = JsonConvert.SerializeObject(customData.ToArray());
                            using (StreamWriter sw = File.CreateText(serverhistorypath))
                            {
                                sw.Write(listjson);
                                sw.Close();
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }

        }

        //public static String getPing(string ip)
        //{

        //    try
        //    {

        //        Ping ping = new Ping();
        //        PingReply pingresult = ping.Send(ip);
        //        string pingSpeed1;
        //        if (pingresult.Status.ToString() == "Success")
        //        {
        //            pingSpeed1 = pingresult.RoundtripTime.ToString();
        //            return pingSpeed1;
        //        }
        //        else
        //        {
        //            pingSpeed1 = pingresult.Status.ToString();
        //            return pingSpeed1;
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        return "Unavailable";
        //    }
        //}


        public static void Pinger(string[] args, int index)
        {
            if (args.Length == 0)
                throw new ArgumentException("Ping needs a host or IP Address.");

            string who = args[0];
            AutoResetEvent waiter = new AutoResetEvent(false);

            Ping pingSender = new Ping();

            // When the PingCompleted event is raised, 
            // the PingCompletedCallback method is called.
            pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);



            // Create a buffer of 32 bytes of data to be transmitted. 
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            // Wait 12 seconds for a reply. 
            int timeout = 12000;

            // Set options for transmission: 
            // The data can go through 64 gateways or routers 
            // before it is destroyed, and the data packet 
            // cannot be fragmented.
            PingOptions options = new PingOptions(64, true);

            Console.WriteLine("Time to live: {0}", options.Ttl);
            Console.WriteLine("Don't fragment: {0}", options.DontFragment);

            // Send the ping asynchronously. 
            // Use the waiter as the user token. 
            // When the callback completes, it can wake up this thread.
            pingSender.SendAsync(who, timeout, buffer, options, waiter);

            // Prevent this example application from ending. 
            // A real application should do something useful 
            // when possible.
            //waiter.WaitOne();
            Console.WriteLine("Ping example completed.");
        }

        private static void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            // If the operation was canceled, display a message to the user. 
            if (e.Cancelled)
            {
                Console.WriteLine("Ping canceled.");

                // Let the main thread resume.  
                // UserToken is the AutoResetEvent object that the main thread  
                // is waiting for.
                ((AutoResetEvent)e.UserState).Set();
            }

            // If an error occurred, display the exception to the user. 
            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(e.Error.ToString());

                // Let the main thread resume. 
                ((AutoResetEvent)e.UserState).Set();
            }

            PingReply reply = e.Reply;

            DisplayReply(reply);

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();
        }



        public static void DisplayReply(PingReply reply)
        {
            if (reply == null)
                return;

            Console.WriteLine("ping status: {0}", reply.Status);
            if (reply.Status == IPStatus.Success)
            {
                string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                string path = System.IO.Path.Combine(appDataPath, "DayZServer");
                string serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
                using (StreamReader sreader = new StreamReader(serverhistorypath))
                {
                    string temphistory = sreader.ReadToEnd();
                    sreader.Close();
                    List<Server> customData = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                    Server match = customData.FirstOrDefault(x => x.IP_Address == reply.Address.ToString());
                    int index = customData.FindIndex(x => x.IP_Address == reply.Address.ToString());

                    if (match != null)
                    {

                        match.linkItem = userList(match.FullIP_Address);
                        match.UserCount = serverInfo(match.FullIP_Address)[0];
                        match.PingSpeed = reply.RoundtripTime.ToString();
                        Server replacement = match;
                        customData[index] = replacement;
                        File.Delete(serverhistorypath);
                        string listjson = JsonConvert.SerializeObject(customData.ToArray());
                        using (StreamWriter sw = File.CreateText(serverhistorypath))
                        {
                            sw.Write(listjson);
                            sw.Close();
                        }
                    }

                }

                //Console.WriteLine("Address: {0}", reply.Address.ToString());
                //Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                //Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                //Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                //Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
            }
        }



        public static String[] serverInfo(string ip)
        {

            WebClient webClient = new WebClient();

            string strUrl = "http://cache.www.gametracker.com/components/html0/?host=" + ip + "&currentPlayersHeight=300&showCurrPlayers=1";
            byte[] reqHTML;
            reqHTML = webClient.DownloadData(strUrl);
            UTF8Encoding objUTF8 = new UTF8Encoding();
            string html = objUTF8.GetString(reqHTML);

            int Start, End;
            string noHTML;
            string strStart = "Players:";
            string strEnd = "Rank";



            if (html.Contains("Players:") && html.Contains("Rank:"))
            {
                Start = html.IndexOf(strStart, 0) + strStart.Length;
                End = html.IndexOf(strEnd, Start);
                html = html.Substring(Start, End - Start);
                noHTML = Regex.Replace(html, @"<[^>]+>|&nbsp;", "").Trim();

            }
            else
            {
                noHTML = "Unavailable";
            }


            string[] arr1 = new string[] { noHTML };
            return arr1;
        }

        public static List<LinkItem> userList(string ip)
        {

            WebClient webClient = new WebClient();

            string strUrl = "http://cache.www.gametracker.com/components/html0/?host=" + ip + "&currentPlayersHeight=300&showCurrPlayers=1";
            byte[] reqHTML;
            reqHTML = webClient.DownloadData(strUrl);
            UTF8Encoding objUTF8 = new UTF8Encoding();
            string phtml = objUTF8.GetString(reqHTML);


            if (phtml.Contains("Players:") && phtml.Contains("Rank:"))
            {
                Match m2 = Regex.Match(phtml, "(?<=Online Players).*?(?=JOIN THIS SERVER)", RegexOptions.Singleline);
                if (m2.Success)
                {
                    phtml = m2.ToString();

                }
            }
            else
            {
                phtml = "<a href=\"https://github.com/DayZServerApp/DayZServer/releases\" target=\"_blank\" > Unavailable </a>";
            }



            List<LinkItem> list = new List<LinkItem>();

            // 1.
            // Find all matches in file.
            MatchCollection m1 = Regex.Matches(phtml, @"(<a.*?>.*?</a>)",
                RegexOptions.Singleline);

            // 2.
            // Loop over each match.
            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                LinkItem i = new LinkItem();

                // 3.
                // Get href attribute.
                Match m3 = Regex.Match(value, @"href=\""(.*?)\""",
                RegexOptions.Singleline);

                if (m3.Success)
                {
                    i.Href = m3.Groups[1].Value.ToString();
                }

                // 4.
                // Remove inner tags from text.
                string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                RegexOptions.Singleline);
                i.UserName = t;

                list.Add(i);
            }
            return list;
        }





    }
}
