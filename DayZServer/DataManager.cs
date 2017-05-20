using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Data;
using System.Collections.Specialized;
using System.Collections;

namespace DayZServer
{
    public static class ExtensionMethods
    {
        public static int Remove<T>(
            this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }
    }

    public class DataManager 
    {
        public string myDocumentsPath;
        public string[] profile;
        public string profilepath;
        public string fileNameProfile;
        public string directoryPathProfile;
        public string filepathHistory;
        public string directorypathHistory;
        public string DayZProfile;
        public string servername;
        public string FullIPAddress;
        public string GamePort;
        public string IPAddress;
        public string version;
        public string localApplicationPath;
        public string dayZServerAppPath;
        public string dayzapppath;
        public string dayzpath;
        public string serverhistorypath;
        public string currentserverpath;
        public string temphistory;
        public int pingIndex;
        public List<Server> server_list;
        public List<Server> serversList = new List<Server>();
        public List<DayZPlayer> playersList = new List<DayZPlayer>();
        public Server profileServer = new Server();
        public static string tester;
        public static string currentIP;
        public ObservableCollection<Server> Servers = new ObservableCollection<Server>();
        public static System.Timers.Timer PingTimer;
        private static System.Timers.Timer PingTimer2;
        static int pingLoopInProgress = 0;
        public FileSystemWatcher watcherProfile = new FileSystemWatcher();


        public static event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        public class Server : INotifyPropertyChanged
        {
            public string ServerName { get; set; }
            public ushort QueryPort { get; set; }
            public string Game_Port { get; set; }
            public string IP_Address { get; set; }
            public string FullIP_Address { get; set; }
            public DateTime Date { get; set; }
            public string Favorite { get; set; }
            private string _Current;
            public string Current
            {
                get
                {
                    return _Current;
                }
                set
                {
                    if (_Current == value)
                        return; _Current = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Current"));
                }
            }
            private long _PingSpeed;
            public long PingSpeed
            {
                get
                {
                    return _PingSpeed;
                }
                set
                {
                    if (_PingSpeed == value)
                        return; _PingSpeed = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("PingSpeed"));
                }
            }

            private long _UserCount;
            public long UserCount
            {
                get
                {
                    return _UserCount;
                }
                set
                {
                    if (_UserCount == value)
                        return; _UserCount = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("UserCount"));
                }
            }

            public int UserTotal { get; set; }
            public bool Details { get; set; }

            private List<DayZPlayer> _playersList;
            public List<DayZPlayer> playersList
            {
                get
                {
                    return _playersList;
                }
                set
                {
                    if (_playersList == value)
                        return; _playersList = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("playersList"));
                }
            }

            public bool IsPrivate { get; set; }
            public long MaxPlayers { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, e);
            }
        }

        public class DayZPlayer
        {
            public string Name { get; set; }
            public string FullIP_Address { get; set; }
            public string Time { get; set; }

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


        public DataManager()
        {

        }


        public async Task startDataManager()
        {
            await Task.Run(() => FileSetup());
            await Task.Run(() => ProfileCheck());
            PingTimer = new System.Timers.Timer(7000);
            PingTimer.Elapsed += PingTimedEvent;
            PingTimer.Enabled = true;
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
            //var profileFileStream = new FileStream(profilepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            FileStream profileFileStream = WaitForFile(profilepath, FileMode.Open, FileAccess.Read, FileShare.Read);
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

            profileServer.ServerName = null;
            profileServer.IP_Address = null;
            profileServer.FullIP_Address = null;
            profileServer.Favorite = null;
            profileServer.Current = null;
            profileServer.Game_Port = null;
            profileServer.playersList = null;


            profileServer.ServerName = servername;
            profileServer.IP_Address = IPAddress;
            profileServer.FullIP_Address = FullIPAddress;
            profileServer.Date = DateTime.Now;
            profileServer.Favorite = "0";
            profileServer.Current = "1";
            profileServer.PingSpeed = 10000;
            profileServer.UserCount = 0;
            profileServer.IsPrivate = false;
            profileServer.MaxPlayers = 0;
            profileServer.QueryPort = 0;
            profileServer.Game_Port = GamePort;
            profileServer.playersList = null;
            profileServer.Details = false;
        }

        FileStream WaitForFile(string fullPath, FileMode mode, FileAccess access, FileShare share)
        {
            for (int numTries = 0; numTries < 10; numTries++)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(fullPath, mode, access, share);
                    return fs;
                }
                catch (IOException)
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                    Thread.Sleep(50);
                }
            }

            return null;
        }



        private async Task CheckingHistory()
        {

            try
            {
                List<Server> serverHistory = new List<Server>();
                var fs = new FileStream(serverhistorypath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var sr = new StreamReader(fs);
                await Task.Run(() => temphistory = sr.ReadToEnd());
                sr.Close();
                fs.Close();
                serverHistory = JsonConvert.DeserializeObject<List<Server>>(temphistory);

                foreach (Server dayZServer in serverHistory)
                {
                    ServerToDictionary(dayZServer);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Reading dayzhistory.txt" + e);
            }

        }



        //need to refactor so there are not two methods here

        private void ServerToDictionary(Server dayZServer)
        {
            // The await causes the handler to return immediately.
            PushData(dayZServer);
        }

        private void PushData(Server dayZServer)
        {
            try
            {
                Server serverMatch = Servers.FirstOrDefault(i => i.IP_Address == dayZServer.IP_Address);

                if (serverMatch != null)
                {


                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
                    {
                        serverMatch.PingSpeed = dayZServer.PingSpeed;
                        serverMatch.UserCount = dayZServer.UserCount;
                        serverMatch.IsPrivate = dayZServer.IsPrivate;
                        serverMatch.MaxPlayers = dayZServer.MaxPlayers;
                        serverMatch.playersList = dayZServer.playersList;
                        serverMatch.Current = dayZServer.Current;

                    });

                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
                    {
                        Servers.Add(dayZServer);
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Pushing Data to Dictionary" + e);
            }

        }


        private async Task ProfileMatch()
        {
            try
            {
                Server match = Servers.FirstOrDefault(x => x.IP_Address == profileServer.IP_Address);
                Server current = Servers.FirstOrDefault(x => x.Current == "1");


                if (current != null)
                {
                    if(match != null)
                    {
                        if (current.IP_Address == match.IP_Address)
                        {
                            current.Date = DateTime.Now;
                            ServerToDictionary(current);
                            UpdateHistory();
                        }
                        else
                        {
                            
                            current.Current = "0";
                            ServerToDictionary(current);
                            ServerToDictionary(profileServer);
                            UpdateHistory();
                        }
                    }
                    else
                    {
                        current.Current = "0";
                        ServerToDictionary(current);
                        ServerToDictionary(profileServer);
                        UpdateHistory();
                    }
                }
                else 
                {
                    ServerToDictionary(profileServer);
                    UpdateHistory();
                }






                //foreach (Server dz in Servers)
                //{
                //    if (dz.Current == "1")
                //    {
                //        dz.Current = "0";
                //        ServerToDictionary(dz);
                //        UpdateHistory();
                //    }
                //}
                //if (match != null)
                //{
                //    match.Current = "1";
                //    match.Date = DateTime.Now;
                //    ServerToDictionary(match);
                //    UpdateHistory();
                //}
                //else
                //{
                //    ServerToDictionary(profileServer);
                //    UpdateHistory();
                //}

            }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e);
                }
        }



        public void UpdateHistory()
        {
            string listjson = JsonConvert.SerializeObject(Servers.ToArray());
            var fsw = new FileStream(serverhistorypath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var sw = new StreamWriter(fsw);
            sw.Write(listjson);
            sw.Close();
            fsw.Close();
            PingTimer.Start();
            watcherProfile.EnableRaisingEvents = true;
        }


        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void WatchProfileChanges()
        {
            watcherProfile.Path = directoryPathProfile;
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            watcherProfile.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                          | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            watcherProfile.Filter = fileNameProfile;

            // Add event handlers.
            watcherProfile.Changed += new FileSystemEventHandler(OnProfileChanged);
            watcherProfile.Created += new FileSystemEventHandler(OnProfileChanged);
            watcherProfile.Deleted += new FileSystemEventHandler(OnProfileChanged);
            watcherProfile.Renamed += new RenamedEventHandler(OnProfileRenamed);

            // Begin watching.
            watcherProfile.EnableRaisingEvents = true;

        }





        private async void OnProfileChanged(object source, FileSystemEventArgs e)
        {
            watcherProfile.EnableRaisingEvents = false;
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            PingTimer.Stop();
            cts.Cancel();
            cts = new CancellationTokenSource();
            await Task.Delay(5000);
            ProfileCheck();
        }

        private void OnProfileRenamed(object source, RenamedEventArgs e)
        {
          
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }

        void PingTimedEvent(Object source, ElapsedEventArgs e)
        {
            string pingtime = PingTimer.Interval.ToString();
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

        public CancellationTokenSource cts = new CancellationTokenSource();
        public ParallelOptions po = new ParallelOptions();
        public void getPing()
        {
            po.MaxDegreeOfParallelism = 10;
            po.CancellationToken = cts.Token;

            try
            {
                Parallel.ForEach(Servers, po,
                    dayZServer =>
                    {
                        QueryServerData(dayZServer);
                        //po.CancellationToken.ThrowIfCancellationRequested();
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine("Thread Kill Error: " + e);
            }

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

                    if (steamservers != null && Servers != null)
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
                                if (Servers == null) continue;
                                Server matchCurrent = Servers.FirstOrDefault(p => p.IP_Address == serverip && p.Game_Port == steamgameport);

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
                    i.Time = new DateTime(dzPlayer.Time.Ticks).ToString("HH:mm:ss");
                    playerList.Add(i);
                    //Console.WriteLine("Name : " + dzPlayer.Name + "\nScore : " + dzPlayer.Score + "\nTime : " + dzPlayer.Time + "\nServer : " + serverInfo.Name + "\nUser Count : " + serverInfo.Players + "\nPing : " + serverInfo.Ping);
                }
            }

            if (serverInfo != null)
            {
                dayZServer.PingSpeed = serverInfo.Ping;
                dayZServer.UserCount = serverInfo.Players;
                dayZServer.IsPrivate = serverInfo.IsPrivate;
                dayZServer.MaxPlayers = serverInfo.MaxPlayers;
            }
            dayZServer.playersList = playerList;
            ServerToDictionary(dayZServer);
        }

        private async Task FileSetup()
        {
            dayZServerAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DayZServer"); //gets C:\Users\<User>\AppData\Local\DayZServer
            dayzapppath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dayZServerAppPath, "dayzapppath.txt"); //path to config that identifies where DayZ is installed
            serverhistorypath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dayZServerAppPath, "dayzhistory.txt"); //path to list that DayZ history is saved to

            if (!Directory.Exists(dayZServerAppPath))
            {
                await Task.Run(() => Directory.CreateDirectory(dayZServerAppPath));
                await Task.Run(() => WriteFile(dayzapppath));
                await Task.Run(() => WriteFile(serverhistorypath));
            }
            else
            {
                if (!File.Exists(dayzapppath))
                {
                    await Task.Run(() => WriteFile(dayzapppath));
                }
                if (!File.Exists(serverhistorypath))
                {
                    await Task.Run(() => WriteFile(serverhistorypath));
                }
            }

            myDocumentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DayZ");  //gets C:\Users\<User>\Documents\DayZ ****//Change to DayZ for Production

            if (!Directory.Exists(myDocumentsPath))
            {
                await Task.Run(() => Directory.CreateDirectory(myDocumentsPath));
                string dummyProfileDirectory = Path.Combine(Environment.CurrentDirectory, @"Profile\");
                await Task.Run(() => ExtractEmbeddedResource(myDocumentsPath, "DayZServer", "VG7.DayZProfile"));
                profile = Directory.GetFiles(myDocumentsPath, "*.DayZProfile").Where(f => !f.Contains("vars")).ToArray();
                if (profile != null)
                {
                    profilepath = profile[0];
                    fileNameProfile = new FileInfo(profilepath).Name;
                    directoryPathProfile = new FileInfo(profilepath).Directory.FullName;
                }
            }
            else
            {
                profile = Directory.GetFiles(myDocumentsPath, "*.DayZProfile").Where(f => !f.Contains("vars")).ToArray();
                if (profile != null)
                {
                    profilepath = profile[0];
                    fileNameProfile = new FileInfo(profilepath).Name;
                    directoryPathProfile = new FileInfo(profilepath).Directory.FullName;
                }
            }
        }

        public void WriteFile(string pathName)
        {
            try
            {
                using (StreamWriter sw = File.CreateText(pathName))
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
                Console.WriteLine("Could not write file: " + pathName + " : " + e);
            }
        }


        private static void ExtractEmbeddedResource(string outputDir, string resourceLocation, string file)
        {

            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceLocation + @"." + file))
            {
                using (FileStream fileStream = new FileStream(Path.Combine(outputDir, file), FileMode.Create))
                {
                    if (stream != null)
                    {
                        for (int i = 0; i < stream.Length; i++)
                        {
                            fileStream.WriteByte((byte)stream.ReadByte());
                        }
                        fileStream.Close();
                    }
                }
            }
        }

        public async Task updateFavorite(string favoriteServer)
        {
            try
            {
                Server match = Servers.FirstOrDefault(x => x.FullIP_Address == favoriteServer);

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
                    UpdateHistory();
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Could Not Set Favorite: " + e);
            }
        }

        public void deleteServer(string IP_Address)
        {
            try
            {
                Server serverMatch = Servers.FirstOrDefault(i => i.IP_Address == IP_Address);

                if (serverMatch != null)
                {
                    cts.Cancel();
                    cts = new CancellationTokenSource();
                    Servers.Remove(serverMatch);
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
               //foreach (Server server in Servers)
                //{
                cts.Cancel();
                cts = new CancellationTokenSource();
                Servers.Remove(x => x.Current != "1");
                //}
                UpdateHistory();
                ProfileCheck();
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Exception" + e);
            }
        }

        public async Task getGTList()
        {
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
            gtServer.IsPrivate = false;
            gtServer.MaxPlayers = 0;
            gtServer.QueryPort = 0;
            gtServer.Game_Port = GamePort;
            gtServer.playersList = null;
            gtServer.Details = false;

            try
            {
                if (Servers.Count != 0)
                {
                    foreach (Server dayZServer in Servers)
                    {
                        Server match = Servers.FirstOrDefault(x => x.IP_Address == gtServer.IP_Address);
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
                            if (match.Favorite == "1")
                            {
                                match.Favorite = "1";
                            }
                            else if (match.Favorite == "0")
                            {
                                match.Favorite = "0";
                            }
                            if (match.Details)
                            {
                                match.Details = true;
                            }
                            else if (!match.Details)
                            {
                                match.Details = false;
                            }
                            await Task.Run(() => ServerToDictionary(match));
                            UpdateHistory();
                        }
                        else
                        {
                            await Task.Run(() => ServerToDictionary(gtServer));
                            UpdateHistory();
                        }

                    }

                }
                else
                {
                    await Task.Run(() => ServerToDictionary(gtServer));
                    UpdateHistory();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Could not Add GT servers to list" + e);
            }

        }
    }
}