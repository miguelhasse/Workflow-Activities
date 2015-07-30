using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace Hasseware.Activities.Presentation
{
	// Interaction logic for FolderWatcherDesigner.xaml
	public partial class FolderWatcherDesigner
	{
		public FolderWatcherDesigner()
		{
			InitializeComponent();
		}
		internal static void RegisterMetadata(AttributeTableBuilder builder)
		{
			builder.AddCustomAttributes(typeof(Statements.FolderWatcher),
				new DesignerAttribute(typeof(FolderWatcherDesigner)),
				new DescriptionAttribute(Properties.Resources.FolderWatcherDesigner_Description));
			//new ToolboxBitmapAttribute(typeof(FolderWatcherDesigner), "FolderWatcher.png")

			builder.AddCustomAttributes(typeof(Statements.SubscribeFileChanges),
				new DesignerAttribute(typeof(FolderWatcherDesigner)),
				new DescriptionAttribute(Properties.Resources.SubscribeFileChangesDesigner_Description));
		}
	}
}
