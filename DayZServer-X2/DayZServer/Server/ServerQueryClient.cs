using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class ServerQueryClient
	{
		private readonly Server _server;
		private readonly string _queryHost;
		private readonly ushort _queryPort;		

		public ServerQueryClient(Server server, string _querHost, ushort queryPort)
		{
			_server = server;
			_queryHost = _querHost;
			_queryPort = queryPort;
		}

		public ServerQueryResult Execute()
		{
			var ipaddress = Dns.GetHostAddresses(_queryHost)[0];
			var ipendpoint = new IPEndPoint(ipaddress, _queryPort);

			var pingTimer = new Stopwatch();
			var infoRetriever = new SSQLib.SSQL(ipendpoint);		

			pingTimer.Start();
			var serverInfo = infoRetriever.Server();
			pingTimer.Stop();

			var settings = new SortedDictionary<string, string>();
			settings.Add("hostname", serverInfo.Name);
			settings.Add("maxplayers", serverInfo.MaxPlayers);
			settings.Add("numplayers", serverInfo.PlayerCount);
			settings.Add("mapname", serverInfo.Map);
			settings.Add("gamever", serverInfo.Version);
			{
				Version outVer;
				if (Version.TryParse(serverInfo.Version, out outVer))
					settings.Add("reqBuild", outVer.Build.ToString());
			}
			
			settings.Add("password", (serverInfo.Password)?"1":"0");
			settings.Add("vac", (serverInfo.VAC)?"1":"0");
			settings.Add("sv_battleye", (serverInfo.VAC) ? "1" : "0");

			settings.Add("gametype", serverInfo.Folder);
			settings.Add("mod", serverInfo.Game);

			if (!string.IsNullOrEmpty(serverInfo.Keywords))
			{
				bool battleye = false;
				var keywords = serverInfo.Keywords.Split(',');
				foreach (string keyword in keywords)
				{
					if (string.IsNullOrEmpty(keyword))
						continue;
					else if (keyword == "bt")
						battleye = true;
					else if (keyword[0] == 'r') //required version
					{
						string majorVer = keyword.Substring(1);
						settings["reqVersion"] = majorVer.Substring(0,1) + "." + majorVer.Substring(1);
					}
					else if (keyword[0] == 'n') //required? build no
						settings["reqBuild"] = keyword.Substring(1);
				}

				settings["sv_battleye"] = (battleye) ? "1" : "0";
			}

			Dictionary<string, string> serverRules = null;
			try
			{
				serverRules = infoRetriever.Rules();
			}
			catch (Exception) //not really necessary for this to succeed
			{
				serverRules = new Dictionary<string, string>();
			}

			//concatenate long rules
			{
				var rulesArray = new Dictionary<string, string[]>();
				foreach (KeyValuePair<string, string> rule in serverRules)
				{
					var idxOfColon = rule.Key.IndexOf(':');
					if (idxOfColon >= 0)
					{
						var ruleName = rule.Key.Substring(0, idxOfColon);
						var ruleNumData = rule.Key.Substring(idxOfColon + 1).Split('-');

						int ruleIdx = int.Parse(ruleNumData[0]);
						int ruleTotal = int.Parse(ruleNumData[1]);

						if (!rulesArray.ContainsKey(ruleName))
							rulesArray.Add(ruleName, new string[ruleTotal]);
						else if (ruleTotal != rulesArray[ruleName].Length)
							throw new FormatException("Indexed rule size mismatch");

						if (rulesArray[ruleName][ruleIdx] == null)
							rulesArray[ruleName][ruleIdx] = rule.Value;
						else
							rulesArray[ruleName][ruleIdx] += rule.Value;
					}
					else
					{
						var ruleName = rule.Key;
						var ruleData = new string[1];
						ruleData[0] = rule.Value;

						rulesArray.Add(ruleName, ruleData);
					}
				}

				serverRules.Clear();
				foreach (var ruleArrItm in rulesArray)
				{
					StringBuilder sb = new StringBuilder();
					foreach (string strItm in ruleArrItm.Value)
						sb.Append(strItm);

					serverRules.Add(ruleArrItm.Key, sb.ToString());
				}
			}	
	
			//merge server rules to settings
			foreach (var rule in serverRules)
				settings[rule.Key] = rule.Value;

			IEnumerable<SSQLib.PlayerInfo> playersInfo = null;
			try
			{
				playersInfo = infoRetriever.Players();
			}
			catch (Exception) //we dont care if player querying fails, really
			{
				playersInfo = new List<SSQLib.PlayerInfo>();
			}

			var players = new List<Player>();
			foreach (object playerInfo in playersInfo)
			{
				var pinfo = (SSQLib.PlayerInfo)playerInfo;
				var pl = new Player(_server);

				pl.Name = pinfo.Name;
				pl.Score = pinfo.Score;
				pl.Deaths = 0;

				players.Add(pl);
			}

			return new ServerQueryResult
			{
				IP = IPAddress.Parse(serverInfo.IP),
				Port = ushort.Parse(serverInfo.Port),
				Settings = settings,
				Players = players,
				Ping = pingTimer.ElapsedMilliseconds
			};
		}
	}
}