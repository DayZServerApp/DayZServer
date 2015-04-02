using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using Caliburn.Micro;
using zombiesnu.DayZeroLauncher.App.Ui;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class ServerList : ViewModelBase,
		IHandle<RefreshServerRequest>
	{
		private bool _downloadingServerList;
		private bool _isRunningRefreshBatch;
		private ObservableCollection<Server> _items;
		private ObservableCollection<Server> _oldItems;

		private ServerBatchRefresher _refreshAllBatch;

		public ServerList()
		{
			Items = new ObservableCollection<Server>();
		}

		public ServerBatchRefresher RefreshAllBatch
		{
			get { return _refreshAllBatch; }
			private set
			{
				_refreshAllBatch = value;
				PropertyHasChanged("RefreshAllBatch");
			}
		}

		public ObservableCollection<Server> Items
		{
			get { return _items; }
			private set
			{
				_items = value;
				PropertyHasChanged("Items");
			}
		}

		public bool DownloadingServerList
		{
			get { return _downloadingServerList; }
			set
			{
				_downloadingServerList = value;
				PropertyHasChanged("DownloadingServerList");
			}
		}

		public void Handle(RefreshServerRequest message)
		{
			if (_isRunningRefreshBatch)
				return;

			_isRunningRefreshBatch = true;
			App.Events.Publish(new RefreshingServersChange(true));
			RefreshAllBatch = message.Batch;
			RefreshAllBatch.RefreshAllComplete += RefreshAllBatchOnRefreshAllComplete;
			RefreshAllBatch.RefreshAll();
		}

		public void GetAndUpdateAll()
		{
			GetAll(() => UpdateAll());
		}

		private void GetAll(Action uiThreadOnComplete)
		{
			DownloadingServerList = true;
			new Thread(() =>
			{
				List<Server> servers = GetAllSync();
				Execute.OnUiThread(() =>
				{
					_oldItems = Items;
					Items = new ObservableCollection<Server>(servers);
					DownloadingServerList = false;
					if (uiThreadOnComplete != null)
						uiThreadOnComplete();
				});
			}).Start();
		}

		private List<Server> GetAllSync()
		{
			string list = "";
			{
				string serverListUrl = "https://zombies.nu/serverlist.txt";
				LocatorInfo locator = CalculatedGameSettings.Current.Locator;
				if (locator != null && locator.ServerListUrl != null)
					serverListUrl = locator.ServerListUrl;

				if (!string.IsNullOrWhiteSpace(serverListUrl))
				{
					using (var wc = new WebClient())
					{
						try
						{
							list = wc.DownloadString(new Uri(serverListUrl));
						}
						catch (Exception)
						{
						}
					}
				}
			}

			if (string.IsNullOrEmpty(list))
				return new List<Server>(); //Empty list.. Too bad.

			List<Server> fullList = list
				.Split('\n').Select(line =>
				{
					string[] serverInfo = line.Split(';');
					var server = new Server("", 0, "", "",0);
					if (serverInfo.Count() > 5)
					{
						string queryHostname = serverInfo[1];
						ushort joinPort = (ushort)serverInfo[2].TryInt();
						string password = serverInfo[3];
						string mod = serverInfo[4];
						ushort queryPort = (ushort)serverInfo[5].TryInt();

						server = new Server(queryHostname, joinPort, password, mod, queryPort);
					}

					server.Settings = new SortedDictionary<string, string>
					{
						{"hostname", serverInfo[0]}
					};

					return server;
				}).ToList();

			return fullList;
		}

		public void UpdateAll()
		{
			var batch = new ServerBatchRefresher("Refreshing all servers...", Items, _oldItems);
			App.Events.Publish(new RefreshServerRequest(batch));
		}

		private void RefreshAllBatchOnRefreshAllComplete()
		{
			RefreshAllBatch.RefreshAllComplete -= RefreshAllBatchOnRefreshAllComplete;
			_isRunningRefreshBatch = false;
			App.Events.Publish(new RefreshingServersChange(false));
		}
	}

	public class RefreshingServersChange
	{
		public RefreshingServersChange(bool isRunning)
		{
			IsRunning = isRunning;
		}

		public bool IsRunning { get; set; }
	}
}