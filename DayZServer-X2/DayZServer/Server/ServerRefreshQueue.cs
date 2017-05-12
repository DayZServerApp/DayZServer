using System;
using System.Collections.Generic;
using System.Threading;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class ServerRefreshQueue
	{
		private readonly object _locker = new object();
		private readonly Queue<Action<Action>> _serverQueue = new Queue<Action<Action>>();
		private int _runningCount;

		static ServerRefreshQueue()
		{
			Instance = new ServerRefreshQueue();
			Instance.Consume();
		}

		public static ServerRefreshQueue Instance { get; private set; }

		public void Enqueue(Action<Action>[] serverUpdates)
		{
			lock (_locker)
			{
				foreach (var server in serverUpdates)
					_serverQueue.Enqueue(server);
				Monitor.PulseAll(_locker);
			}
		}

		public void Consume()
		{
			var t = new Thread(() =>
			{
				while (true)
				{
					Action<Action> server;
					lock (_locker)
					{
						while (_serverQueue.Count == 0 || _runningCount >= (UserSettings.Current.AppOptions.LowPingRate ? 20 : 300))
						{
							Monitor.Wait(_locker);
						}
						server = _serverQueue.Dequeue();
						_runningCount++;
					}
					Thread.Sleep(new Random().Next(5, 17));
					server(() =>
					{
						lock (_locker)
						{
							_runningCount--;
							Monitor.PulseAll(_locker);
						}
					});
				}
			});
			t.IsBackground = true;
			t.Start();
		}
	}
}