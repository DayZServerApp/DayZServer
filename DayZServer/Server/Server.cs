using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class Server : BindableBase, IEquatable<Server>
	{
		public static Regex ServerTimeRegex = new Regex(@"((GmT|Utc)[\s]*(?<Offset>([+]|[-])[\s]?[\d]{1,2})?)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private readonly string _queryHost;
		private readonly ushort _queryPort;
		private readonly string _mod;
		private readonly string _password;
		private IPAddress _joinIpAddress; //can be updated by queryClient
		private ushort _joinPort; //can be updated by queryClient
		private readonly ServerQueryClient _queryClient;
		public string LastException;
		private Version _arma2Version;
		private string _dayZVersion;
		private bool? _hasNotes;
		private string _hostName;
		private ServerInfo _info;
		private bool _isUpdating;
		private string _name;
		private long _ping;
		private ObservableCollection<Player> _players;
		private SortedDictionary<string, string> _settings;

		public Server(string hostname, ushort port, string password, string mod, ushort queryPort)
		{
			_queryHost = hostname;
			_queryPort = queryPort;

			_joinIpAddress = null;
			_joinPort = port;

			_password = password;
			_mod = mod;

			_queryClient = new ServerQueryClient(this, _queryHost, _queryPort);
			Settings = new SortedDictionary<string, string>();
			Players = new ObservableCollection<Player>();
			Info = new ServerInfo(null, null);
		}

		//unique id for hashing and putting in sets (that doesn't change)
		public string Id
		{
			get { return "[" + QueryHost + "]" + ":" + QueryPort; }
		}

		public string Name
		{
			get
			{
				if (string.IsNullOrEmpty(_name))
				{
					_name = CleanServerName(ServerName);
				}
				return _name;
			}
		}

		public string ServerName
		{
			get
			{
				if (string.IsNullOrEmpty(_hostName))
				{
					_hostName = GetSettingOrDefault("hostname");
				}
				return _hostName;
			}
		}

		public bool IsFavorite
		{
			get { return UserSettings.Current.IsFavorite(this); }
			set
			{
				if (value)
					UserSettings.Current.AddFavorite(this);
				else
					UserSettings.Current.RemoveFavorite(this);
				PropertyHasChanged("IsFavorite");
			}
		}

		public bool IsSameArmaAndDayZVersion
		{
			get { return IsSameArma2OAVersion && IsSameDayZVersion; }
		}

		public bool IsSameArma2OAVersion
		{
			get
			{
				int? reqBuild = GetSettingOrDefault("reqBuild").TryIntNullable();
				if (reqBuild == null)
					return false;

				GameVersions versions = CalculatedGameSettings.Current.Versions;
				if (versions == null)
					return false;

				GameVersion bestVer = versions.BestVersion;
				if (bestVer == null)
					return false;

				return ((bestVer.BuildNo ?? 0) >= reqBuild);
			}
		}

		public bool IsSameDayZVersion
		{
			get
			{
				string modContentVersion = CalculatedGameSettings.Current.ModContentVersion;
				if (modContentVersion == null)
					return false;

				if (DayZVersion == null)
					return false;

				return modContentVersion.EndsWith(DayZVersion, StringComparison.OrdinalIgnoreCase);
			}
		}

		public int? CurrentPlayers
		{
			get { return GetSettingOrDefault("numplayers").TryIntNullable(); }
		}

		public int? MaxPlayers
		{
			get { return GetSettingOrDefault("maxplayers").TryIntNullable(); }
		}

		public DateTime? ServerTime
		{
			get
			{
				string name = GetSettingOrDefault("hostname");
				if (string.IsNullOrWhiteSpace(name))
					return null;

				Match match = ServerTimeRegex.Match(name);
				if (!match.Success)
					return null;

				string offset = match.Groups["Offset"].Value.Replace(" ", "");
				if (offset == "") offset = "0";
				int offsetInt = int.Parse(offset);

				return DateTime.UtcNow
					.AddHours(offsetInt);
			}
		}

		public SortedDictionary<string, string> Settings
		{
			get { return _settings; }
			internal set
			{
				_settings = value;
				if (_settings != null && _settings.ContainsKey("hostname"))
				{
					if (_settings["hostname"] != null)
						_name = _settings["hostname"];
				}
				Info = new ServerInfo((ServerDifficulty?) Difficulty, Name);
				PropertyHasChanged("Settings");
				PropertyHasChanged("Name");
				PropertyHasChanged("CurrentPlayers");
				PropertyHasChanged("MaxPlayers");
				PropertyHasChanged("ServerTime");
				PropertyHasChanged("HasPassword");
				PropertyHasChanged("Difficulty");
				NotifyGameVersionChanged();
			}
		}

		public ServerInfo Info
		{
			get { return _info; }
			private set
			{
				_info = value;
				PropertyHasChanged("Info");
			}
		}

		public bool IsUpdating
		{
			get { return _isUpdating; }
			private set
			{
				_isUpdating = value;
				Execute.OnUiThread(() => PropertyHasChanged("IsUpdating"));
			}
		}

		public long Ping
		{
			get
			{
				if (LastException != null)
				{
					return 10*1000;
				}
				return _ping;
			}
			set
			{
				_ping = value;
				PropertyHasChanged("Ping");
			}
		}

		public ObservableCollection<Player> Players
		{
			get { return _players; }
			private set
			{
				_players = value;
				PropertyHasChanged("Players");
			}
		}

		public IPAddress JoinAddress
		{
			get 
			{
				if (_joinIpAddress == null)
					_joinIpAddress = Dns.GetHostAddresses(QueryHost)[0];

				return _joinIpAddress;
			}
			set { _joinIpAddress = value; }
		}

		public ushort JoinPort
		{
			get { return _joinPort;	}
			set { _joinPort = value; }
		}

		public string QueryHost
		{
			get { return _queryHost; }
		}

		public ushort QueryPort
		{
			get { return _queryPort; }
		}

		public string Mod
		{
			get { return _mod; }
		}

		public string Password
		{
			get { return _password; }
		}

		public int? Difficulty
		{
			get { return GetSettingOrDefault("difficulty").TryIntNullable(); }
		}

		public int FreeSlots
		{
			get
			{
				if (MaxPlayers != null && CurrentPlayers != null)
				{
					return (int) (MaxPlayers - CurrentPlayers);
				}
				return 0;
			}
		}

		public bool IsEmpty
		{
			get { return CurrentPlayers == null || CurrentPlayers == 0; }
		}

		public string LastJoinedOn
		{
			get
			{
				RecentServer recent = UserSettings.Current.RecentServers
					.OrderByDescending(x => x.On)
					.FirstOrDefault(x => x.Server == this);
				if (recent == null)
					return "Never";

				return recent.Ago;
			}
		}

		public string Notes
		{
			get { return UserSettings.Current.GetNotes(this); }
			set
			{
				UserSettings.Current.SetNotes(this, value);
				_hasNotes = !string.IsNullOrEmpty(value);
				PropertyHasChanged("Notes", "HasNotes");
			}
		}

		public bool HasNotes
		{
			get
			{
				if (_hasNotes != null)
					return (bool) _hasNotes;
				_hasNotes = UserSettings.Current.HasNotes(this);
				return (bool) _hasNotes;
			}
		}

		public bool? IsNight
		{
			get
			{
				DateTime? serverTime = ServerTime;
				if (serverTime == null)
					return null;

				return serverTime.Value.Hour < 5 || serverTime.Value.Hour > 19;
			}
		}

		public bool HasPassword
		{
			get { return GetSettingOrDefault("password").TryInt() > 0; }
		}

		public Version Arma2Version
		{
			get
			{
				if (_arma2Version == null)
				{
					string arma2VersionString = GetSettingOrDefault("gamever");
					Version.TryParse(arma2VersionString, out _arma2Version);
				}
				return _arma2Version;
			}
		}

		public string DayZVersion
		{
			get
			{
				if (_dayZVersion == null)
					_dayZVersion = GetDayZVersionString(Name);

				return _dayZVersion;
			}
		}

		public bool ProtectionEnabled
		{
			get
			{
				return GetSettingOrDefault("verifySignatures").TryInt() > 0
				       && GetSettingOrDefault("sv_battleye").TryInt() > 0;
			}
		}

		public bool Equals(Server other)
		{
			if (other == null)
				return false;

			if (other.QueryPort != QueryPort)
				return false;

			return other.QueryHost.Equals(QueryHost, StringComparison.OrdinalIgnoreCase);
		}

		public void NotifyGameVersionChanged()
		{
			PropertyHasChanged("IsSameArma2OAVersion", "IsSameDayZVersion", "IsSameArmaAndDayZVersion");
		}

		public bool MatchesIpPort(string ipAddr, int port)
		{
			if (JoinPort != port)
				return false;

			IPAddress ourIpAddress = Dns.GetHostAddresses(ipAddr.Trim())[0];
			return JoinAddress.Equals(ourIpAddress);
		}

		private string GetSettingOrDefault(string settingName)
		{
			if (Settings.ContainsKey(settingName))
			{
				return Settings[settingName];
			}
			return null;
		}

		public void Update(bool supressRefresh = false)
		{
			try
			{
				IsUpdating = true;
				ServerQueryResult serverResult = _queryClient.Execute();
				Execute.OnUiThread(() =>
				{
					JoinAddress = serverResult.IP;
					JoinPort = serverResult.Port;
					Players = new ObservableCollection<Player>(serverResult.Players.OrderBy(x => x.Name));
					LastException = null;

					//Ugly hack to go around 63 char limit, remove when fixed
					if (Settings.ContainsKey("hostname"))
					{
						string oldHostname = Settings["hostname"];
						string newHostname = serverResult.Settings["hostname"];

						if (newHostname.Length == (64-1)) //it probably got cut off
						{
							if (newHostname.Length < oldHostname.Length)
								serverResult.Settings["hostname"] = newHostname + oldHostname.Substring(newHostname.Length);
						}						
					}

					Settings = serverResult.Settings;				
					Ping = serverResult.Ping;
					App.Events.Publish(new ServerUpdated(this, supressRefresh));
				});
			}
			catch (Exception ex)
			{
				Execute.OnUiThread(() =>
				{
					LastException = ex.Message;
					PropertyHasChanged("Name", "Ping");
					App.Events.Publish(new ServerUpdated(this, supressRefresh));
				});
			}
			finally
			{
				IsUpdating = false;
			}
		}

		private static string CleanServerName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return name;
			}

			string cleanName = name.Trim();

			cleanName = Regex.Replace(cleanName, @"^DayZ\s*(Zombie){0,1}\s*(RPG){0,1}\s*-\s*", "", RegexOptions.IgnoreCase);

			return cleanName.Trim();
		}

		private static string GetDayZVersionString(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}

			Match match = Regex.Match(name, @"\d(\.\d){1,3}");
			if (!match.Success)
				return null;

			int strlen = match.Value.Length;
			for (int i = match.Index + strlen; i < name.Length; i++)
			{
				if (name[i] == '(' || name[i] == ')' || name[i] == ' ' || name[i] == '\t' || name[i] == '/')
					break;

				strlen++;
			}

			return name.Substring(match.Index, strlen);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public void BeginUpdate(Action<Server> onComplete, bool supressRefresh = false)
		{
			new Thread(() =>
			{
				try
				{
					Update(supressRefresh);
				}
				finally
				{
					onComplete(this);
				}
			}, 1).Start();
		}
	}
}