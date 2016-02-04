using System;
using System.Activities;
using System.ComponentModel;

namespace Hasseware.Activities.Statements
{
	public sealed class FolderWatcher : NativeActivity<string>
	{
		private readonly Variable<NoPersistHandle> noPersistHandle;
		private Variable<Bookmark> bookmarkWatcher;

		private BookmarkCallback bookmarkCallback;

		#region Constructors

		public FolderWatcher()
		{
			// create the variables to hold the NoPersistHandle and Bookmark
			this.noPersistHandle = new Variable<NoPersistHandle>();
			this.bookmarkWatcher = new Variable<Bookmark>();

			this.bookmarkCallback = new BookmarkCallback(OnFileWatcherCallback);
			this.WatchSubfolders = false;
		}

		#endregion

		[RequiredArgument]
		public InArgument<string> WatchFolder { get; set; }

		public InArgument<string> WatchPattern { get; set; }

		[DefaultValue(false)]
		public bool WatchSubfolders { get; set; }

		/// <summary>
		/// Gets the file watcher bookmark callback.
		/// </summary>
		public BookmarkCallback FileWatcherBookmarkCallback
		{
			get
			{
				return this.bookmarkCallback ?? InlineAssignHelper(ref this.bookmarkCallback,
					new BookmarkCallback(OnFileWatcherCallback));
			}
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the activity can cause the workflow to become idle.
		/// </summary>
		/// <returns>true if the activity can cause the workflow to become idle, otherwise false. This value is false by default.</returns>
		protected override bool CanInduceIdle
		{
			get { return true; }
		}

		/// <summary>
		/// Creates and validates a description of the activity’s arguments, variables, child activities, and activity delegates.
		/// </summary>
		/// <param name="metadata">The activity’s metadata that encapsulates the activity’s arguments, variables, child activities, and activity delegates.</param>
		protected override void CacheMetadata(NativeActivityMetadata metadata)
		{
			// Tell the runtime that we need this extension
			metadata.RequireExtension(typeof(Hosting.FolderWatcherExtension));

			// Provide a Func<T> to create the extension if it does not already exist
			metadata.AddDefaultExtensionProvider(() => new Hosting.FolderWatcherExtension());

			metadata.AddArgument(new RuntimeArgument("WatchFolder", typeof(string), ArgumentDirection.In, true));
			metadata.AddArgument(new RuntimeArgument("WatchPattern", typeof(string), ArgumentDirection.In));

            metadata.AddImplementationVariable(this.noPersistHandle);
			metadata.AddImplementationVariable(this.bookmarkWatcher);
		}

		/// <summary>
		/// When implemented in a derived class, runs the activity’s execution logic.
		/// </summary>
		/// <param name="context">The execution context in which the activity executes.</param>
		protected override void Execute(System.Activities.NativeActivityContext context)
		{
			// Enter a no persist zone to pin this activity to memory since we are setting up a delegate to receive a callback
            var handle = this.noPersistHandle.Get(context);
			handle.Enter(context);

			// Get (which may create) the extension
			var extension = context.GetExtension<Hosting.FolderWatcherExtension>();

			string folder = context.GetValue(this.WatchFolder);

			if (!System.IO.Directory.Exists(folder))
				throw new OperationCanceledException(String.Format("The path \"{0}\" is not a directory or does not exist.", folder));

			// Set a bookmark - the extension will resume when the FileSystemWatcher is fired
			var bookmark = context.CreateBookmark(String.Format("FolderWatch_{0:N}", Guid.NewGuid()), FileWatcherBookmarkCallback);
			bookmarkWatcher.Set(context, bookmark);

			extension.RegisterListener(bookmark, folder, context.GetValue(this.WatchPattern), this.WatchSubfolders);
		}

		protected override void Cancel(NativeActivityContext context)
		{
			CancelInternal(context);
			context.MarkCanceled();
		}

		/// <summary>
		/// Called when [file watcher callback].
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="bookmark">The bookmark.</param>
		/// <param name="value">The value.</param>
		internal void OnFileWatcherCallback(NativeActivityContext context, Bookmark bookmark, Object data)
		{
			if (bookmarkWatcher.Get(context) != bookmark)
				return;

			CancelInternal(context);

			if (data is FileChangeEvent && !context.IsCancellationRequested)
			{
				// Store the result
				this.Result.Set(context, ((FileChangeEvent)data).FullPath);
			}
			if (data is Exception)
				throw data as Exception;
		}

		private void CancelInternal(NativeActivityContext context)
		{
			var bookmark = bookmarkWatcher.Get(context);

			var extension = context.GetExtension<Hosting.FolderWatcherExtension>();
			extension.UnregisterListener(bookmark, context.GetValue(this.WatchFolder),
				context.GetValue(this.WatchPattern), this.WatchSubfolders);

			this.noPersistHandle.Get(context).Exit(context);
			context.RemoveBookmark(bookmark);
		}

		/// <summary>
		/// Assists inline assign.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="target">The target.</param>
		/// <param name="value">The value.</param><returns></returns>
		private static T InlineAssignHelper<T>(ref T target, T value)
		{
			target = value;
			return value;
		}
	}
}
