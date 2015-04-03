using Microsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections;
using System.Timers;
using QueryMaster;


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
        public string GamePort;
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
        public List<DayZPlayer> playersList = new List<DayZPlayer>();
        public static string tester;
        public static string currentIP;
        static ConcurrentDictionary<string, Server> Servers = new ConcurrentDictionary<string, Server>();
        //static ConcurrentDictionary<string, Server> Players = new ConcurrentDictionary<string, Server>();
        private static System.Timers.Timer PingTimer;
        private static System.Timers.Timer PlayerTimer;
        static int pingLoopInProgress = 0;

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
            dirs = dirs.Where(w => w != dirs[1]).ToArray(); // crashes if there is only 1 profile
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

            PingTimer = new System.Timers.Timer(4000);
            PingTimer.Elapsed += PingTimedEvent;
            PingTimer.Enabled = true;

            PlayerTimer = new System.Timers.Timer(10000);
            PlayerTimer.Elapsed += PlayerTimedEvent;
            PlayerTimer.Enabled = true;
            
        }

        public List<Server> getList()
        {
            return serversList;
        }

        public class Server
        {
            public string ServerName { get; set; }
            public ushort QueryPort  { get; set; }
            public string Game_Port { get; set; }
            public string IP_Address { get; set; }
            public string FullIP_Address { get; set; }
            public DateTime Date { get; set; }
            public string Favorite { get; set; }
            public string Current { get; set; }
            public string PingSpeed { get; set; }
            public string UserCount { get; set; }
            public int UserTotal { get; set; }
            public List<DayZPlayer> playersList { get; set; }
        }

        public class DayZPlayer
        {
            public string Name { get; set; }
            public string FullIP_Address { get; set; }
            
        }

        public class SteamServer
        {
            public string addr { get; set; }
            public int gmsindex { get; set; }
            public string message { get; set; }
            public int appid { get; set; }
            public string gamedir { get; set; }
            public int region { get; set; }
            public bool secure { get; set; }
            public bool lan { get; set; }
            public int gameport { get; set; }
            public int specport { get; set; }
        }

        public class Response
        {
            public bool success { get; set; }
            public List<SteamServer> servers { get; set; }
        }

        public class RootObject
        {
            public Response response { get; set; }
        }

        public void writeServerHistoryList()
        {
            Debug.WriteLine("writeServerHistoryList");
            var profileFileStream = new FileStream(configpath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var profileStremReader = new StreamReader(profileFileStream);
            DayZProfile = profileStremReader.ReadToEnd();
            profileStremReader.Close();
            profileFileStream.Close();
            servername = DayZProfile.Split(new string[] { "lastMPServerName=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
            FullIPAddress = DayZProfile.Split(new string[] { "lastMPServer=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
            GamePort = FullIPAddress.Substring(FullIPAddress.LastIndexOf(':') + 1);
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
                                Server matchCurrent = server_list.FirstOrDefault(x => x.Current == "1");
                                int indexCurrent = server_list.FindIndex(x => x.Current == "1");
                                if (matchCurrent != null)
                                {
                                    matchCurrent.Current = "0";

                                    server_list[indexCurrent] = matchCurrent;
                                }
                                
                                match.Date = DateTime.Now;
                                match.IP_Address = IPAddress;
                                match.FullIP_Address = FullIPAddress;
                                match.Current = "1";
                                match.PingSpeed = "Accessing...";
                                match.UserCount = "Accessing...";
                                match.Favorite = match.Favorite;
                                match.QueryPort = match.QueryPort;
                                match.Game_Port = match.Game_Port;
                                server_list[index] = match;
                                string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                                var fsw = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                                var sw = new StreamWriter(fsw);
                                sw.Write(listjson);
                                sw.Close();
                                fsw.Close();
                            }
                            }
                        else
                        { 
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
                                UserCount = "Accessing...",
                                QueryPort = 0,
                                Game_Port = GamePort,
                            });
                            
                            string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                            var fswadd = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                            var swadd = new StreamWriter(fswadd);
                            swadd.Write(listjson);
                            swadd.Close();
                            fswadd.Close();
                    }

                            readHistoryfile();
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
                            UserCount = "Accessing...",
                            QueryPort = 0,
                            Game_Port = GamePort,
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

        public void removeHistoryfile()
        {
            server_list = getServerList();
            foreach (Server DayZServer in server_list)
            {
                removeServerMemory(DayZServer);
            }
            File.Delete(serverhistorypath);
        }

        public void writeServerMemory(Server DayZServer)
        {
            Debug.WriteLine(" update Server: {0} current: {1}, favorite: {2}", DayZServer.IP_Address, DayZServer.Current, DayZServer.Favorite);
            Servers.AddOrUpdate(DayZServer.IP_Address, DayZServer, (key, existingVal) =>
            {
                // If this delegate is invoked, then the key already exists.
                try
                {
                    existingVal.Current = DayZServer.Current;
                    existingVal.Favorite = DayZServer.Favorite;
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
            dzServer.UserCount = DayZServer.UserCount;
            dzServer.Game_Port = DayZServer.Game_Port;
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

        public Server userList(string ip)
        {
            Server matchPlayers = serversList.FirstOrDefault(x => x.IP_Address == ip);
            int indexCurrent = server_list.FindIndex(x => x.IP_Address == ip);
            if (matchPlayers != null)
            {
                return matchPlayers;
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


        public void deleteServerHistory()
        {

              try
                {
                    server_list.Clear();
                    serversList.Clear();
                    Servers.Clear();
                    File.Delete(serverhistorypath);

                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Exception" + e);
            }
            
        }

        void PingTimedEvent(Object source, ElapsedEventArgs e)
        {
            Interlocked.Increment(ref pingLoopInProgress);
            if (pingLoopInProgress == 1)
            {
                lock (server_list)
                {
                    try
                    {
                        getPing();
                    }
                    catch (Exception err)
                    {
                        Debug.WriteLine("The process failed: {0}", err.ToString());
                    }
                    //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
                }
            }
            else
            {
                Debug.WriteLine("!!!!!!!!!!!! PING PROCESS ALREADY RUNNING !!!!!!!!!!!!");
            }
            Interlocked.Decrement(ref pingLoopInProgress);
        }

        void PlayerTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                getPlayer();
            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }
            //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        }


        public void getPing()
        {
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
                Player(DayZServer);
            }
        }

        public void getPlayer()
        {
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
                Player(DayZServer);
            }
        }

        public void Pinger(string IP)
        {
            if (IP.Length == 0)
                throw new ArgumentException("Ping needs a host or IP Address.");

            string who = IP;
            //AutoResetEvent waiter = new AutoResetEvent(false);
            Ping pingSender = new Ping();
            pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 12000;
            PingOptions options = new PingOptions(64, true);
            //Console.WriteLine("Time to live: {0}", options.Ttl);
            //Console.WriteLine("Don't fragment: {0}", options.DontFragment);
            pingSender.SendAsync(who, timeout, buffer, options); //, waiter);
        }

        public void Player(Server DayZServer)
        {

            if (DayZServer.QueryPort == 0)
            {
                WebClient webClient = new WebClient();
                webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(PlayerCompletedCallback);
                //string strUrl = "http://cache.www.gametracker.com/components/html0/?host=" + FullIP + "&currentPlayersHeight=300&showCurrPlayers=1";
                string strUrl = "http://api.steampowered.com/ISteamApps/GetServersAtAddress/v1?addr=" + DayZServer.IP_Address + "&format=json";
                //byte[] reqHTML;
                Uri uri = new Uri(strUrl);
                webClient.DownloadDataAsync(uri);
            }
            else
            {
                QueryMaster.Server server = ServerQuery.GetServerInstance(EngineType.Source, DayZServer.IP_Address, DayZServer.QueryPort);
                QueryMaster.ServerInfo info = server.GetInfo();

                System.Collections.ObjectModel.ReadOnlyCollection<Player> players = server.GetPlayers();
                List<DayZPlayer> listZ = new List<DayZPlayer>();
                foreach (Player Z in players)
                {

                    DayZPlayer i = new DayZPlayer();
                    i.Name = Z.Name;
                    i.FullIP_Address = DayZServer.FullIP_Address;

                    listZ.Add(i);
                    //Console.WriteLine("Name : " + Z.Name + "\nScore : " + Z.Score + "\nTime : " + Z.Time);
                }



                Servers.AddOrUpdate(DayZServer.IP_Address, DayZServer, (key, existingVal) =>
                {
                    // If this delegate is invoked, then the key already exists.
                    try
                    {
                        existingVal.UserTotal = players.Count;
                        existingVal.PingSpeed = info.Ping.ToString();
                        existingVal.playersList = listZ;
                        existingVal.UserCount = players.Count.ToString() + "/" + info.MaxPlayers;
                        return existingVal;

                    }
                    catch (ArgumentException err)
                    {
                        Console.WriteLine("Exception" + err);
                        existingVal.UserTotal = players.Count;
                        existingVal.PingSpeed = info.Ping.ToString();
                        existingVal.playersList = listZ;
                        existingVal.UserCount = players.Count.ToString() + "/" + info.MaxPlayers;
                        return existingVal;
                    }
                });

                        serversList = Servers.Values.ToList() as List<Server>;
            }



        }



        private static void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("Ping canceled.");
            }

            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(e.Error.ToString());
            }
            else
            {
                PingReply reply = e.Reply;
                DisplayReply(reply);
            }

            //((AutoResetEvent)e.UserState).Set();
        }

        private static void PlayerCompletedCallback(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("Steam call canceled.");
            }

            if (e.Error != null)
            {
                Console.WriteLine("Steam call failed.");
                Console.WriteLine(e.Error.ToString());
            }
            else
            {
                byte[] data = (byte[])e.Result;
                string result = System.Text.Encoding.UTF8.GetString(data);
                RootObject rootObject = new RootObject();
                rootObject = JsonConvert.DeserializeObject<RootObject>(result);

                Console.WriteLine(rootObject.response.servers);
                List<SteamServer> steamservers = rootObject.response.servers;
                List<Server> compareList = new List<Server>();
                DataManager dm = new DataManager();
                compareList = dm.getServerList();

                if (steamservers != null) 
                    foreach (SteamServer steamServer in steamservers)
                    {

                        string serverip = steamServer.addr.Substring(0, steamServer.addr.IndexOf(":", StringComparison.Ordinal));
                        string queryport = steamServer.addr.Substring(steamServer.addr.LastIndexOf(':') + 1);
                        string steamgameport = steamServer.gameport.ToString();
                        ushort queryportnum = ushort.Parse(queryport);
                        QueryMaster.Server server = ServerQuery.GetServerInstance(EngineType.Source, serverip, queryportnum);
                        QueryMaster.ServerInfo info = server.GetInfo();
                        //Server matchCurrent = compareList.FirstOrDefault((x, y) => x.Game_Port == steamgameport);
                        Server matchCurrent = compareList.FirstOrDefault(p => p.IP_Address == serverip && p.Game_Port == steamgameport);
                        int indexCurrent = compareList.FindIndex(p => p.IP_Address == serverip && p.Game_Port == steamgameport);
                        if (matchCurrent != null)
                        {
                            matchCurrent.Date = matchCurrent.Date;
                            matchCurrent.IP_Address = matchCurrent.IP_Address;
                            matchCurrent.FullIP_Address = matchCurrent.FullIP_Address;
                            matchCurrent.Current = matchCurrent.Current;


                            matchCurrent.Favorite = matchCurrent.Favorite;
                            matchCurrent.QueryPort = queryportnum;

                            compareList[indexCurrent] = matchCurrent;


                            string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                            string path = System.IO.Path.Combine(appDataPath, "DayZServer");
                            string serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
                            string listjson = JsonConvert.SerializeObject(compareList.ToArray());
                            var fswadd = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                            var swadd = new StreamWriter(fswadd);
                            swadd.Write(listjson);
                            swadd.Close();
                            fswadd.Close();
                            dm.readHistoryfile();


                            System.Collections.ObjectModel.ReadOnlyCollection<Player> players = server.GetPlayers();
                            List<DayZPlayer> listZ = new List<DayZPlayer>();

                            foreach (Player Z in players)
                            {

                                DayZPlayer i = new DayZPlayer();
                                i.Name = Z.Name;
                                i.FullIP_Address = matchCurrent.FullIP_Address;
                                listZ.Add(i);
                                //Console.WriteLine("Name : " + Z.Name + "\nScore : " + Z.Score + "\nTime : " + Z.Time);
                            }

                            matchCurrent.QueryPort = queryportnum;


                            Servers.AddOrUpdate(matchCurrent.IP_Address, matchCurrent, (key, existingVal) =>
                            {
                                // If this delegate is invoked, then the key already exists.
                                try
                                {
                                    existingVal.UserTotal = players.Count;
                                    existingVal.PingSpeed = info.Ping.ToString();
                                    existingVal.QueryPort = queryportnum;
                                    existingVal.playersList = listZ;
                                    existingVal.UserCount = players.Count.ToString() + "/" + info.MaxPlayers;
                                    return existingVal;

                                }
                                catch (ArgumentException err)
                                {
                                    Console.WriteLine("Exception" + err);
                                    existingVal.UserTotal = players.Count;
                                    existingVal.PingSpeed = info.Ping.ToString();
                                    existingVal.QueryPort = queryportnum;
                                    existingVal.playersList = listZ;
                                    existingVal.UserCount = players.Count.ToString() + "/" + info.MaxPlayers;
                                    return existingVal;
                                }
                            });

                            dm.serversList = Servers.Values.ToList() as List<Server>;



                        }





                        //if (info.Address == steamServer.addr)
                        //{

                        //    break;
                        //}

                    }

              
  

            }    
               

                string URL;

            //    // EXAMPLE     ip=216.244.78.242&
            //    Match m2 = Regex.Match(result, "(?<=ip=).*?(?=&)", RegexOptions.Singleline);
            //    if (m2.Success)
            //    {
            //        URL = m2.ToString();
            //        DisplayResult(result, URL);
            //    }
            //}

            //// Let the main thread resume.
            //// UserToken is the AutoResetEvent object that the main thread  
            //// is waiting for.
            ////((AutoResetEvent)e.UserState).Set();
        }

        public static void DisplayReply(PingReply reply)
        {
            Debug.WriteLine("DisplayReply");
            if (reply == null)
                return;

            //Console.WriteLine("ping status: {0}", reply.Status);
            if (reply.Status == IPStatus.Success)
            {
                //Console.WriteLine("Address: {0}", reply.Address.ToString());
                //Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
               // Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                //Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                //Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                Server dzServer = new Server();
                dzServer.IP_Address = reply.Address.ToString();
                dzServer.PingSpeed = reply.RoundtripTime.ToString();

                Console.WriteLine(" update Server {0} withPing: {1}", dzServer.IP_Address, dzServer.PingSpeed);
                Servers.AddOrUpdate(reply.Address.ToString(), dzServer, (key, existingVal) =>
                {
                    // If this delegate is invoked, then the key already exists.
                    try
                    {
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
                //List<Server> serversList = new List<Server>();
                //serversList = Servers.Values.ToList() as List<Server>;
            }
        }


        //public static void DisplayResult(string result, string IP)
        //{
        //    Debug.WriteLine("DisplayResult");
        //    if (result == null)
        //        return;

        //   // Console.WriteLine("html: {0}", result);

        //    string playerhtml = result;
        //    if (playerhtml.Contains("Players:") && playerhtml.Contains("Rank:"))
        //    {
        //        Match m2 = Regex.Match(playerhtml, "(?<=Online Players).*?(?=JOIN THIS SERVER)", RegexOptions.Singleline);
        //        if (m2.Success)
        //        {
        //            playerhtml = m2.ToString();

        //        }
        //    }
        //    else
        //    {
        //        playerhtml = "<a href=\"https://github.com/DayZServerApp/DayZServer/releases\" target=\"_blank\" > Unavailable </a>";
        //}


        //    List<DayZPlayer> list = new List<DayZPlayer>();

        //    // 1.
        //    // Find all matches in file.
        //    MatchCollection m1 = Regex.Matches(playerhtml, @"(<a.*?>.*?</a>)",
        //        RegexOptions.Singleline);

        //    // 2.
        //    // Loop over each match.
        //    foreach (Match m in m1)
        //    {
        //        string value = m.Groups[1].Value;
                //DayZPlayer i = new DayZPlayer();

                //// 3.
                //// Get href attribute.
                //Match m3 = Regex.Match(value, @"href=\""(.*?)\""",
                //RegexOptions.Singleline);

                //if (m3.Success)
                //{
                //    i.Href = m3.Groups[1].Value.ToString();
                //}

                //// 4.
                //// Remove inner tags from text.
                //string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                //RegexOptions.Singleline);
                //i.Name = t;

                //list.Add(i);

        //    }

        //    int Start, End;
        //    string userCount;
        //    string strStart = "Players:";
        //    string strEnd = "Rank";



        //    if (result.Contains("Players:") && result.Contains("Rank:"))
        //    {
        //        Start = result.IndexOf(strStart, 0) + strStart.Length;
        //        End = result.IndexOf(strEnd, Start);
        //        result = result.Substring(Start, End - Start);
        //        userCount = Regex.Replace(result, @"<[^>]+>|&nbsp;", "").Trim();
        //    }
        //    else
        //    {
        //        userCount = "Unavailable";
        //    }






        //    Server dzServer = new Server();
        //    dzServer.UserCount = userCount;
        //    dzServer.playersList = list;

        //    Console.WriteLine(" update Server: {0} withPlayerslist {1}", dzServer.IP_Address, dzServer.UserCount );
        //    Servers.AddOrUpdate(IP, dzServer, (key, existingVal) =>
        //    {
        //        // If this delegate is invoked, then the key already exists.
        //        try
        //        {
        //            existingVal.UserCount = dzServer.UserCount;
        //            existingVal.playersList = dzServer.playersList;
        //            return existingVal;
        //        }
        //        catch (ArgumentException e)
        //        {
        //            Console.WriteLine("Exception" + e);
        //            existingVal.UserCount = dzServer.UserCount;
        //            existingVal.playersList = dzServer.playersList;
        //            return existingVal;
        //        }
        //    });
        //    List<Server> serversList = new List<Server>();
        //    serversList = Servers.Values.ToList() as List<Server>;

        //}

      

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



        //public static String[] serverInfo(string ip)
        //{

        //    WebClient webClient = new WebClient();

        //    string strUrl = "http://cache.www.gametracker.com/components/html0/?host=" + ip + "&currentPlayersHeight=300&showCurrPlayers=1";
        //    byte[] reqHTML;

        //    //check this
        //    reqHTML = webClient.DownloadData(strUrl);
        //    UTF8Encoding objUTF8 = new UTF8Encoding();
        //    string html = objUTF8.GetString(reqHTML);

        //    int Start, End;
        //    string noHTML;
        //    string strStart = "Players:";
        //    string strEnd = "Rank";



        //    if (html.Contains("Players:") && html.Contains("Rank:"))
        //    {
        //        Start = html.IndexOf(strStart, 0) + strStart.Length;
        //        End = html.IndexOf(strEnd, Start);
        //        html = html.Substring(Start, End - Start);
        //        noHTML = Regex.Replace(html, @"<[^>]+>|&nbsp;", "").Trim();

        //    }
        //    else
        //    {
        //        noHTML = "Unavailable";
        //    }


        //    string[] arr1 = new string[] { noHTML };
        //    return arr1;
        //}

        //public static List<LinkItem> userList(string ip)
        //{

        //    WebClient webClient = new WebClient();

        //    string strUrl = "http://cache.www.gametracker.com/components/html0/?host=" + ip + "&currentPlayersHeight=300&showCurrPlayers=1";
        //    byte[] reqHTML;
        //    reqHTML = webClient.DownloadData(strUrl);
        //    UTF8Encoding objUTF8 = new UTF8Encoding();
        //    string phtml = objUTF8.GetString(reqHTML);


        //    if (phtml.Contains("Players:") && phtml.Contains("Rank:"))
        //    {
        //        Match m2 = Regex.Match(phtml, "(?<=Online Players).*?(?=JOIN THIS SERVER)", RegexOptions.Singleline);
        //        if (m2.Success)
        //        {
        //            phtml = m2.ToString();

        //        }
        //    }
        //    else
        //    {
        //        phtml = "<a href=\"https://github.com/DayZServerApp/DayZServer/releases\" target=\"_blank\" > Unavailable </a>";
        //    }



        //    List<Player> list = new List<Player>();

        //    // 1.
        //    // Find all matches in file.
        //    MatchCollection m1 = Regex.Matches(phtml, @"(<a.*?>.*?</a>)",
        //        RegexOptions.Singleline);

        //    // 2.
        //    // Loop over each match.
        //    foreach (Match m in m1)
        //    {
        //        string value = m.Groups[1].Value;
        //        Player i = new Player();

        //        // 3.
        //        // Get href attribute.
        //        Match m3 = Regex.Match(value, @"href=\""(.*?)\""",
        //        RegexOptions.Singleline);

        //        if (m3.Success)
        //        {
        //            i.Href = m3.Groups[1].Value.ToString();
        //        }

        //        // 4.
        //        // Remove inner tags from text.
        //        string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
        //        RegexOptions.Singleline);
        //        i.UserName = t;

        //        list.Add(i);
        //    }
        //    return list;
        //}





    }
}
