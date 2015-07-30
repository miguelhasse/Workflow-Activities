using System.Activities.Presentation.Metadata;

namespace Hasseware.Activities
{
	public sealed class DesignerMetadata : IRegisterMetadata
	{
		void IRegisterMetadata.Register()
		{
			var builder = new AttributeTableBuilder();
            Presentation.FileCopyDesigner.RegisterMetadata(builder);
			Presentation.FolderWatcherDesigner.RegisterMetadata(builder);
			Presentation.ReceiveFileChangesDesigner.RegisterMetadata(builder);
			MetadataStore.AddAttributeTable(builder.CreateTable());
		}
	}
}
