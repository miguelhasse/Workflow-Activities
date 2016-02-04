using Hasseware.Activities.Hosting;
using System;
using System.Activities;
using System.Runtime.Serialization;

namespace Hasseware.Activities.Statements
{
	[DataContract]
	public class FileChangeSubscriptionHandle : Handle
	{
		[DataMember(EmitDefaultValue = false)]
		internal string Id { get; private set; }

		internal bool IsInitialized
		{
			get { return (this.Id != null); }
		}

		internal void Initialize(FolderWatcherExtension extension, Bookmark bookmark, string folder, string pattern, bool subfolders, string subscriberDisplayName)
		{
			if (this.IsInitialized)
			{
				throw new InvalidOperationException(String.Format(
					Properties.Resources.SubscriptionHandleAlreadyInitialized, subscriberDisplayName));
			}
			this.Id = extension.Subscribe(bookmark, folder, pattern, subfolders);
		}

		protected override void OnUninitialize(HandleInitializationContext context)
		{
			FolderWatcherExtension extension = context.GetExtension<FolderWatcherExtension>();
			this.ReleaseSubscription(extension);
			base.OnUninitialize(context);
		}

		internal void ReleaseSubscription(FolderWatcherExtension extension)
		{
			if (this.IsInitialized)
			{
				extension.Unsubscribe(this.Id);
				this.Id = null;
			}
		}
	}
}

