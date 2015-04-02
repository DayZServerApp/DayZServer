namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class Player
	{
		private readonly Server _server;

		public Player(Server server)
		{
			_server = server;
		}

		public string Hash
		{
			get { return _server.Id + "(" + Name + ")"; }
		}

		public string Name { get; set; }
		public int Score { get; set; }
		public int Deaths { get; set; }

		public Server Server
		{
			get { return _server; }
		}
	}
}