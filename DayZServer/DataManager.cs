﻿using System;
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
using System.Collections.Concurrent;


namespace DayZServer
{

    public class DataManager
    {
        public string defaultPath;
        public string[] dirs;
        public string configpath;
        public string DayZProfile;
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
        public List<Server> serversList = new List<Server>();
        public static string tester;
        public static string currentIP;
        static ConcurrentDictionary<string, Server> Servers = new ConcurrentDictionary<string, Server>();
        private static System.Timers.Timer PingTimer;

        public DataManager()
        {
            appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            path = System.IO.Path.Combine(appDataPath, "DayZServer");
            dayzapppath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzapppath.txt");
            dayzpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "steam", "SteamApps", "common", "DayZ", "DayZ.exe");
            serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
            //Console.WriteLine("Server History Path: " + serverhistorypath);
        }

        public void startDataManager()
        {
            defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString();
            dirs = Directory.GetFiles(defaultPath + @"\DayZ", "*.DayZProfile"); // TODO: crashes if DayZ is not loaded
            dirs = dirs.Where(w => w != dirs[1]).ToArray(); // TODO: crashes if there is only 1 profile
            configpath = dirs[0];

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                if (!File.Exists(dayzapppath))
                {
                    writeAppPath(dayzpath);
                }
            }

            writeServerHistoryList();

            PingTimer = new System.Timers.Timer(10000);
            PingTimer.Elapsed += PingTimedEvent;
            PingTimer.Enabled = true;
        }

        public List<Server> getList()
        {
            return serversList;
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
            static ConcurrentDictionary<string, LinkItem> ConcurrentDictionary { get; set; }
        }

        public class LinkItem
        {
            public string Href { get; set; }
            public string UserName { get; set; }
        }





        public void writeServerHistoryList()
        {
            var profileFileStream = new FileStream(configpath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var profileStremReader = new StreamReader(profileFileStream);
            DayZProfile = profileStremReader.ReadToEnd();
            profileStremReader.Close();
            profileFileStream.Close();
            servername = DayZProfile.Split(new string[] { "lastMPServerName=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
            FullIPAddress = DayZProfile.Split(new string[] { "lastMPServer=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
            IPAddress = FullIPAddress.Substring(0, FullIPAddress.LastIndexOf(":"));
            version = DayZProfile.Split(new string[] { "version=" }, StringSplitOptions.None)[1].Split(new string[] { ";" }, StringSplitOptions.None)[0].Trim();

            if (File.Exists(serverhistorypath))
            {
                string temphistory;
                try
                {
                    var fs = new FileStream(serverhistorypath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var sr = new StreamReader(fs);
                    temphistory = sr.ReadToEnd();
                    sr.Close();
                    fs.Close();
                    server_list = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                    Server match = server_list.FirstOrDefault(x => x.IP_Address == IPAddress);
                    int index = server_list.FindIndex(x => x.IP_Address == IPAddress);

                    if (match != null)
                    {
                        if (match.Current == "0")
                        {
                            match.Date = DateTime.Now;
                            match.IP_Address = IPAddress;
                            match.FullIP_Address = FullIPAddress;
                            match.Current = "1";
                            match.PingSpeed = "Accessing...";
                            match.Favorite = match.Favorite;
                            server_list[index] = match;
                            string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                            var fsw = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                            var sw = new StreamWriter(fsw);
                            sw.Write(listjson);
                            sw.Close();
                            fsw.Close();
                            readHistoryfile();
                        }
                        else
                        {
                            readHistoryfile();
                        }

                    }
                    else
                    {  //******Need to look at this due to saving already visited and current as current
                        Server matchCurrent = server_list.FirstOrDefault(x => x.Current == "1");
                        int indexCurrent = server_list.FindIndex(x => x.Current == "1");
                        if (matchCurrent != null)
                        {
                            matchCurrent.Current = "0";

                            server_list[indexCurrent] = matchCurrent;
                        }

                        server_list.Add(new Server()
                        {
                            ServerName = servername,
                            IP_Address = IPAddress,
                            FullIP_Address = FullIPAddress,
                            Date = DateTime.Now,
                            Favorite = "0",
                            Current = "1",
                            PingSpeed = "Accessing...",
                        });

                        string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                        var fswadd = new FileStream(serverhistorypath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                        var swadd = new StreamWriter(fswadd);
                        swadd.Write(listjson);
                        swadd.Close();
                        fswadd.Close();
                        readHistoryfile();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }
            else
            {
                if (server_list == null)
                {
                    server_list = new List<Server>();
                }
                server_list.Add(new Server()
                {
                    ServerName = servername,
                    IP_Address = IPAddress,
                    FullIP_Address = FullIPAddress,
                    Date = DateTime.Now,
                    Favorite = "0",
                    Current = "1",
                    PingSpeed = "Accessing...",
                });
                string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                var fswnew = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var swnew = new StreamWriter(fswnew);
                try
                {
                    swnew.Write(listjson);
                    swnew.Close();
                    fswnew.Close();
                    readHistoryfile();
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }
        }

        public void readHistoryfile()
        {
            server_list = getServerList();
            foreach (Server DayZServer in server_list)
            {
                writeServerMemory(DayZServer);
            }
        }

        public void writeServerMemory(Server DayZServer)
        {
            Servers.AddOrUpdate(DayZServer.IP_Address, DayZServer, (key, existingVal) =>
            {
                //Console.WriteLine(" writeServerMemory:Servers1 " + Servers.Count);
                // If this delegate is invoked, then the key already exists.
                try
                {
                    if (DayZServer.IP_Address == existingVal.IP_Address)
                    {
                        existingVal.Current = DayZServer.Current;
                        existingVal.Favorite = DayZServer.Favorite;
                    }

                    return existingVal;
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Exception" + e);
                    existingVal.Current = DayZServer.Current;
                    existingVal.Favorite = DayZServer.Favorite;
                    return existingVal;
                }
            });

            serversList = Servers.Values.ToList() as List<Server>;
        }

        public void removeServerMemory(Server DayZServer)
        {
            Server dzServer = new Server();
            dzServer.Date = DayZServer.Date;
            dzServer.ServerName = DayZServer.ServerName;
            dzServer.IP_Address = DayZServer.IP_Address;
            dzServer.FullIP_Address = DayZServer.FullIP_Address;
            dzServer.Current = DayZServer.Current;
            dzServer.Favorite = DayZServer.Favorite;
            dzServer.PingSpeed = DayZServer.PingSpeed;
            Servers.TryRemove(DayZServer.IP_Address, out DayZServer);
            serversList = Servers.Values.ToList() as List<Server>;
        }

        public List<Server> getServerList()
        {
            if (File.Exists(serverhistorypath))
            {
                string temphistory;

                if (server_list == null)
                {
                    server_list = new List<Server>();
                }

                if (server_list.Count == 0)
                {
                    var fs = new FileStream(serverhistorypath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var sr = new StreamReader(fs);
                    temphistory = sr.ReadToEnd();
                    sr.Close();
                    fs.Close();
                    server_list = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                }
                try
                {
                    return server_list;
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
            if (server_list.Count == 0)
            {
                try
                {
                    server_list = getServerList();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                    return null;
                }
            }

            Server matchCurrent = server_list.FirstOrDefault(x => x.Current == "1");
            int indexCurrent = server_list.FindIndex(x => x.Current == "1");
            if (matchCurrent != null)
            {
                return matchCurrent;
            }
            else
            {
                return null;
            }
        }

        public Server getServerByIP(string ip)
        {
            if (server_list.Count == 0)
            {
                try
                {
                    server_list = getServerList();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                    return null;
                }
            }

            Server matchCurrent = server_list.FirstOrDefault(x => x.IP_Address == ip);
            if (matchCurrent != null)
            {
                return matchCurrent;
            }
            else
            {
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
            catch (Exception e)
            {
                Console.WriteLine("Exception" + e);
            }
        }

        String readAppPath(string dayzapppath)
        {
            if (File.Exists(dayzapppath))
            {
                try
                {
                    using (StreamReader sreader = new StreamReader(File.OpenRead(dayzapppath)))
                    {
                        String line = sreader.ReadToEnd();
                        sreader.Close();
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
            if (server_list == null)
            {
                server_list = new List<Server>();
            }
            if (server_list.Count == 0)
            {
                try
                {
                    server_list = getServerList();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }

            Server match = server_list.FirstOrDefault(x => x.FullIP_Address == favoriteServer);
            int index = server_list.FindIndex(x => x.FullIP_Address == favoriteServer);

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

                server_list[index] = match;
                string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                var fsw = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var sw = new StreamWriter(fsw);
                try
                {
                    sw.Write(listjson);
                    sw.Close();
                    fsw.Close();
                    writeServerMemory(match);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }
        }

        public void deleteServer(string deleteServer)
        {
            if (server_list == null)
            {
                server_list = new List<Server>();
            }

            if (server_list.Count == 0)
            {
                try
                {
                    server_list = getServerList();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }

            Server match = server_list.FirstOrDefault(x => x.ServerName == deleteServer);
            int index = server_list.FindIndex(x => x.ServerName == deleteServer);

            if (match != null)
            {
                server_list.RemoveAt(index);
                string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                var fsw = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var sw = new StreamWriter(fsw);
                try
                {
                    sw.Write(listjson);
                    sw.Close();
                    fsw.Close();
                    removeServerMemory(match);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }
        }



        void PingTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                getPing();
            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }
            //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        }


        public void getPing()
        {

            //TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            //CancellationToken token = new CancellationToken();
            try
            {
                server_list = getServerList();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception" + e);
            }
            foreach (Server DayZServer in server_list)
            {

                Pinger(DayZServer.IP_Address);
                //Task.Factory.StartNew( () => DoWork(DayZServer), token, TaskContinuationOptions.None, scheduler);
                // Task.Factory.StartNew(() => Pinger(DayZServer.FullIP_Address), token);
            }

            //foreach (KeyValuePair<string, Server> pair in Servers)
            //{
            //    Console.WriteLine(Servers);


            //   // Task.Factory.StartNew([DoWork(pair.Value.FullIP_Address])).ContinueWith(w => tester = "All done", token, TaskContinuationOptions.None, scheduler);

            //}



            //TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            ////We don't use the Cancel Token in this example but it's required
            //CancellationToken token = new CancellationToken();

            ////This starts the work on a new thread calling the DoWork method.
            ////Once the work is done it sets txtResult.Text = "All done". Notice we need
            ////to give the ContinueWith statement the scheduler. This is how it knows what
            ////thread to marshal the ContinueWith block to. So it executes it on our UI thread.
            ////If you remove that part it will try to call it from a different thread and
            ////an exception will be thrown.

            //DataManager dm = new DataManager();
            //tester = "working...";
            //currentIP = reply.Address.ToString();
            //Task.Factory.StartNew(dm.DoWork).ContinueWith(w => tester = "All done", token, TaskContinuationOptions.None, scheduler);
        }

        public void Pinger(string fullIP)
        {
            if (fullIP.Length == 0)
                throw new ArgumentException("Ping needs a host or IP Address.");

            string who = fullIP;
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
            waiter.WaitOne();
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
                Console.WriteLine("Address: {0}", reply.Address.ToString());
                Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                Server dzServer = new Server();
                dzServer.IP_Address = reply.Address.ToString();
                dzServer.PingSpeed = reply.RoundtripTime.ToString();

                Servers.AddOrUpdate(reply.Address.ToString(), dzServer, (key, existingVal) =>
                {
                    Console.WriteLine(" writeServerHistoryList:Servers1 " + Servers.Count.ToString());
                    // If this delegate is invoked, then the key already exists.
                    try
                    {
                        if (dzServer.IP_Address == existingVal.IP_Address)
                            existingVal.PingSpeed = dzServer.PingSpeed;

                        return existingVal;
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine("Exception" + e);
                        existingVal.PingSpeed = dzServer.PingSpeed;
                        return existingVal;
                    }
                });
                List<Server> serversList = new List<Server>();
                serversList = Servers.Values.ToList() as List<Server>;
            }
        }

        //public void DoWork(Server DayZServer)
        //{

        //    // List<Server> server_list_temp = new List<Server>();
        //    // if (server_list_temp.Count == 0)
        //    //{

        //    try
        //    {

        //        string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        //        string path = System.IO.Path.Combine(appDataPath, "DayZServer");
        //        string serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");

        //        if (File.Exists(serverhistorypath))
        //        {
        //            //server_list = getServerList();
        //            var fs = new FileStream(serverhistorypath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        //            var sr = new StreamReader(fs);
        //            string temphistory = sr.ReadToEnd();
        //            sr.Close();
        //            fs.Close();
        //            server_list = JsonConvert.DeserializeObject<List<Server>>(temphistory);
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Exception" + e);
        //    }

        //    // }


        //    Server match = server_list.FirstOrDefault(x => x.IP_Address == currentIP);
        //    int index = server_list.FindIndex(x => x.IP_Address == currentIP);

        //    if (match != null)
        //    {
        //        match.linkItem = userList(match.FullIP_Address);
        //        match.UserCount = serverInfo(match.FullIP_Address)[0];
        //        Server replacement = match;
        //        server_list[index] = replacement;

        //        string listjson = JsonConvert.SerializeObject(server_list.ToArray());

        //        try
        //        {
        //            using (StreamWriter sw = File.CreateText(serverhistorypath))
        //            {
        //                sw.Write(listjson);
        //                sw.Close();
        //            }

        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("Exception" + e);
        //        }



        //        //Console.WriteLine("Address: {0}", reply.Address.ToString());
        //        //Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
        //        //Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
        //        //Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
        //        //Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);


        //    }



        //    //Thread.Sleep(3000);
        //}



        //public void DisplayReply(PingReply reply)
        //{
        //    if (reply == null)
        //        return;



        //    Console.WriteLine("ping status: {0}", reply.Status);
        //    if (reply.Status == IPStatus.Success)
        //    {


        //        string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        //        string path = System.IO.Path.Combine(appDataPath, "DayZServer");
        //        string serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");

        //        if (File.Exists(serverhistorypath))
        //        {

        //            server_list = getServerList();


        //            Server match = server_list.FirstOrDefault(x => x.IP_Address == reply.Address.ToString());
        //            int index = server_list.FindIndex(x => x.IP_Address == reply.Address.ToString());

        //            if (match != null)
        //            {
        //                match.PingSpeed = reply.RoundtripTime.ToString();
        //                Server replacement = match;
        //                server_list[index] = replacement;
        //                string listjson = JsonConvert.SerializeObject(server_list.ToArray());

        //                using (StreamWriter sw = File.CreateText(serverhistorypath))
        //                {
        //                    sw.Write(listjson);
        //                    sw.Close();
        //                }
        //            }



        //            //Console.WriteLine("Address: {0}", reply.Address.ToString());
        //            //Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
        //            //Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
        //            //Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
        //            //Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);


        //        }
        //    }
        //}



        public static String[] serverInfo(string ip)
        {

            WebClient webClient = new WebClient();

            string strUrl = "http://cache.www.gametracker.com/components/html0/?host=" + ip + "&currentPlayersHeight=300&showCurrPlayers=1";
            byte[] reqHTML;

            //check this
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
