using System;
using System.Activities;
using System.Activities.Tracking;
using System.Collections.Generic;
using System.ComponentModel;

namespace Hasseware.Activities.Statements
{
	public sealed class ReceiveFileChanges : NativeActivity<ICollection<FileChangeEvent>>
	{
		private readonly Variable<NoPersistHandle> noPersistHandle;
		private Variable<Bookmark> receiveCompleteBookmark;
		private BookmarkCallback receiveCompleteCallback;

		#region Constructors

		public ReceiveFileChanges()
		{
			this.noPersistHandle = new Variable<NoPersistHandle>();
			this.receiveCompleteBookmark = new Variable<Bookmark>() { Name = "ReceiveBookmark" };
			this.receiveCompleteCallback = new BookmarkCallback(this.OnReceiveNotification);
		}

		public ReceiveFileChanges(InArgument<FileChangeSubscriptionHandle> subscriptionHandle) : this()
		{
			this.SubscriptionHandle = subscriptionHandle;
		}

		#endregion

		[DefaultValue(null)]
		[RequiredArgument]
		public InArgument<FileChangeSubscriptionHandle> SubscriptionHandle { get; set; }

		[DefaultValue(null)]
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
		public ActivityAction Body { get; set; }

		protected override bool CanInduceIdle
		{
			get { return true; }
		}

		protected override void CacheMetadata(NativeActivityMetadata metadata)
		{
			// Tell the runtime that we need this extension
			metadata.RequireExtension(typeof(Hosting.FolderWatcherExtension));

			if (this.Body != null) 
				metadata.AddDelegate(this.Body);

			metadata.AddArgument(new RuntimeArgument("SubscriptionHandle",
				typeof(FileChangeSubscriptionHandle), ArgumentDirection.In, true));

			metadata.AddImplementationVariable(noPersistHandle);
			metadata.AddImplementationVariable(this.receiveCompleteBookmark);
		}

		protected override void Execute(NativeActivityContext context)
		{
			if (this.SubscriptionHandle != null)
			{
				var subscriptionHandle = this.SubscriptionHandle.Get(context);

				if (!subscriptionHandle.IsInitialized)
				{
					throw new ArgumentException("Subscription",
						String.Format(Properties.Resources.HandleNotInitialized, this.DisplayName));
				}

				var emptyList = new List<FileChangeEvent>();
				this.Result.Set(context, emptyList);

				var extension = context.GetExtension<Hosting.FolderWatcherExtension>();

				// Enter a no persist zone to pin this activity to memory since we are setting up a delegate to receive a callback
				var handle = this.noPersistHandle.Get(context);
				handle.Enter(context);

				Bookmark bookmark = context.CreateBookmark(this.receiveCompleteCallback, BookmarkOptions.MultipleResume);
				this.receiveCompleteBookmark.Set(context, bookmark);

				extension.RegisterReceiveNotification(bookmark, subscriptionHandle.Id);

				if (this.Body != null)
				{
					context.ScheduleAction(this.Body,
						new CompletionCallback(OnBodyCompleted),
						new FaultCallback(OnBodyFaulted));
				}
			}
		}

		protected override void Abort(NativeActivityAbortContext context)
		{
			UnregisterReceiveNotification(context);
			base.Abort(context);
		}

		protected override void Cancel(NativeActivityContext context)
		{
			UnregisterReceiveNotification(context);
			context.MarkCanceled();
		}

		private void OnBodyCompleted(NativeActivityContext context, ActivityInstance completedInstance)
		{
			if (!context.IsCancellationRequested)
				UnregisterReceiveNotification(context);
		}

		private void OnBodyFaulted(NativeActivityFaultContext context, Exception propagatedException, ActivityInstance propagatedFrom)
		{
			UnregisterReceiveNotification(context);
			throw propagatedException;
		}

		private void OnReceiveNotification(NativeActivityContext context, Bookmark bookmark, object data)
		{
			if (receiveCompleteBookmark.Get(context) != bookmark)
				return;

			if (data is FileChangeEvent && !context.IsCancellationRequested)
			{
				Track(context, (FileChangeEvent)data);

				// Store the result...
				var result = this.Result.Get(context);
				result.Add((FileChangeEvent)data);

				this.Result.Set(context, result);
			}
			else
			{
				UnregisterReceiveNotification(context);
				context.RemoveBookmark(bookmark);

				if (data is Exception)
					throw data as Exception;
			}
		}

		private void UnregisterReceiveNotification(ActivityContext context)
		{
			var subscriptionHandle = this.SubscriptionHandle.Get(context);
			Bookmark bookmark = this.receiveCompleteBookmark.Get(context);

			if (bookmark != null)
			{
				if (subscriptionHandle != null)
				{
					context.GetExtension<Hosting.FolderWatcherExtension>()
						.UnregisterReceiveNotification(bookmark, subscriptionHandle.Id);
				}
			}
			if (context is NativeActivityContext)
			{
				NativeActivityContext ctx = context as NativeActivityContext;
				if (bookmark != null) ctx.RemoveBookmark(bookmark);
				this.noPersistHandle.Get(ctx).Exit(ctx);
			}
			else if (context is NativeActivityFaultContext)
			{
				NativeActivityFaultContext ctx = context as NativeActivityFaultContext;
				if (bookmark != null) ctx.RemoveBookmark(bookmark);
				this.noPersistHandle.Get(ctx).Exit(ctx);
			}
		}

		private void Track(NativeActivityContext context, FileChangeEvent data)
		{
			var record = new CustomTrackingRecord("FileChangeEvent", System.Diagnostics.TraceLevel.Info)
			{
				Data = {
					{ "ChangeType", data.ChangeType },
					{ "ChangeTime", data.ChangeTime },
					{ "FullPath", data.FullPath }
				}
			};
			if (!String.IsNullOrEmpty(data.OldPath))
				record.Data.Add("PrevPath", data.OldPath);
			context.Track(record);
		}
	}
}
