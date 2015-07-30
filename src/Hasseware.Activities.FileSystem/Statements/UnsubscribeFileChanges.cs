using System.Activities;

namespace Hasseware.Activities.Statements
{
	public sealed class UnsubscribeFileChanges : CodeActivity
	{
		[RequiredArgument]
		public InArgument<FileChangeSubscriptionHandle> SubscriptionHandle { get; set; }

		#region Constructors

		public UnsubscribeFileChanges()
		{
		}

		public UnsubscribeFileChanges(InArgument<FileChangeSubscriptionHandle> subscriptionHandle) : this()
		{
			this.SubscriptionHandle = subscriptionHandle;
		}

		#endregion

		protected override void CacheMetadata(CodeActivityMetadata metadata)
		{
			// Tell the runtime that we need this extension
			metadata.RequireExtension(typeof(Hosting.FolderWatcherExtension));
			metadata.AddArgument(new RuntimeArgument("SubscriptionHandle", typeof(FileChangeSubscriptionHandle), ArgumentDirection.In, true));
		}

		protected override void Execute(CodeActivityContext context)
		{
			var subscriptionHandle = this.SubscriptionHandle.Get(context);
			if (subscriptionHandle != null) subscriptionHandle.ReleaseSubscription(context.GetExtension<Hosting.FolderWatcherExtension>());
		}
	}
}
