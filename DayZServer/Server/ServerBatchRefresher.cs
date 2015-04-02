using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class ServerBatchRefresher : BindableBase
	{
		private readonly ICollection<Server> _items;
		private readonly ICollection<Server> _removedItems;
		private bool _isUpdating;
		private int _processed;
		private int _processedServersCount;

		public ServerBatchRefresher(string inProgressText, ICollection<Server> items, ICollection<Server> _oldItems = null)
		{
			_items = items;
			_removedItems = new List<Server>();
			if (_oldItems != null)
			{
				foreach (Server item in _oldItems)
				{
					if (_items.Count(x => x.Id.Equals(item.Id)) < 1) //this server has been removed
						_removedItems.Add(item);
				}
			}

			InProgressText = inProgressText;
		}

		public ServerBatchRefresher()
		{
		}

		public string InProgressText { get; set; }

		public int UnprocessedServersCount
		{
			get { return _items.Count - ProcessedServersCount; }
		}

		public int TotalCount
		{
			get { return _items.Count; }
		}

		public int ProcessedServersCount
		{
			get { return _processedServersCount; }
			set
			{
				_processedServersCount = value;
				PropertyHasChanged("ProcessedServersCount", "UnprocessedServersCount");
			}
		}

		public event Action RefreshAllComplete;

		public void RefreshAll()
		{
			if (_isUpdating)
				return;

			var incrementLock = new object();

			_isUpdating = true;
			ProcessedServersCount = 0;
			_processed = 0;

			ICollection<Server> items = _items; //we got the new downloaded list when we were made
			int totalCount = items.Count;
			var t = new Thread(() =>
			{
				try
				{
					while (_processed <= totalCount)
					{
						Execute.OnUiThread(() => { ProcessedServersCount = _processed; });
						Thread.Sleep(150);
						if (_processed == totalCount)
						{
							_isUpdating = false;
							break;
						}
					}
				}
				finally
				{
					Execute.OnUiThread(() => { ProcessedServersCount = totalCount; });
					if (RefreshAllComplete != null)
						RefreshAllComplete();
					_isUpdating = false;
				}
			});
			t.IsBackground = true;
			t.Start();

			var serverRemovals = new Action<Action>[1];
			serverRemovals[0] = (Action onComplete) =>
			{
				Execute.OnUiThread(() =>
				{
					foreach (Server remSrv in _removedItems)
						App.Events.Publish(new ServerUpdated(remSrv, false, true));
				});

				if (onComplete != null)
					onComplete();
			};

			Action<Action>[] serverUpdates = items
				.Select<Server, Action<Action>>(server =>
					onComplete =>
						server.BeginUpdate(doubleDispatchServer =>
						{
							try
							{
								onComplete();
							}
							finally
							{
								lock (incrementLock)
								{
									_processed++;
								}
							}
						}))
				.ToArray();

			ServerRefreshQueue.Instance.Enqueue(serverRemovals);
			ServerRefreshQueue.Instance.Enqueue(serverUpdates);
		}
	}
}