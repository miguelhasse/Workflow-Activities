using System;
using System.Activities;
using System.Activities.Hosting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Hasseware.Activities.Hosting
{
	internal sealed class FolderWatcherExtension : IWorkflowInstanceExtension
	{
		private WorkflowInstanceProxy instance;
		private ConcurrentDictionary<int, FolderSubscription> contexts;
		private ConcurrentDictionary<Guid, int> subscriptions;

		IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
		{
			yield break;
		}

		void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)
		{
			this.instance = instance;
			this.contexts = new ConcurrentDictionary<int, FolderSubscription>();
			this.subscriptions = new ConcurrentDictionary<Guid, int>();
		}

		public void RegisterListener(Bookmark bookmark, string folder, string pattern, bool subfolders)
		{
			int key = GenerateSubscriptionKey(folder, pattern, subfolders);
			RegisterListenerInternal(bookmark, key, folder, pattern, subfolders).Start();
		}

		public void UnregisterListener(Bookmark bookmark, string folder, string pattern, bool subfolders)
		{
			int key = GenerateSubscriptionKey(folder, pattern, subfolders);
			UnregisterListenerInternal(bookmark, key);
		}

		public string Subscribe(Bookmark bookmark, string folder, string pattern, bool subfolders)
		{
			Guid subscriptionId = Guid.NewGuid();
			int key = GenerateSubscriptionKey(folder, pattern, subfolders);

			if (this.subscriptions.TryAdd(subscriptionId, key))
			{
				RegisterListenerInternal(null, key, folder, pattern, subfolders).Start();
				instance.BeginResumeBookmark(bookmark, null, (a) => instance.EndResumeBookmark(a), null);
				return subscriptionId.ToString();
			}
			return null;
		}

		public void RegisterReceiveNotification(Bookmark bookmark, string subscriptionId)
		{
			Guid id = new Guid(subscriptionId);
			int value;

			if (this.subscriptions.TryGetValue(id, out value))
			{
				FolderSubscription subscription;
				if (contexts.TryGetValue(value, out subscription))
					subscription.GetOrAdd(bookmark, id);
			}
		}

		public void UnregisterReceiveNotification(Bookmark bookmark, string subscriptionId)
		{
			Guid? id;
			int value;

			if (this.subscriptions.TryGetValue(new Guid(subscriptionId), out value))
			{
				FolderSubscription subscription;
				if (contexts.TryGetValue(value, out subscription))
					subscription.TryRemove(bookmark, out id);
			}
		}

		public void Unsubscribe(string subscriptionId)
		{
			Guid id = new Guid(subscriptionId);
			int value;

			if (this.subscriptions.TryRemove(id, out value))
			{
				FolderSubscription subscription;
				if (contexts.TryGetValue(value, out subscription))
				{
					subscription.Remove(id);
					UnregisterListenerInternal(null, value);
				}
			}
		}

		private void FileSystemEventNotify(Bookmark bookmark, Statements.FileChangeEvent sysevent)
		{
            instance.BeginResumeBookmark(bookmark, sysevent, (a) => instance.EndResumeBookmark(a), null);
		}

		private void FileSystemErrorNotify(Bookmark bookmark, Exception exception)
		{
			instance.BeginResumeBookmark(bookmark, exception, (a) => instance.EndResumeBookmark(a), null);
		}

		private FolderSubscription RegisterListenerInternal(Bookmark bookmark, int key, string folder, string pattern, bool subfolders)
		{
			FolderSubscription subscription;
			if (!contexts.TryGetValue(key, out subscription))
			{
				subscription = new FolderSubscription(this, folder, pattern, subfolders);
				if (!contexts.TryAdd(key, subscription)) throw new InvalidOperationException("Unable to register watcher.");
			}
			if (bookmark != null)
				subscription.GetOrAdd(bookmark, (Guid?)null);
			return subscription;
		}

		private FolderSubscription UnregisterListenerInternal(Bookmark bookmark, int key)
		{
			FolderSubscription subscription;
			if (contexts.TryGetValue(key, out subscription))
			{
				if (subscription.Count == 0)
				{
					subscription.Stop();
					contexts.TryRemove(key, out subscription);
				}
				else if (bookmark != null)
				{
					Guid? subscriptionId;
					subscription.TryRemove(bookmark, out subscriptionId);
				}
				return subscription;
			}
			return null;
		}

		internal static int GenerateSubscriptionKey(string folder, string pattern, bool subfolders)
		{
			int capacity = folder.Length + (pattern == null ? 0 : pattern.Length) + 1;
			var sb = new System.Text.StringBuilder(subfolders ? "+" : String.Empty, capacity);
			sb.Append(folder.ToLowerInvariant());

			if (pattern != null)
			{
				sb.Append('/');
				sb.Append(pattern.ToLowerInvariant());
			}
			return sb.ToString().GetHashCode();
		}

		class FolderSubscription : ConcurrentDictionary<Bookmark, Guid?>
		{
			private FolderWatcherExtension instance;
			private System.IO.FileSystemWatcher watcher;

			public FolderSubscription(FolderWatcherExtension instance)
			{
				this.instance = instance;
			}

			public FolderSubscription(FolderWatcherExtension instance, string folder, string pattern, bool subfolders)
				: this(instance)
			{
				this.watcher = new System.IO.FileSystemWatcher(folder, pattern ?? "*.*") { IncludeSubdirectories = subfolders };
			}

			public void Start()
			{
				if (this.watcher.EnableRaisingEvents == false)
				{
					this.watcher.Created += OnWatcherEvent;
					this.watcher.Changed += OnWatcherEvent;
					this.watcher.Deleted += OnWatcherEvent;
					this.watcher.Renamed += OnFileRenamedEvent;
					this.watcher.Error += OnWatcherError;
					this.watcher.EnableRaisingEvents = true;
				}
			}

			public void Stop()
			{
				if (this.watcher.EnableRaisingEvents == true)
				{
					this.watcher.EnableRaisingEvents = false;
					this.watcher.Error -= OnWatcherError;
					this.watcher.Renamed -= OnFileRenamedEvent;
					this.watcher.Deleted -= OnWatcherEvent;
					this.watcher.Changed -= OnWatcherEvent;
					this.watcher.Created -= OnWatcherEvent;
				}
			}

			public void Remove(Guid subscriptionId)
			{
				Guid? id;
				foreach (Bookmark bookmark in base.Keys.ToList())
				{
					if (base.TryGetValue(bookmark, out id) && id == subscriptionId)
						base.TryRemove(bookmark, out id);
				}
			}

			private void OnWatcherEvent(object sender, System.IO.FileSystemEventArgs e)
			{
				var sysevent = new Statements.FileChangeEvent
				{
					ChangeTime = DateTime.UtcNow,
					ChangeType = e.ChangeType.ToString(),
					Name = e.Name,
					FullPath = e.FullPath
				};
				NotifyBookmarks(b => instance.FileSystemEventNotify(b, sysevent));
			}

			private void OnFileRenamedEvent(object sender, System.IO.RenamedEventArgs e)
			{
				var sysevent = new Statements.FileChangeEvent
				{
					ChangeTime = DateTime.UtcNow,
					ChangeType = e.ChangeType.ToString(),
					Name = e.Name,
					FullPath = e.FullPath,
					OldPath = e.OldFullPath
				};
				NotifyBookmarks(b => instance.FileSystemEventNotify(b, sysevent));
			}

			private void OnWatcherError(object sender, System.IO.ErrorEventArgs e)
			{
				Exception ex = e.GetException();
				NotifyBookmarks(b => instance.FileSystemErrorNotify(b, ex));
			}

			private void NotifyBookmarks(Action<Bookmark> action)
			{
				Guid? subscriptionId;
				foreach (Bookmark bookmark in base.Keys.ToList())
				{
					if (base.TryGetValue(bookmark, out subscriptionId))
					{
						if (!subscriptionId.HasValue)
							base.TryRemove(bookmark, out subscriptionId);

						action(bookmark);
					}
				}
			}
		}
	}
}
