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

using Sample2_CallbackManager;

using System.IO;



namespace DayZServer
{

    /// <summary>

    /// Interaction logic for Window1.xaml

    /// </summary>

    public partial class Window1 : Window
    {
        static SteamClient steamClient;
        static CallbackManager manager;

        static SteamUser steamUser;
        static SteamFriends steamFriends;

        static bool isRunning;

        static string user, pass;

        static string authCode, twoFactorAuth;


        public Window1()
        {

            InitializeComponent();

            List<Server> items = new List<Server>();

            items.Add(new Server() { ServerName = "Server 1 test dont go there because its ...", IPAddress = "192.168.0.456", Date = "01-22-2015 : 10:30 AM", Favorite = "1" });

            items.Add(new Server() { ServerName = "Off the Cloud Server Death Zone 5", IPAddress = "192.168.0.275", Date = "01-22-2015 : 10:30 AM", Favorite = "4" });

            items.Add(new Server() { ServerName = "Joe's Jimmy Joe SAW Only Bambies Welcome Joe's Jimmy Joe SAW Only Bambies Welcome Joe's Jimmy Joe SAW Only Bambies Welcome", IPAddress = "192.168.0.756", Date = "01-22-2015 : 10:30 AM", Favorite = "5" });

            severList.ItemsSource = items;

            steamLogin.Visibility = Visibility.Hidden;
            //authCodeBox.Visibility = Visibility.Hidden;

        }



        public class Server
        {

            public string ServerName { get; set; }



            public string IPAddress { get; set; }



            public string Date { get; set; }



            public string Favorite { get; set; }

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




        static void MainLogin(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Sample5: No username and password specified!");
                return;
            }

            // save our logon details
            user = args[0];
            pass = args[1];
            authCode = args[2];
            twoFactorAuth = args[3];


            // create our steamclient instance
            steamClient = new SteamClient(System.Net.Sockets.ProtocolType.Tcp);
            // create the callback manager which will route callbacks to function calls
            manager = new CallbackManager(steamClient);

            // get the steamuser handler, which is used for logging on after successfully connecting
            steamUser = steamClient.GetHandler<SteamUser>();
            // get the steam friends handler, which is used for interacting with friends on the network after logging on
            steamFriends = steamClient.GetHandler<SteamFriends>();

            // register a few callbacks we're interested in
            // these are registered upon creation to a callback manager, which will then route the callbacks
            // to the functions specified
            new Callback<SteamClient.ConnectedCallback>(OnConnected, manager);
            new Callback<SteamClient.DisconnectedCallback>(OnDisconnected, manager);

            new Callback<SteamUser.LoggedOnCallback>(OnLoggedOn, manager);
            new Callback<SteamUser.LoggedOffCallback>(OnLoggedOff, manager);
            // this callback is triggered when the steam servers wish for the client to store the sentry file
            new Callback<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth, manager);

            // we use the following callbacks for friends related activities
            new Callback<SteamUser.AccountInfoCallback>(OnAccountInfo, manager);
            new Callback<SteamFriends.FriendsListCallback>(OnFriendsList, manager);
            new Callback<SteamFriends.PersonaStateCallback>(OnPersonaState, manager);
            new Callback<SteamFriends.FriendAddedCallback>(OnFriendAdded, manager);

            isRunning = true;

            Console.WriteLine("Connecting to Steam...");

            // initiate the connection
            steamClient.Connect();

            // create our callback handling loop
            while (isRunning)
            {
                // in order for the callbacks to get routed, they need to be handled by the manager
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to connect to Steam: {0}", callback.Result);

                isRunning = false;
                return;
            }

            Console.WriteLine("Connected to Steam! Logging in '{0}'...", user);

            byte[] sentryHash = null;
            if (File.Exists("sentry.bin"))
            {
                // if we have a saved sentry file, read and sha-1 hash it
                byte[] sentryFile = File.ReadAllBytes("sentry.bin");
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }

            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = user,
                Password = pass,

                // in this sample, we pass in an additional authcode
                // this value will be null (which is the default) for our first logon attempt
                AuthCode = authCode,

                // if the account is using 2-factor auth, we'll provide the two factor code instead
                // this will also be null on our first logon attempt
                TwoFactorCode = twoFactorAuth,

                // our subsequent logons use the hash of the sentry file as proof of ownership of the file
                // this will also be null for our first (no authcode) and second (authcode only) logon attempts
                SentryFileHash = sentryHash,
            });
        }

        static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Disconnected from Steam");

            isRunning = false;
        }

        static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
            bool is2FA = callback.Result == EResult.AccountLogonDeniedNeedTwoFactorCode;

            if (isSteamGuard || is2FA)
            {
                Console.WriteLine("This account is SteamGuard protected!");

                if (is2FA)
                {
                    Console.Write("Please enter your 2 factor auth code from your authenticator app: ");
                    twoFactorAuth = Console.ReadLine();
                    Window1 mainwindow = new Window1();
                    mainwindow.authCodeBox.Visibility = Visibility.Visible;
                }
                else
                {
                    Console.Write("Please enter the auth code sent to the email at {0}: ", callback.EmailDomain);
                    authCode = Console.ReadLine();
                    Window1 mainwindow = new Window1();
                    mainwindow.authCodeBox.Visibility = Visibility.Visible;
                }

                return;
            }

            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

                isRunning = false;
                return;
            }

            Console.WriteLine("Successfully logged on!");
            Window1 mainwindow2 = new Window1();
            mainwindow2.authCodeBox.Visibility = Visibility.Visible;
            mainwindow2.steamLogin.Visibility = Visibility.Hidden;

            // at this point, we'd be able to perform actions on Steam
        }


        static void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            // before being able to interact with friends, you must wait for the account info callback
            // this callback is posted shortly after a successful logon

            // at this point, we can go online on friends, so lets do that
            steamFriends.SetPersonaState(EPersonaState.Online);
        }

        static void OnFriendsList(SteamFriends.FriendsListCallback callback)
        {
            // at this point, the client has received it's friends list

            int friendCount = steamFriends.GetFriendCount();

            Console.WriteLine("We have {0} friends", friendCount);

            for (int x = 0; x < friendCount; x++)
            {
                // steamids identify objects that exist on the steam network, such as friends, as an example
                SteamID steamIdFriend = steamFriends.GetFriendByIndex(x);

                // we'll just display the STEAM_ rendered version
                Console.WriteLine("Friend: {0}", steamIdFriend.Render());
            }

            // we can also iterate over our friendslist to accept or decline any pending invites

            foreach (var friend in callback.FriendList)
            {
                if (friend.Relationship == EFriendRelationship.RequestRecipient)
                {
                    // this user has added us, let's add him back
                    steamFriends.AddFriend(friend.SteamID);
                }
            }
        }

        static void OnFriendAdded(SteamFriends.FriendAddedCallback callback)
        {
            // someone accepted our friend request, or we accepted one
            Console.WriteLine("{0} is now a friend", callback.PersonaName);
        }

        static void OnPersonaState(SteamFriends.PersonaStateCallback callback)
        {
            // this callback is received when the persona state (friend information) of a friend changes

            // for this sample we'll simply display the names of the friends
            Console.WriteLine("State change: {0}", callback.Name);
        }

        static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }

        static void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
        {
            Console.WriteLine("Updating sentryfile...");

            byte[] sentryHash = CryptoHelper.SHAHash(callback.Data);

            // write out our sentry file
            // ideally we'd want to write to the filename specified in the callback
            // but then this sample would require more code to find the correct sentry file to read during logon
            // for the sake of simplicity, we'll just use "sentry.bin"
            File.WriteAllBytes("sentry.bin", callback.Data);

            // inform the steam servers that we're accepting this sentry file
            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
            {
                JobID = callback.JobID,

                FileName = callback.FileName,

                BytesWritten = callback.BytesToWrite,
                FileSize = callback.Data.Length,
                Offset = callback.Offset,

                Result = EResult.OK,
                LastError = 0,

                OneTimePassword = callback.OneTimePassword,

                SentryFileHash = sentryHash,
            });

            Console.WriteLine("Done!");
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

            string[] arr1 = new string[] { steamid, steampassword, steamAuthCode, steamAuthCode};
           
            MainLogin(arr1);
        }

        private void cancelLogin_Click(object sender, RoutedEventArgs e)
        {
            steamLogin.Visibility = Visibility.Hidden;
            userId.Foreground = Brushes.LightGray;
            userId.Text = "UserID";
        }



    }

}