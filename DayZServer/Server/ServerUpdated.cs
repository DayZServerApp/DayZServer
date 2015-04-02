namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class ServerUpdated
	{
		public ServerUpdated(Server server, bool supressRefresh, bool isRemoved = false)
		{
			Server = server;
			SupressRefresh = supressRefresh;
			IsRemoved = isRemoved;
		}

		public Server Server { get; set; }
		public bool SupressRefresh { get; set; }
		public bool IsRemoved { get; set; }
	}
}