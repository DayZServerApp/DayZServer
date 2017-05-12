using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;

namespace DayZServer.Events
{
    public static class UIEvents
    {
        #region Machine Auth Events

        public static event UpdateMachineAuthHandler UpdateMachineAuth;
        public delegate void UpdateMachineAuthHandler(SteamUser.UpdateMachineAuthCallback callbackStatus);
        public static void OnUpdateMachineAuth(SteamUser.UpdateMachineAuthCallback callbackStatus)
        {
            Debug.WriteLine("Event: OnUpdateMachineAuth");
            if (UpdateMachineAuth != null)
                UpdateMachineAuth(callbackStatus);
        }
        #endregion

        #region User Logon Events

        public static event UserHasLoggedOnHandler UserHasLoggedOn;
        public delegate void UserHasLoggedOnHandler(SteamUser.LoggedOnCallback callbackStatus);
        public static void OnUserHasLoggedOn(SteamUser.LoggedOnCallback callbackStatus)
        {
            Debug.WriteLine("Event: OnUserHasLoggedOn");
            if (UserHasLoggedOn != null)
                UserHasLoggedOn(callbackStatus);
        }

        public static event UserHasLoggedOffHandler UserHasLoggedOff;
        public delegate void UserHasLoggedOffHandler(SteamUser.LoggedOffCallback callbackStatus);
        public static void OnUserHasLoggedOff(SteamUser.LoggedOffCallback callbackStatus)
        {
            Debug.WriteLine("Event: OnUserHasLoggedOff");
            if (UserHasLoggedOff != null)
                UserHasLoggedOff(callbackStatus);
        }

        #endregion

        #region Connection Callbacks

        public static event UserHasDisconnectedHandler UserHasDisconnected;
        public delegate void UserHasDisconnectedHandler(SteamClient.DisconnectedCallback callbackStatus);
        public static void OnUserHasDisconnected(SteamClient.DisconnectedCallback callbackStatus)
        {
            Debug.WriteLine("Event: OnUserHasDisconnected");
            if (UserHasDisconnected != null)
                UserHasDisconnected(callbackStatus);
        }

        #endregion
 
        #region FriendsList Callbacks

        public static event FriendsListHandler FriendsList;
        public delegate void FriendsListHandler(SteamFriends.FriendsListCallback callbackStatus);
        public static void OnFriendsList(SteamFriends.FriendsListCallback callbackStatus)
        {
            Debug.WriteLine("Event: OnFriendsList");
            if (FriendsList != null)
                FriendsList(callbackStatus);
        }

        public static event FriendAddedHandler FriendAdded;
        public delegate void FriendAddedHandler(SteamFriends.FriendAddedCallback callbackStatus);
        public static void OnFriendAdded(SteamFriends.FriendAddedCallback callbackStatus)
        {
            Debug.WriteLine("Event: OnFriendAdded");
            if (FriendAdded != null)
                FriendAdded(callbackStatus);
        }

        public static event PersonaStateHandler PersonaState;
        public delegate void PersonaStateHandler(SteamFriends.PersonaStateCallback callbackStatus);
        public static void OnPersonaState(SteamFriends.PersonaStateCallback callbackStatus)
        {
            Debug.WriteLine("Event: OnPersonaState");
            if (PersonaState != null)
                PersonaState(callbackStatus);
        }

        #endregion
    }
}
