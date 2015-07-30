using System;
using System.Activities;
using System.ComponentModel;

namespace Hasseware.Activities.Statements
{
	public sealed class SubscribeFileChanges : NativeActivity
	{
		private Variable<Bookmark> bookmark;

		#region Constructors

		public SubscribeFileChanges()
		{
			this.WatchSubfolders = false;
			this.bookmark = new Variable<Bookmark>()
			{
				Name = "SubscriptionBookmark"
			};
		}

		public SubscribeFileChanges(InArgument<FileChangeSubscriptionHandle> subscriptionHandle,
			InArgument<string> folder, InArgument<string> pattern, bool subfolders) : this()
		{
			this.SubscriptionHandle = subscriptionHandle;
			this.WatchFolder = folder;
			this.WatchPattern = pattern;
			this.WatchSubfolders = subfolders;
		}

		#endregion

		[RequiredArgument]
		public InArgument<FileChangeSubscriptionHandle> SubscriptionHandle { get; set; }

		[RequiredArgument]
		public InArgument<string> WatchFolder { get; set; }

		public InArgument<string> WatchPattern { get; set; }

		[DefaultValue(false)]
		public bool WatchSubfolders { get; set; }

		protected override bool CanInduceIdle
		{
			get { return true; }
		}

		protected override void CacheMetadata(NativeActivityMetadata metadata)
		{
			// Tell the runtime that we need this extension
			metadata.RequireExtension(typeof(Hosting.FolderWatcherExtension));

			// Provide a Func<T> to create the extension if it does not already exist
			metadata.AddDefaultExtensionProvider(() => new Hosting.FolderWatcherExtension());

			if (this.SubscriptionHandle == null)
			{
				metadata.AddValidationError(String.Format(
					Properties.Resources.SubscriptionHandleCannotBeNull, base.DisplayName));
			}
			metadata.AddArgument(new RuntimeArgument("SubscriptionHandle", typeof(FileChangeSubscriptionHandle), ArgumentDirection.In, true));
			metadata.AddArgument(new RuntimeArgument("WatchFolder", typeof(string), ArgumentDirection.In, true));
			metadata.AddArgument(new RuntimeArgument("WatchPattern", typeof(string), ArgumentDirection.In));
			metadata.AddImplementationVariable(this.bookmark);
		}

		protected override void Execute(NativeActivityContext context)
		{
			var subscriptionHandle = this.SubscriptionHandle.Get(context);

			string folder = context.GetValue(this.WatchFolder);

			if (!System.IO.Directory.Exists(folder))
				throw new OperationCanceledException(String.Format("The path \"{0}\" is not a directory or does not exist.", folder));

			var extension = context.GetExtension<Hosting.FolderWatcherExtension>();

            var bookmark = context.CreateBookmark(String.Format("SubscribeFileChanges_{0:N}", Guid.NewGuid()));
			this.bookmark.Set(context, bookmark);

			subscriptionHandle.Initialize(extension, bookmark, folder,
				context.GetValue(this.WatchPattern), this.WatchSubfolders, base.DisplayName);
		}

		protected override void Abort(NativeActivityAbortContext context)
		{
			var subscriptionHandle = this.SubscriptionHandle.Get(context);
			subscriptionHandle.ReleaseSubscription(context.GetExtension<Hosting.FolderWatcherExtension>());
			base.Abort(context);
		}

		protected override void Cancel(NativeActivityContext context)
		{
			var subscriptionHandle = this.SubscriptionHandle.Get(context);
			subscriptionHandle.ReleaseSubscription(context.GetExtension<Hosting.FolderWatcherExtension>());
			context.RemoveAllBookmarks();
			context.MarkCanceled();
		}
	}
}

