using System;
using System.Runtime.Serialization;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	[DataContract]
	public class RecentServer : BindableBase
	{
		[DataMember] private readonly string _ipAddress;
		[DataMember] private readonly string _name;
		[DataMember] private readonly DateTime _on;
		[DataMember] private readonly int _port;

		private Server _server;

		public RecentServer(Server server, DateTime on)
		{
			_on = @on;
			_ipAddress = server.QueryHost;
			_port = server.QueryPort;
			_name = server.Name;
		}

		public Server Server
		{
			get { return _server; }
			set
			{
				_server = value;
				PropertyHasChanged("Server");
			}
		}

		public DateTime On
		{
			get { return _on; }
		}

		public string Ago
		{
			get { return AgoText.Ago(On); }
		}

		public bool Matches(Server server)
		{
			return server.QueryHost == _ipAddress && server.QueryPort == _port;
		}

		/*public Server CreateServer()
		{
			Server = new Server(_ipAddress, _port);
			Server.Settings = new SortedDictionary<string, string>()
			                  	{
			                  		{"hostname",_name}
			                  	};
			return Server;
		}
        */

		public void RefreshAgo()
		{
			PropertyHasChanged("Ago");
		}
	}

	public static class AgoText
	{
		public static string Ago(DateTime date)
		{
			const int SECOND = 1;
			const int MINUTE = 60*SECOND;
			const int HOUR = 60*MINUTE;
			const int DAY = 24*HOUR;
			const int MONTH = 30*DAY;

			var ts = new TimeSpan(DateTime.Now.Ticks - date.Ticks);
			double delta = Math.Abs(ts.TotalSeconds);


			if (delta < 0)
			{
				return "not yet";
			}
			if (delta < 1*MINUTE)
			{
				return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
			}
			if (delta < 2*MINUTE)
			{
				return "a minute ago";
			}
			if (delta < 45*MINUTE)
			{
				return ts.Minutes + " minutes ago";
			}
			if (delta < 90*MINUTE)
			{
				return "an hour ago";
			}
			if (delta < 24*HOUR)
			{
				return ts.Hours + " hours ago";
			}
			if (delta < 48*HOUR)
			{
				return "yesterday";
			}
			if (delta < 30*DAY)
			{
				return ts.Days + " days ago";
			}
			if (delta < 12*MONTH)
			{
				int months = Convert.ToInt32(Math.Floor((double) ts.Days/30));
				return months <= 1 ? "one month ago" : months + " months ago";
			}
			int years = Convert.ToInt32(Math.Floor((double) ts.Days/365));
			return years <= 1 ? "one year ago" : years + " years ago";
		}
	}
}