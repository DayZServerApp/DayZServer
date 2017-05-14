﻿using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using QueryMaster;
using System.Threading;
using System.Security.Permissions;
using SteamKit2.Unified.Internal;

namespace DayZServer
{
    public class DataManager
    {
        public string defaultPath;
        public string[] dirs;
        public string configpath;
        public string filepathProfile;
        public string directorypathProfile;
        public string filepathHistory;
        public string directorypathHistory;
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
        public string temphistory;
        public int pingIndex;
        public List<Server> server_list;
        public List<Server> serverHistory;
        public List<Server> serversList = new List<Server>();
        public List<DayZPlayer> playersList = new List<DayZPlayer>();
        public Server profileServer = new Server();
        public static string tester;
        public static string currentIP;
        public ObservableConcurrentDictionary<string, Server> Servers = new ObservableConcurrentDictionary<string, Server>();
        //static ConcurrentDictionary<string, Server> Players = new ConcurrentDictionary<string, Server>();
        private static System.Timers.Timer PingTimer;
        static int pingLoopInProgress = 0;
        //public QueryMaster.ServerInfo info;
        



        public DataManager()
        {
            appDataPath = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            path = System.IO.Path.Combine(appDataPath, "DayZServer");
            dayzapppath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzapppath.txt");
            dayzpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "steam", "SteamApps", "common", "DayZ", "DayZ.exe");
            serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path, "dayzhistory.txt");
            filepathHistory = new FileInfo(serverhistorypath).Name;
            directorypathHistory = new FileInfo(serverhistorypath).Directory.FullName;
            //Console.WriteLine("Server History Path: " + serverhistorypath);
        }

        public void startDataManager()
        {
            defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString();
            dirs = Directory.GetFiles(defaultPath + @"\DayZ", "*.DayZProfile"); // TODO: crashes if DayZ is not loaded
            //dirs = dirs.Where(w => w != dirs[1]).ToArray(); // crashes if there is only 1 profile
            configpath = dirs[1];
            filepathProfile = new FileInfo(configpath).Name;
            directorypathProfile = new FileInfo(configpath).Directory.FullName;
            ;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                if (!File.Exists(dayzapppath))
                {
                    writeAppPath(dayzpath);
                }
            }


           ProfileCheck();
            

            PingTimer = new System.Timers.Timer(10000);
            PingTimer.Elapsed += PingTimedEvent;
            PingTimer.Enabled = true;

            //WatchProfileChanges();


        }

        public List<Server> getList()
        {
            return serversList;
        }

        public class Server
        {
            public string ServerName { get; set; }
            public ushort QueryPort { get; set; }
            public string Game_Port { get; set; }
            public string IP_Address { get; set; }
            public string FullIP_Address { get; set; }
            public DateTime Date { get; set; }
            public string Favorite { get; set; }
            public string Current { get; set; }
            public long PingSpeed { get; set; }
            public long UserCount { get; set; }
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

        public class PlayerInfo
        {
            /// <summary>
            /// Name of the player. 
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Player's score (usually "frags" or "kills".) 
            /// </summary>
            public long Score { get; set; }
            /// <summary>
            /// Time  player has been connected to the server.(returns TimeSpan instance)
            /// </summary>
            public TimeSpan Time { get; set; }
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



        private async void ProfileCheck()
        {
            
            await Task.Run(() => CheckingProfile());
            await Task.Run(() => CheckingHistory());
            await Task.Run(() => ProfileMatch());
            await Task.Run(() => WatchProfileChanges());
        }

        private async Task CheckingProfile()
        {
            var profileFileStream = new FileStream(configpath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var profileStremReader = new StreamReader(profileFileStream);
            await Task.Run(() => DayZProfile = profileStremReader.ReadToEnd());
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

            profileServer.ServerName = servername;
            profileServer.IP_Address = IPAddress;
            profileServer.FullIP_Address = FullIPAddress;
            profileServer.Date = DateTime.Now;
            profileServer.Favorite = "0";
            profileServer.Current = "1";
            profileServer.PingSpeed = 10000;
            profileServer.UserCount = 0;
            profileServer.QueryPort = 0;
            profileServer.Game_Port = GamePort;
            profileServer.playersList = null;
        }



            private async void HistoryCheck()
            {
                await Task.Run(() => CheckingHistory());
                // Update the UI with results
            }

            private async Task CheckingHistory()
            {
                if (!Directory.Exists(path))
                {
                    try
                    {
                        await Task.Run(() => Directory.CreateDirectory(path));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error Creating DayZServer Directory" + e);
                    }

            }
            else if (!File.Exists(serverhistorypath))
            {
                try
                {
                    await Task.Run(() => writeAppPath(serverhistorypath));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error Creating dayzhistory.txt" + e);
                }
            }
                
                else
                {
                    try
                    {
                        var fs = new FileStream(serverhistorypath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        var sr = new StreamReader(fs);
                        await Task.Run(() => temphistory = sr.ReadToEnd());
                        sr.Close();
                        fs.Close();
                        serverHistory = JsonConvert.DeserializeObject<List<Server>>(temphistory);
                        //serversList = Servers.Values.ToList();
                    foreach (Server dayZServer in serverHistory)
                        {
                            Server match = serversList.FirstOrDefault(x => x.IP_Address == dayZServer.IP_Address);
                            if (match == null) { 
                            ServerToDictionary(dayZServer);
                            }
                        }
                }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error Reading dayzhistory.txt" + e);
                    }
                }
            }



        //need to refactor so there are not two methods here

        private async void ServerToDictionary(Server dayZServer)
        {
            // The await causes the handler to return immediately.
            await System.Threading.Tasks.Task.Run(() => PushData(dayZServer));
            serversList = Servers.Values.ToList();


        }

        private async System.Threading.Tasks.Task PushData(Server dayZServer)
        {
            try
            {
                await Task.Run(() => Servers.UpdateWithNotification(dayZServer.IP_Address, dayZServer));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Pushing Data to Dictionary" + e);
            }

        }


        private async Task ProfileMatch()
        {
            //serversList = Servers.Values.ToList();

            if (serversList.Count != 0)
            {
                foreach (Server dayZServer in serversList)
                {
                    Server match = serversList.FirstOrDefault(x => x.IP_Address == profileServer.IP_Address);
                    Server matchCurrent = serversList.FirstOrDefault(x => x.Current == "1");
                    if (matchCurrent != null)
                    {
                        await Task.Run(() => ServerToDictionary(matchCurrent));
                        //serversList = Servers.Values.ToList();
                    }
                    if (match == null)
                    {
                        if (matchCurrent != null)
                        {
                            matchCurrent.Current = "0";
                        }
                        await Task.Run(() => ServerToDictionary(profileServer));
                        await Task.Run(() => UpdateHistory());
                    }
                    else
                    {
                        await Task.Run(() => ServerToDictionary(match));
                    }

                }
            }
            else
            {
                await Task.Run(() => ServerToDictionary(profileServer));
                await Task.Run(() => UpdateHistory());
            }
        }

        private async Task UpdateHistory()
        {
            List<Server> historyList = Servers.Values.ToList();
            string listjson = JsonConvert.SerializeObject(historyList.ToArray());
            var fsw = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var sw = new StreamWriter(fsw);
            await Task.Run(() => sw.Write(listjson));
            sw.Close();
            fsw.Close();
        }


        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void WatchProfileChanges()
        {

            FileSystemWatcher watcherProfile = new FileSystemWatcher();
            watcherProfile.Path = directorypathProfile;
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            watcherProfile.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            watcherProfile.Filter = filepathProfile;

            // Add event handlers.
            watcherProfile.Changed += new FileSystemEventHandler(OnProfileChanged);
            watcherProfile.Created += new FileSystemEventHandler(OnProfileChanged);
            watcherProfile.Deleted += new FileSystemEventHandler(OnProfileChanged);
            watcherProfile.Renamed += new RenamedEventHandler(OnProfileRenamed);

            // Begin watching.
            watcherProfile.EnableRaisingEvents = true;

            //// Wait for the user to quit the program.
            //Console.WriteLine("Press \'q\' to quit the sample.");
            //while (Console.Read() != 'q') ;

        }


        // Define the event handlers.
        private void OnProfileChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            ProfileCheck();
        }

        private void OnProfileRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }



        void PingTimedEvent(Object source, ElapsedEventArgs e)
        {
            Interlocked.Increment(ref pingLoopInProgress);
            if (pingLoopInProgress == 1)
            {
                getPing();
            }
            else
            {
                Debug.WriteLine("!!!!!!!!!!!! PING PROCESS ALREADY RUNNING !!!!!!!!!!!!");
            }
            Interlocked.Decrement(ref pingLoopInProgress);
        }


        public void getPing()
        {
            Parallel.ForEach(serversList, new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10
                },
                dayZServer =>
                {
                    QueryServerData(dayZServer);
                });
        }

        public async Task QueryServerData(Server dayZServer)
        {
            ReadOnlyCollection<Player> players;
            ServerInfo info;
            if (dayZServer.QueryPort == 0)
            {
                byte[] data;
                
                try
                {
                    WebClient webClient = new WebClient();
                    data = await webClient.DownloadDataTaskAsync(new Uri("http://api.steampowered.com/ISteamApps/GetServersAtAddress/v1?addr=" + dayZServer.IP_Address + "&format=json", UriKind.Absolute));
                    string result = System.Text.Encoding.UTF8.GetString(data);
                    RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(result);
                    Console.WriteLine(rootObject.response.servers);
                    List<SteamServer> steamservers = rootObject.response.servers;
                    

                    if (steamservers != null && serversList != null)
                    {

                        foreach (SteamServer steamServer in steamservers)
                        {
                            string serverip = steamServer.addr.Substring(0, steamServer.addr.IndexOf(":", StringComparison.Ordinal));
                            string queryport = steamServer.addr.Substring(steamServer.addr.LastIndexOf(':') + 1);
                            string steamgameport = steamServer.gameport.ToString();
                            ushort queryportnum = ushort.Parse(queryport);
                            if (queryportnum == 0) continue;
                            try
                            {
                                if (serversList == null) continue;
                                Server matchCurrent = serversList.FirstOrDefault(p => p.IP_Address == serverip && p.Game_Port == steamgameport);
                                
                                if (matchCurrent != null)
                                {
                                    QueryMaster.Server server = ServerQuery.GetServerInstance(EngineType.Source, serverip, queryportnum);
                                    players = server.GetPlayers();
                                    info = server.GetInfo();
                                    matchCurrent.QueryPort = queryportnum;
                                    UpdateServerData(matchCurrent, players, info);
                                }
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine("Failed to query the DayZ server for data" + e);
                            }


                        }

                        
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to query steam server for data" + e);
                }

            }
            else
            {
                try
                {
                    QueryMaster.Server server = ServerQuery.GetServerInstance(EngineType.Source, dayZServer.IP_Address, dayZServer.QueryPort);
                    players = server.GetPlayers();
                    info = server.GetInfo();
                    UpdateServerData(dayZServer, players, info);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Failed to query the DayZ server for data" + e);
                }

                

            }
        }


        public void UpdateServerData(Server dayZServer, ReadOnlyCollection<Player> playerData, ServerInfo serverInfo)
        {
            List<DayZPlayer> playerList = new List<DayZPlayer>();
            if (playerData != null)
            {
                foreach (Player dzPlayer in playerData)
                {
                    DayZPlayer i = new DayZPlayer();
                    i.Name = dzPlayer.Name;
                    i.FullIP_Address = dayZServer.FullIP_Address;
                    playerList.Add(i);
                    //Console.WriteLine("Name : " + dzPlayer.Name + "\nScore : " + dzPlayer.Score + "\nTime : " + dzPlayer.Time + "\nServer : " + serverInfo.Name + "\nUser Count : " + serverInfo.Players + "\nPing : " + serverInfo.Ping);
                }
            }

            if (serverInfo != null)
            {
                dayZServer.PingSpeed = serverInfo.Ping;
                dayZServer.UserCount = serverInfo.Players;
            }
            dayZServer.playersList = playerList;
            ServerToDictionary(dayZServer);
            
        }



        

        




        //private async Task PlayerCompletedCallback(object sender, DownloadDataCompletedEventArgs e)
        //{
        //    if (e.Cancelled)
        //    {
        //        Console.WriteLine("Steam call canceled.");
        //    }

        //    if (e.Error != null)
        //    {
        //        Console.WriteLine("Steam call failed.");
        //        Console.WriteLine(e.Error.ToString());
        //    }
        //    else
        //    {
        //        byte[] data = (byte[])e.Result;
        //        string result = System.Text.Encoding.UTF8.GetString(data);
        //        RootObject rootObject = new RootObject();
        //        rootObject = JsonConvert.DeserializeObject<RootObject>(result);
        //        Console.WriteLine(rootObject.response.servers);
        //        List<SteamServer> steamservers = rootObject.response.servers;




        //        if (steamservers != null && serversList != null)
        //        {
        //            foreach (SteamServer steamServer in steamservers)
        //            {
        //                string serverip = steamServer.addr.Substring(0, steamServer.addr.IndexOf(":", StringComparison.Ordinal));
        //                string queryport = steamServer.addr.Substring(steamServer.addr.LastIndexOf(':') + 1);
        //                string steamgameport = steamServer.gameport.ToString();
        //                ushort queryportnum = ushort.Parse(queryport);
        //                if (queryportnum == 0) continue;




        //                try
        //                {
        //                    server = ServerQuery.GetServerInstance(EngineType.Source, serverip, queryportnum);
        //                    await Task.Run(async () => players = await GetPlayers(server));
        //                    await Task.Run(async () => info = await UpdateInfo(server));

        //                }
        //                catch (ArgumentException e)
        //                {
        //                    Console.WriteLine("Failed to query the DayZ server for data" + e);
        //                }

        //                await Task.Run(() => UpdateServerData(dayZServer, players, info));

        //                try
        //                {
        //                    server = ServerQuery.GetServerInstance(EngineType.Source, serverip, queryportnum);
        //                    ServerInfo info = server.GetInfo();
        //                }
        //                catch (Exception err)
        //                {
        //                    Console.WriteLine("QueryMaster failed: " + err);
        //                    continue;
        //                }
        //                //Server matchCurrent = compareList.FirstOrDefault((x, y) => x.Game_Port == steamgameport);
        //                if (serversList == null) continue;
        //                Server matchCurrent = serversList.FirstOrDefault(p => p.IP_Address == serverip && p.Game_Port == steamgameport);
        //                int indexCurrent = serversList.FindIndex(p => p.IP_Address == serverip && p.Game_Port == steamgameport);
        //                if (matchCurrent != null)
        //                {

        //                    matchCurrent.QueryPort = queryportnum;
        //                    await Task.Run(() => ServerToDictionary(matchCurrent));
        //                    await Task.Run(() => UpdateHistory());


        //                    List<DayZPlayer> listZ = new List<DayZPlayer>();
        //                    try
        //                    {

        //                        players = server.GetPlayers();


        //                    }
        //                    catch (ArgumentException err)
        //                    {
        //                        Console.WriteLine("Exception" + err);
        //                        continue;
        //                    }

        //                    if (dm.players != null)
        //                        foreach (Player Z in dm.players)
        //                        {

        //                            DayZPlayer i = new DayZPlayer();
        //                            i.Name = Z.Name;
        //                            i.FullIP_Address = matchCurrent.FullIP_Address;
        //                            listZ.Add(i);
        //                            //Console.WriteLine("Name : " + Z.Name + "\nScore : " + Z.Score + "\nTime : " + Z.Time);
        //                        }
        //                    //if (dm.info.) { matchCurrent.PingSpeed = dm.info.Ping;}
        //                    if (dm.info != null)
        //                    {
        //                        var type = dm.info.GetType();
        //                        matchCurrent.PingSpeed = dm.info.Ping;
        //                        matchCurrent.UserCount = dm.info.Players.ToString();
        //                    }
        //                    matchCurrent.playersList = listZ;
        //                    matchCurrent.QueryPort = queryportnum;
        //                    if (dm.info == null) continue;
        //                    dm.Servers.UpdateWithNotification(matchCurrent.IP_Address, matchCurrent);
        //                    //dm.server.Dispose();
        //                    dm.serversList = dm.Servers.Values.ToList();

        //                }

        //                //if (info.Address == steamServer.addr)
        //                //{

        //                //    break;
        //                //}
        //            }
        //        }
        //    }

        //    //    // EXAMPLE     ip=216.244.78.242&
        //    //    Match m2 = Regex.Match(result, "(?<=ip=).*?(?=&)", RegexOptions.Singleline);
        //    //    if (m2.Success)
        //    //    {
        //    //        URL = m2.ToString();
        //    //        DisplayResult(result, URL);
        //    //    }
        //    //}

        //    //// Let the main thread resume.
        //    //// UserToken is the AutoResetEvent object that the main thread  
        //    //// is waiting for.
        //    ////((AutoResetEvent)e.UserState).Set();
        //}





        //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        //public void WatchHistoryChanges()
        //{

        //    FileSystemWatcher watcherHistory = new FileSystemWatcher();
        //    watcherHistory.Path = directorypathHistory;
        //    /* Watch for changes in LastAccess and LastWrite times, and
        //       the renaming of files or directories. */
        //    watcherHistory.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
        //                           | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        //    // Only watch text files.
        //    watcherHistory.Filter = filepathHistory;

        //    // Add event handlers.
        //    watcherHistory.Changed += new FileSystemEventHandler(OnHistoryChanged);
        //    watcherHistory.Created += new FileSystemEventHandler(OnHistoryChanged);
        //    watcherHistory.Deleted += new FileSystemEventHandler(OnHistoryChanged);
        //    watcherHistory.Renamed += new RenamedEventHandler(OnHistoryRenamed);

        //    // Begin watching.
        //    watcherHistory.EnableRaisingEvents = true;

        //    // Wait for the user to quit the program.
        //    Console.WriteLine("Press \'q\' to quit the sample.");
        //    while (Console.Read() != 'q') ;
        //}

        //// Define the event handlers.
        //private void OnHistoryChanged(object source, FileSystemEventArgs e)
        //{
        //    // Specify what is done when a file is changed, created, or deleted.
        //    Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        //    getCurrentServerList();
        //}

        //private void OnHistoryRenamed(object source, RenamedEventArgs e)
        //{
        //    // Specify what is done when a file is renamed.
        //    Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        //}

        //public void writeServerHistoryList()
        //{
        //    Debug.WriteLine("writeServerHistoryList");
        //    var profileFileStream = new FileStream(configpath, FileMode.Open, FileAccess.Read, FileShare.Read);
        //    var profileStremReader = new StreamReader(profileFileStream);
        //    DayZProfile = profileStremReader.ReadToEnd();
        //    profileStremReader.Close();
        //    profileFileStream.Close();
        //    servername = DayZProfile.Split(new string[] { "lastMPServerName=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
        //    FullIPAddress = DayZProfile.Split(new string[] { "lastMPServer=\"" }, StringSplitOptions.None)[1].Split(new string[] { "\";" }, StringSplitOptions.None)[0].Trim();
        //    GamePort = FullIPAddress.Substring(FullIPAddress.LastIndexOf(':') + 1);
        //    IPAddress = FullIPAddress.Substring(0, FullIPAddress.LastIndexOf(":"));
        //    version = DayZProfile.Split(new string[] { "version=" }, StringSplitOptions.None)[1].Split(new string[] { ";" }, StringSplitOptions.None)[0].Trim();

        //    bool result = !IPAddress.Any(x => char.IsLetter(x));
        //    if (!result)
        //    {
        //        string domain = IPAddress;
        //        IPAddress[] ip_Addresses = Dns.GetHostAddresses(domain);
        //        string ips = string.Empty;
        //        foreach (IPAddress ipAddress in ip_Addresses)
        //        {
        //            IPAddress = ips;
        //            Console.WriteLine("Address from host name: " + IPAddress);
        //        }
        //    }

        //    if (File.Exists(serverhistorypath))
        //    {
        //        string temphistory;
        //        try
        //        {
        //            var fs = new FileStream(serverhistorypath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        //            var sr = new StreamReader(fs);
        //            temphistory = sr.ReadToEnd();
        //            sr.Close();
        //            fs.Close();
        //            server_list = JsonConvert.DeserializeObject<List<Server>>(temphistory);
        //            Server match = server_list.FirstOrDefault(x => x.IP_Address == IPAddress);
        //            int index = server_list.FindIndex(x => x.IP_Address == IPAddress);

        //            if (match != null)
        //            {
        //                if (match.Current == "0")
        //                {
        //                    Server matchCurrent = server_list.FirstOrDefault(x => x.Current == "1");
        //                    int indexCurrent = server_list.FindIndex(x => x.Current == "1");
        //                    if (matchCurrent != null)
        //                    {
        //                        matchCurrent.Current = "0";
        //                        server_list[indexCurrent] = matchCurrent;
        //                        writeServerMemory(matchCurrent);
        //                    }

        //                }

        //                match.Date = DateTime.Now;
        //                match.IP_Address = IPAddress;
        //                match.FullIP_Address = FullIPAddress;
        //                match.Current = "1";
        //                match.PingSpeed = 1000;
        //                match.UserCount = "Accessing...";
        //                match.Favorite = match.Favorite;
        //                match.QueryPort = match.QueryPort;
        //                match.Game_Port = match.Game_Port;
        //                match.playersList = null;
        //                writeServerMemory(match);
        //                server_list[index] = match;
        //                string listjson = JsonConvert.SerializeObject(server_list.ToArray());
        //                var fsw = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        //                var sw = new StreamWriter(fsw);
        //                sw.Write(listjson);
        //                sw.Close();
        //                fsw.Close();

        //            }
        //            else
        //            {
        //                Server matchCurrent = server_list.FirstOrDefault(x => x.Current == "1");
        //                int indexCurrent = server_list.FindIndex(x => x.Current == "1");
        //                if (matchCurrent != null)
        //                {
        //                    matchCurrent.Current = "0";
        //                    server_list[indexCurrent] = matchCurrent;
        //                    writeServerMemory(matchCurrent);
        //                }

        //                Server newserver = new Server();
        //                newserver.ServerName = servername;
        //                newserver.IP_Address = IPAddress;
        //                newserver.FullIP_Address = FullIPAddress;
        //                newserver.Date = DateTime.Now;
        //                newserver.Favorite = "0";
        //                newserver.Current = "1";
        //                newserver.PingSpeed = 10000;
        //                newserver.UserCount = "Accessing...";
        //                newserver.QueryPort = 0;
        //                newserver.Game_Port = GamePort;
        //                newserver.playersList = null;

        //                server_list.Add(newserver);
        //                writeServerMemory(newserver);

        //                string listjson = JsonConvert.SerializeObject(server_list.ToArray());
        //                var fswadd = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        //                var swadd = new StreamWriter(fswadd);
        //                swadd.Write(listjson);
        //                swadd.Close();
        //                fswadd.Close();
        //            }
        //            //readHistoryfile();
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("Exception" + e);
        //        }
        //    }
        //    else
        //    {
        //        if (server_list == null)
        //        {
        //            server_list = new List<Server>();
        //        }

        //        Server newserver = new Server();
        //        newserver.ServerName = servername;
        //        newserver.IP_Address = IPAddress;
        //        newserver.FullIP_Address = FullIPAddress;
        //        newserver.Date = DateTime.Now;
        //        newserver.Favorite = "0";
        //        newserver.Current = "1";
        //        newserver.PingSpeed = 10000;
        //        newserver.UserCount = "Accessing...";
        //        newserver.QueryPort = 0;
        //        newserver.Game_Port = GamePort;
        //        newserver.playersList = null;
        //        server_list.Add(newserver);
        //        writeServerMemory(newserver);

        //        string listjson = JsonConvert.SerializeObject(server_list.ToArray());
        //        var fswnew = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        //        var swnew = new StreamWriter(fswnew);
        //        try
        //        {
        //            swnew.Write(listjson);
        //            swnew.Close();
        //            fswnew.Close();
        //            //readHistoryfile();
        //        }
        //        catch (ArgumentException e)
        //        {
        //            Console.WriteLine("Exception" + e);
        //        }
        //    }
        //}

        //public void readHistoryfile()
        //{
        //    server_list = getServerList();
        //    if(server_list != null) { 
        //    foreach (Server DayZServer in server_list)
        //    {
        //        writeServerMemory(DayZServer);
        //    }
        //    }
        //}

        //public void removeHistoryfile()
        //{
        //    server_list = getServerList();
        //    foreach (Server DayZServer in server_list)
        //    {
        //        removeServerMemory(DayZServer);
        //    }
        //    File.Delete(serverhistorypath);
        //}

        //public void writeServerMemory(Server DayZServer)
        //{
        //    Console.WriteLine(" update Server: {0} current: {1}, favorite: {2}", DayZServer.IP_Address, DayZServer.Current, DayZServer.Favorite);
        //    Servers.UpdateWithNotification(DayZServer.IP_Address, DayZServer);

        //    serversList = Servers.Values.ToList();
        //}

        //public void removeServerMemory(Server DayZServer)
        //{
        //    Server dzServer = new Server();
        //    dzServer.Date = DayZServer.Date;
        //    dzServer.ServerName = DayZServer.ServerName;
        //    dzServer.IP_Address = DayZServer.IP_Address;
        //    dzServer.FullIP_Address = DayZServer.FullIP_Address;
        //    dzServer.Current = DayZServer.Current;
        //    dzServer.Favorite = DayZServer.Favorite;
        //    dzServer.PingSpeed = DayZServer.PingSpeed;
        //    dzServer.UserCount = DayZServer.UserCount;
        //    dzServer.Game_Port = DayZServer.Game_Port;
        //    Servers.TryRemoveWithNotification(DayZServer.IP_Address, out DayZServer);
        //    serversList = Servers.Values.ToList() as List<Server>;
        //}

        //public List<Server> getServerList()
        //{
        //    if (server_list == null)
        //        server_list = new List<Server>();

        //    if (File.Exists(serverhistorypath))
        //    {
        //        string temphistory;

        //        if (server_list.Count == 0)
        //        {
        //            var fs = new FileStream(serverhistorypath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        //            var sr = new StreamReader(fs);
        //            temphistory = sr.ReadToEnd();
        //            sr.Close();
        //            fs.Close();
        //            try
        //            {
        //                server_list = JsonConvert.DeserializeObject<List<Server>>(temphistory);
        //            }
        //            catch (Exception e)
        //            {
        //                Console.WriteLine("Exception" + e);
        //                server_list.Clear();
        //                serversList.Clear();
        //                // Servers.Clear();
        //                File.Delete(serverhistorypath);
        //                return null;
        //            }
        //        }
        //    }

        //    return server_list;
        //}

        //public Server getCurrentServerList()
        //{
        //    if (server_list == null)
        //        server_list = new List<Server>();

        //    if (server_list.Count == 0)
        //    {
        //        try
        //        {
        //            server_list = getServerList();
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("Exception" + e);
        //            return null;
        //        }
        //    }

        //    try
        //    {
        //        Server matchCurrent = server_list.FirstOrDefault(x => x.Current == "1");
        //        int indexCurrent = server_list.FindIndex(x => x.Current == "1");
        //        if (matchCurrent != null)
        //        {
        //            return matchCurrent;
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Exception" + e);
        //        return null;
        //    }
        //}

        //public Server userList(string ip)
        //{
        //    Server matchPlayers = serversList.FirstOrDefault(x => x.IP_Address == ip);
        //    int indexCurrent = server_list.FindIndex(x => x.IP_Address == ip);
        //    if (matchPlayers != null)
        //    {
        //        return matchPlayers;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        //public Server getServerByIP(string ip)
        //{
        //    if (server_list.Count == 0)
        //    {
        //        try
        //        {
        //            server_list = getServerList();
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("Exception" + e);
        //            return null;
        //        }
        //    }

        //    Server matchCurrent = server_list.FirstOrDefault(x => x.IP_Address == ip);
        //    if (matchCurrent != null)
        //    {
        //        return matchCurrent;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

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

        //String readAppPath(string dayzapppath)
        //{
        //    if (File.Exists(dayzapppath))
        //    {
        //        try
        //        {
        //            using (StreamReader sreader = new StreamReader(File.OpenRead(dayzapppath)))
        //            {
        //                String line = sreader.ReadToEnd();
        //                sreader.Close();
        //                return line;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            return dayzpath;
        //        }
        //    }
        //    else
        //    {
        //        writeAppPath(dayzpath);
        //        return dayzpath;
        //    }
        //}

        public async Task updateFavorite(string favoriteServer)
        {

            try
            {
                Server match = serversList.FirstOrDefault(x => x.FullIP_Address == favoriteServer);

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

                    await Task.Run(() => ServerToDictionary(match));
                    await Task.Run(() => UpdateHistory());
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Could Not Set Favorite: " + e);
            }
        }

        public void deleteServer(string deleteServer)
        {
            try
            {
                Server match = serversList.FirstOrDefault(x => x.ServerName == deleteServer);

            if (match != null)
            {
                Servers.TryRemoveWithNotification(match.IP_Address, out match);
                serversList = Servers.Values.ToList();
            }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Exception: " + e);
            }
        }

        public async Task deleteServerHistory()
        {
            try
            {
               
                
                foreach (Server dayZServer in serversList)
                {
                    Server deletServer = dayZServer;
                    Servers.TryRemoveWithNotification(dayZServer.IP_Address, out deletServer);
                }
                serversList = Servers.Values.ToList();
                await Task.Run(() => UpdateHistory());
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Exception" + e);
            }
        }









        public async Task getGTList()
        {
            //WebClient webClient = new WebClient();
            //string _UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
            //webClient.Headers["Accept"] = "application/x-ms-application, image/jpeg, application/xaml+xml, image/gif, image/pjpeg, application/x-ms-xbap, application/x-shockwave-flash, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
            //webClient.Headers["User-Agent"] = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; MDDC)";

            //webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(GTCompletedCallback);
            //string strUrl = "https://www.gametracker.com/search/dayz/US/?sort=4&order=DESC&searchipp=50#search";
            ////byte[] reqHTML;
            //Uri uri = new Uri(strUrl);
            //webClient.DownloadDataAsync(uri);



            byte[] data;
            WebClient webClient = new WebClient();
            string _UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
            webClient.Headers["Accept"] = "application/x-ms-application, image/jpeg, application/xaml+xml, image/gif, image/pjpeg, application/x-ms-xbap, application/x-shockwave-flash, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
            webClient.Headers["User-Agent"] = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; MDDC)";
            data = await webClient.DownloadDataTaskAsync(new Uri("https://www.gametracker.com/search/dayz/US/?sort=3&order=DESC&searchipp=50", UriKind.Absolute));
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
            
            foreach (DayZPlayer GTServer in GTlist)
            {
                bool serverdata = !GTServer.FullIP_Address.Any(x => char.IsLetter(x));
                if (!serverdata)
                {
                    continue;
                }

                await Task.Run(() => writeGTList(GTServer.Name, GTServer.FullIP_Address));
            }





        }

        //private static void GTCompletedCallback(object sender, DownloadDataCompletedEventArgs e)
        //{
        //    if (e.Cancelled)
        //    {
        //        Console.WriteLine("GT call canceled.");
        //    }

        //    if (e.Error != null)
        //    {
        //        Console.WriteLine("GT call failed.");
        //        Console.WriteLine(e.Error.ToString());
        //    }
        //    else
        //    {
        //        byte[] data = (byte[])e.Result;
        //        string result = System.Text.Encoding.UTF8.GetString(data);

        //        Match m2 = Regex.Match(result, "(?<=Server Map).*?(?=Server Map)", RegexOptions.Singleline);
        //        if (m2.Success)
        //        {
        //            result = m2.ToString();
        //        }
        //        else
        //        {
        //            result = "<td><a href=\"/search/dayz/600/\"><img src=\"/images/game_icons16/dayz.png\" alt=\"DAYZ\"/></a></td><td><a class=\"c03serverlink\" href=\"/server_info/216.244.78.242:2802/\">\\DG Clan - SUPERSHARD #1 \\ Unlocked \\ High Loot \\ 24/7 day</a><a href=\"javascript:showPopupExternalLink('gt://joinGame:game=dayz&amp;ip=216.244.78.242&amp;port=2802');\"><img src=\"/images/global/btn_join.png\" alt=\"Join\"/></a></td><td>5/50</td><td></td><td><a href=\"/search/dayz/US/\"><img src=\"/images/flags/us.gif\" alt=\"\" class=\"item_16x11\"/></a></td><td><span class=\"ip\">216.244.78.242</span><span class=\"port\">:2802</span></td><td>DayZ_Auto</td>";
        //        }

        //        List<DayZPlayer> GTlist = new List<DayZPlayer>();

        //        // 1.
        //        // Find all matches in file.
        //        MatchCollection m1 = Regex.Matches(result, @"(<a.*?>.*?</a>)",
        //            RegexOptions.Singleline);

        //        // 2.
        //        // Loop over each match.
        //        foreach (Match m in m1)
        //        {
        //            string value = m.Groups[1].Value;
        //            DayZPlayer i = new DayZPlayer();

        //            // 3.
        //            // Get href attribute.
        //            Match m3 = Regex.Match(value, "href=\"/server_info/" + "(.*?)" + "/\"",
        //            RegexOptions.Singleline);

        //            if (m3.Success)
        //            {
        //                i.FullIP_Address = m3.Groups[1].Value.ToString();
        //            }

        //            // 4.
        //            // Remove inner tags from text.
        //            string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
        //            RegexOptions.Singleline);
        //            i.Name = t;
        //            if (i.FullIP_Address == null) continue;
        //            GTlist.Add(i);
        //        }
        //        Console.WriteLine("Servers" + GTlist);
        //        DataManager dm = new DataManager();
        //        foreach (DayZPlayer GTServer in GTlist)
        //        {
        //            bool serverdata = !GTServer.FullIP_Address.Any(x => char.IsLetter(x));
        //            if (!serverdata)
        //            {
        //                continue;
        //            }

        //            dm.writeGTList(GTServer.Name, GTServer.FullIP_Address);
        //        }
        //    }
        //}

        public async Task writeGTList(string serverName, string fullIPAdress)
        {
            Server gtServer = new Server();
            servername = serverName;
            FullIPAddress = fullIPAdress;
            GamePort = FullIPAddress.Substring(FullIPAddress.LastIndexOf(':') + 1);
            IPAddress = FullIPAddress.Substring(0, FullIPAddress.LastIndexOf(":"));
            version = DayZProfile.Split(new string[] { "version=" }, StringSplitOptions.None)[1].Split(new string[] { ";" }, StringSplitOptions.None)[0].Trim();

            gtServer.ServerName = servername;
            gtServer.IP_Address = IPAddress;
            gtServer.FullIP_Address = FullIPAddress;
            gtServer.Date = DateTime.Now;
            gtServer.Favorite = "0";
            gtServer.Current = "0";
            gtServer.PingSpeed = 10000;
            gtServer.UserCount = 0;
            gtServer.QueryPort = 0;
            gtServer.Game_Port = GamePort;
            gtServer.playersList = null;



            try
                {
                    if (serversList.Count != 0)
                    {
                        foreach (Server dayZServer in serversList)
                        {
                            Server match = serversList.FirstOrDefault(x => x.IP_Address == gtServer.IP_Address);
                            if (match != null)
                            {
                                if (match.Current == "0")
                                {
                                    match.Current = "0";
                                }
                                else
                                {
                                    match.Current = "1";
                                }
                                await Task.Run(() => ServerToDictionary(match));
                                await Task.Run(() => UpdateHistory());
                            }
                            else
                            {
                                await Task.Run(() => ServerToDictionary(gtServer));
                                await Task.Run(() => UpdateHistory());
                            }

                        }

                }
                else {
                        await Task.Run(() => ServerToDictionary(gtServer));
                        await Task.Run(() => UpdateHistory());
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception Could not Add GT servers to list" + e);
                }
           
        }






        // ******************SUPER OLD *********************

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

        // ******************SUPER OLD *********************
    }
}
