using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using QueryMaster;
using DayZ;



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
        public ObservableConcurrentDictionary<string, Server> Servers = new ObservableConcurrentDictionary<string, Server>();
        //static ConcurrentDictionary<string, Server> Players = new ConcurrentDictionary<string, Server>();
        private static System.Timers.Timer PingTimer;
        static int pingLoopInProgress = 0;
        public QueryMaster.Server server;
        public QueryMaster.ServerInfo info;
        public System.Collections.ObjectModel.ReadOnlyCollection<Player> players;


        public DataManager()
        {
            appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            path = System.IO.Path.Combine(appDataPath, "DayZServer");
            dayzapppath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzapppath.txt");
            dayzpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "steam", "SteamApps", "common", "DayZ", "DayZ.exe");
            serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");

            
            
 
        }

        public void startDataManager()
        {
            defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString();
            dirs = Directory.GetFiles(defaultPath + @"\DayZ", "*.DayZProfile"); // TODO: crashes if DayZ is not loaded
            dirs = dirs.Where(w => w != dirs[1]).ToArray(); // crashes if there is only 1 profile
            configpath = dirs[0];
            DZA dza = new DZA();
            dza.runDayZ();
            
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                if (!File.Exists(dayzapppath))
                {
                    writeAppPath(dayzpath);
                }
            }
           

            readHistoryfile();
           // writeServerHistoryList();
            PingTimer = new System.Timers.Timer(4000);
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
            public ushort QueryPort  { get; set; }
            public string Game_Port { get; set; }
            public string IP_Address { get; set; }
            public string FullIP_Address { get; set; }
            public DateTime Date { get; set; }
            public string Favorite { get; set; }
            public string Current { get; set; }
            public long PingSpeed { get; set; }
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

            bool result = !IPAddress.Any(x => char.IsLetter(x));
            if (!result)
            {
                string domain = IPAddress;
                IPAddress[] ip_Addresses = Dns.GetHostAddresses(domain);
                string ips = string.Empty;
                foreach (IPAddress ipAddress in ip_Addresses)
                {
                    IPAddress = ips;
                    Console.WriteLine("Address from host name: " + IPAddress);
                }
            }
          
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
                                    writeServerMemory(matchCurrent);
                                }
                                
                                match.Date = DateTime.Now;
                                match.IP_Address = IPAddress;
                                match.FullIP_Address = FullIPAddress;
                                match.Current = "1";
                                match.PingSpeed = 1000;
                                match.UserCount = "Accessing...";
                                match.Favorite = match.Favorite;
                                match.QueryPort = match.QueryPort;
                                match.Game_Port = match.Game_Port;
                                match.playersList = null;
                                writeServerMemory(match);
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
                                writeServerMemory(matchCurrent);
                            }

                                Server newserver = new Server();
                                newserver.ServerName = servername;
                                newserver.IP_Address = IPAddress;
                                newserver.FullIP_Address = FullIPAddress;
                                newserver.Date = DateTime.Now;
                                newserver.Favorite = "0";
                                newserver.Current = "1";
                                newserver.PingSpeed = 10000;
                                newserver.UserCount = "Accessing...";
                                newserver.QueryPort = 0;
                                newserver.Game_Port = GamePort;
                                newserver.playersList = null;
                            
                            server_list.Add(newserver);
                            writeServerMemory(newserver);
                            
                            string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                            var fswadd = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                            var swadd = new StreamWriter(fswadd);
                            swadd.Write(listjson);
                            swadd.Close();
                            fswadd.Close();
                    }
                            //readHistoryfile();
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
                
                Server newserver = new Server();
                newserver.ServerName = servername;
                newserver.IP_Address = IPAddress;
                newserver.FullIP_Address = FullIPAddress;
                newserver.Date = DateTime.Now;
                newserver.Favorite = "0";
                newserver.Current = "1";
                newserver.PingSpeed = 10000;
                newserver.UserCount = "Accessing...";
                newserver.QueryPort = 0;
                newserver.Game_Port = GamePort;
                newserver.playersList = null;
                server_list.Add(newserver);
                writeServerMemory(newserver);
                
                        string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                        var fswnew = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                        var swnew = new StreamWriter(fswnew);
                        try
                        {
                            swnew.Write(listjson);
                            swnew.Close();
                            fswnew.Close();
                            //readHistoryfile();
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
            Console.WriteLine(" update Server: {0} current: {1}, favorite: {2}", DayZServer.IP_Address, DayZServer.Current, DayZServer.Favorite);
            Servers.UpdateWithNotification(DayZServer.IP_Address, DayZServer);
            

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
            Servers.TryRemoveWithNotification(DayZServer.IP_Address, out DayZServer);
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
                    try
                    {
                        server_list = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception" + e);
                        server_list.Clear();
                        serversList.Clear();
                       // Servers.Clear();
                        File.Delete(serverhistorypath);
                        return null;
                    }
                    
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
                    return null;
                }
            }

            try
            {
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
            catch (Exception e)
            {
                Console.WriteLine("Exception" + e);
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
                    //Servers.Clear();
                    File.Delete(serverhistorypath);

                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Exception" + e);
            }
            
        }

        void PingTimedEvent(Object source, ElapsedEventArgs e)
        {
            //Interlocked.Increment(ref pingLoopInProgress);
            //if (pingLoopInProgress == 1)
            //{
            //    if (server_list != null)
            //        lock (server_list)
            //        {
            //            try
            //            {
            //                if (server_list != null)
                                getPing();
            //            }
            //            catch (Exception err)
            //            {
            //                Debug.WriteLine("The process failed: {0}", err.ToString());
            //            }
            //            //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
            //        }
            //}
            //else
            //{
            //    Debug.WriteLine("!!!!!!!!!!!! PING PROCESS ALREADY RUNNING !!!!!!!!!!!!");
            //}
            //Interlocked.Decrement(ref pingLoopInProgress);
                    }

        //void PlayerTimedEvent(Object source, ElapsedEventArgs e)
        //{
        //    try
        //    {
        //        getPlayer();
        //    }
        //    catch (Exception err)
        //    {
        //        Console.WriteLine("The process failed: {0}", err.ToString());
        //    }
        //    //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        //}


        public void getPing()
        {
            //try
            //{
            //    server_list = getServerList();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Exception" + e);
            //}
 
            Parallel.ForEach(Servers, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = 5 
            }, 
            DayZServer => 
            { 
                Player(DayZServer.Value); 
            });
        }

        //public void getPlayer()
        //{
        //    try
        //    {
        //        server_list = getServerList();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Exception" + e);
        //    }

        //    foreach (Server DayZServer in server_list)
        //    {
        //        Player(DayZServer);
        //    }
        //}

        //public void Pinger(string IP)
        //{
        //    if (IP.Length == 0)
        //        throw new ArgumentException("Ping needs a host or IP Address.");

        //    string who = IP;
        //    //AutoResetEvent waiter = new AutoResetEvent(false);
        //    Ping pingSender = new Ping();
        //    pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);
        //    string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        //    byte[] buffer = Encoding.ASCII.GetBytes(data);
        //    int timeout = 12000;
        //    PingOptions options = new PingOptions(64, true);
        //    //Console.WriteLine("Time to live: {0}", options.Ttl);
        //    //Console.WriteLine("Don't fragment: {0}", options.DontFragment);
        //    pingSender.SendAsync(who, timeout, buffer, options); //, waiter);
        //}

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
                DataManager dm = new DataManager();
                try
                {
                    dm.server = ServerQuery.GetServerInstance(EngineType.Source, DayZServer.IP_Address, DayZServer.QueryPort);
                    dm.info = dm.server.GetInfo();
                    dm.players = dm.server.GetPlayers();
                    
                }
                catch (ArgumentException err)
                {
                    Console.WriteLine("Exception" + err);

                 
                }

                List<DayZPlayer> listZ = new List<DayZPlayer>();
                if (dm.players != null)
                foreach (Player Z in dm.players)
                {

                    DayZPlayer i = new DayZPlayer();
                    i.Name = Z.Name;
                    i.FullIP_Address = DayZServer.FullIP_Address;

                    listZ.Add(i);
                    //Console.WriteLine("Name : " + Z.Name + "\nScore : " + Z.Score + "\nTime : " + Z.Time);
                }



                Servers.UpdateWithNotification(DayZServer.IP_Address, DayZServer);

                        //dm.server.Dispose();
                        //serversList = Servers.Values.ToList() as List<Server>;
            }



        }



        //private static void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        //{
        //    if (e.Cancelled)
        //    {
        //        Console.WriteLine("Ping canceled.");
        //    }

        //    if (e.Error != null)
        //    {
        //        Console.WriteLine("Ping failed:");
        //        Console.WriteLine(e.Error.ToString());
        //    }
        //    else
        //    {
        //        PingReply reply = e.Reply;
        //        DisplayReply(reply);
        //    }

        //    //((AutoResetEvent)e.UserState).Set();
        //}

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
               
                if (steamservers != null && compareList != null) 
                    foreach (SteamServer steamServer in steamservers)
                    {

                      
                        
                        string serverip = steamServer.addr.Substring(0, steamServer.addr.IndexOf(":", StringComparison.Ordinal));
                        string queryport = steamServer.addr.Substring(steamServer.addr.LastIndexOf(':') + 1);
                        string steamgameport = steamServer.gameport.ToString();
                        ushort queryportnum = ushort.Parse(queryport);
                        if (queryportnum == 0) continue;
                        
                        try
                        {

                        
                        dm.server = ServerQuery.GetServerInstance(EngineType.Source, serverip, queryportnum);
                        QueryMaster.ServerInfo info = dm.server.GetInfo();
                        

                        

                        }
                        catch (Exception err)
                        {

                            Console.WriteLine("QueryMaster" + err); 
                            continue;
                        }
                        //Server matchCurrent = compareList.FirstOrDefault((x, y) => x.Game_Port == steamgameport);
                        if (compareList == null) continue; 
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
                            dm.writeServerMemory(matchCurrent);

                            string appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                            string path = System.IO.Path.Combine(appDataPath, "DayZServer");
                            string serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
                            string listjson = JsonConvert.SerializeObject(compareList.ToArray());
                            var fswadd = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                            var swadd = new StreamWriter(fswadd);
                            swadd.Write(listjson);
                            swadd.Close();
                            fswadd.Close();
                            //dm.readHistoryfile();
                            
                            List<DayZPlayer> listZ = new List<DayZPlayer>();
                            try
                            {
                                
                                dm.players = dm.server.GetPlayers();
                                
                                
                            }
                            catch (ArgumentException err)
                            {
                                Console.WriteLine("Exception" + err);
                                continue;
                            }

                            if ( dm.players != null)
                            foreach (Player Z in dm.players)
                            {

                                DayZPlayer i = new DayZPlayer();
                                i.Name = Z.Name;
                                i.FullIP_Address = matchCurrent.FullIP_Address;
                                listZ.Add(i);
                                //Console.WriteLine("Name : " + Z.Name + "\nScore : " + Z.Score + "\nTime : " + Z.Time);
                            }

                            matchCurrent.QueryPort = queryportnum;
                            if (dm.info == null) continue;
                            dm.Servers.UpdateWithNotification(matchCurrent.IP_Address, matchCurrent);
                            //dm.server.Dispose();
                            dm.serversList = dm.Servers.Values.ToList() as List<Server>;

                        }





                        //if (info.Address == steamServer.addr)
                        //{

                        //    break;
                        //}

                    }

              
  

            }    
               

               
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

        public void getGTList()
        {
            WebClient webClient = new WebClient();
            webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(GTCompletedCallback);
            string strUrl = "http://www.gametracker.com/search/dayz/?&searchipp=50#search";
            //byte[] reqHTML;
            Uri uri = new Uri(strUrl);
            webClient.DownloadDataAsync(uri);
        }

        private static void GTCompletedCallback(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("GT call canceled.");
            }

            if (e.Error != null)
            {
                Console.WriteLine("GT call failed.");
                Console.WriteLine(e.Error.ToString());
            }
            else
            {
                byte[] data = (byte[])e.Result;
                string result = System.Text.Encoding.UTF8.GetString(data);

                Match m2 = Regex.Match(result, "(?<=Server Map).*?(?=Server Map)", RegexOptions.Singleline);
                    if (m2.Success)
                    {
                        result = m2.ToString();

                    }
                else
                {
                    result = "<td><a href=\"/search/dayz/600/\"><img src=\"/images/game_icons16/dayz.png\" alt=\"DAYZ\"/></a></td><td><a class=\"c03serverlink\" href=\"/server_info/216.244.78.242:2802/\">\\DG Clan - SUPERSHARD #1 \\ Unlocked \\ High Loot \\ 24/7 day</a><a href=\"javascript:showPopupExternalLink('gt://joinGame:game=dayz&amp;ip=216.244.78.242&amp;port=2802');\"><img src=\"/images/global/btn_join.png\" alt=\"Join\"/></a></td><td>5/50</td><td></td><td><a href=\"/search/dayz/US/\"><img src=\"/images/flags/us.gif\" alt=\"\" class=\"item_16x11\"/></a></td><td><span class=\"ip\">216.244.78.242</span><span class=\"port\">:2802</span></td><td>DayZ_Auto</td>";
                }

                    List<DayZPlayer> GTlist = new List<DayZPlayer>();

                    // 1.
                    // Find all matches in file.
                    MatchCollection m1 = Regex.Matches(result, @"(<a.*?>.*?</a>)",
                        RegexOptions.Singleline);

                    // 2.
                    // Loop over each match.
                    foreach (Match m in m1)
                    {
                        string value = m.Groups[1].Value;
                        DayZPlayer i = new DayZPlayer();

                        // 3.
                        // Get href attribute.
                        Match m3 = Regex.Match(value, "href=\"/server_info/" + "(.*?)" + "/\"",
                        RegexOptions.Singleline);

                        if (m3.Success)
                        {
                            i.FullIP_Address = m3.Groups[1].Value.ToString();
                        }

                        // 4.
                        // Remove inner tags from text.
                        string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                        RegexOptions.Singleline);
                        i.Name = t;
                        if (i.FullIP_Address == null) continue;
                        GTlist.Add(i);

                    }
                    Console.WriteLine("Servers" + GTlist);
                    DataManager dm = new DataManager();
                    foreach (DayZPlayer GTServer in GTlist)
                    {
                        bool serverdata = !GTServer.FullIP_Address.Any(x => char.IsLetter(x));
                        if (!serverdata)
                          {
                            continue;
                          }
                        
                        dm.writeGTList(GTServer.Name, GTServer.FullIP_Address);
                    }
            }

        }

        public void writeGTList(string serverName, string fullIPAdress)
        {
            string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString();
            string[] dirs = Directory.GetFiles(defaultPath + @"\DayZ", "*.DayZProfile"); // TODO: crashes if DayZ is not loaded
            dirs = dirs.Where(w => w != dirs[1]).ToArray(); // crashes if there is only 1 profile
            configpath = dirs[0];
            var profileFileStream = new FileStream(configpath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var profileStremReader = new StreamReader(profileFileStream);
            DayZProfile = profileStremReader.ReadToEnd();
            profileStremReader.Close();
            profileFileStream.Close();
            servername = serverName;
            FullIPAddress = fullIPAdress;
            GamePort = FullIPAddress.Substring(FullIPAddress.LastIndexOf(':') + 1);
            IPAddress = FullIPAddress.Substring(0, FullIPAddress.LastIndexOf(":"));
            version = DayZProfile.Split(new string[] { "version=" }, StringSplitOptions.None)[1].Split(new string[] { ";" }, StringSplitOptions.None)[0].Trim();

            //bool result = !IPAddress.Any(x => char.IsLetter(x));
            //if (!result)
            //{

            //    Console.WriteLine(IPAddress);

            //    IPAddress[] IPAddressResolve = Dns.GetHostAddresses(IPAddress);

            //    foreach (IPAddress theaddress in IPAddressResolve)
            //    {
            //        Console.WriteLine(theaddress.ToString());
            //        IPAddress = theaddress.ToString();
            //    }
            //}

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
                            match.Current = "0";

                        }   else
                        {
                            match.Current = "1";
                        }
                            
                            match.Date = DateTime.Now;
                            match.IP_Address = IPAddress;
                            match.FullIP_Address = FullIPAddress;
                            match.PingSpeed = 1000;
                            match.UserCount = "Accessing...";
                            match.Favorite = match.Favorite;
                            match.QueryPort = match.QueryPort;
                            match.Game_Port = match.Game_Port;
                            match.playersList = null;
                            server_list[index] = match;
                            string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                            var fsw = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                            var sw = new StreamWriter(fsw);
                            sw.Write(listjson);
                            sw.Close();
                            fsw.Close();
                    }
                    else
                    {


                        Server newserver = new Server();
                        newserver.ServerName = servername;
                        newserver.IP_Address = IPAddress;
                        newserver.FullIP_Address = FullIPAddress;
                        newserver.Date = DateTime.Now;
                        newserver.Favorite = "0";
                        newserver.Current = "0";
                        newserver.PingSpeed = 10000;
                        newserver.UserCount = "Accessing...";
                        newserver.QueryPort = 0;
                        newserver.Game_Port = GamePort;
                        newserver.playersList = null;
                        writeServerMemory(newserver);
                        server_list.Add(newserver);

                        string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                        var fswadd = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                        var swadd = new StreamWriter(fswadd);
                        swadd.Write(listjson);
                        swadd.Close();
                        fswadd.Close();
                    }

                    //readHistoryfile();
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


                Server newserver = new Server();
                newserver.ServerName = servername;
                newserver.IP_Address = IPAddress;
                newserver.FullIP_Address = FullIPAddress;
                newserver.Date = DateTime.Now;
                newserver.Favorite = "0";
                newserver.Current = "1";
                newserver.PingSpeed = 10000;
                newserver.UserCount = "Accessing...";
                newserver.QueryPort = 0;
                newserver.Game_Port = GamePort;
                newserver.playersList = null;
                writeServerMemory(newserver);
                server_list.Add(newserver);

                string listjson = JsonConvert.SerializeObject(server_list.ToArray());
                var fswnew = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                var swnew = new StreamWriter(fswnew);
                try
                {
                    swnew.Write(listjson);
                    swnew.Close();
                    fswnew.Close();
                   // readHistoryfile();
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Exception" + e);
                }
            }
        }



        //public static void DisplayReply(PingReply reply)
        //{
        //    Debug.WriteLine("DisplayReply");
        //    if (reply == null)
        //        return;

        //    //Console.WriteLine("ping status: {0}", reply.Status);
        //    if (reply.Status == IPStatus.Success)
        //    {
        //        //Console.WriteLine("Address: {0}", reply.Address.ToString());
        //        //Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
        //       // Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
        //        //Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
        //        //Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
        //        Server dzServer = new Server();
        //        dzServer.IP_Address = reply.Address.ToString();
        //        dzServer.PingSpeed = reply.RoundtripTime.ToString();

        //        Console.WriteLine(" update Server {0} withPing: {1}", dzServer.IP_Address, dzServer.PingSpeed);
        //        Servers.AddOrUpdate(reply.Address.ToString(), dzServer, (key, existingVal) =>
        //        {
        //            // If this delegate is invoked, then the key already exists.
        //            try
        //            {
        //                existingVal.PingSpeed = dzServer.PingSpeed;

        //                return existingVal;
        //            }
        //            catch (ArgumentException e)
        //            {
        //                Console.WriteLine("Exception" + e);
        //                existingVal.PingSpeed = dzServer.PingSpeed;
        //                return existingVal;
        //            }
        //        });
        //        //List<Server> serversList = new List<Server>();
        //        //serversList = Servers.Values.ToList() as List<Server>;
        //    }
        //}


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


            //List<DayZPlayer> list = new List<DayZPlayer>();

            //// 1.
            //// Find all matches in file.
            //MatchCollection m1 = Regex.Matches(playerhtml, @"(<a.*?>.*?</a>)",
            //    RegexOptions.Singleline);

            //// 2.
            //// Loop over each match.
            //foreach (Match m in m1)
            //{
            //    string value = m.Groups[1].Value;
            //    DayZPlayer i = new DayZPlayer();

            //    // 3.
            //    // Get href attribute.
            //    Match m3 = Regex.Match(value, @"href=\""(.*?)\""",
            //    RegexOptions.Singleline);

            //    if (m3.Success)
            //    {
            //        i.Href = m3.Groups[1].Value.ToString();
            //    }

            //    // 4.
            //    // Remove inner tags from text.
            //    string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
            //    RegexOptions.Singleline);
            //    i.Name = t;

            //    list.Add(i);

            //}

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
