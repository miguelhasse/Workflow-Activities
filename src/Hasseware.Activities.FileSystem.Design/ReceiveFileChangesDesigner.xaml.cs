using System.Activities;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace Hasseware.Activities.Presentation
{
	// Interaction logic for ReceiveFileChangesDesigner.xaml
	public partial class ReceiveFileChangesDesigner
	{
		public ReceiveFileChangesDesigner()
		{
			InitializeComponent();
		}

		protected override void OnModelItemChanged(object newItem)
		{
			var modelProgressProperty = this.ModelItem.Properties["Body"];

			if (modelProgressProperty.Value == null)
			{
				modelProgressProperty.SetValue(new ActivityAction());
			}
			base.OnModelItemChanged(newItem);
		}

		internal static void RegisterMetadata(AttributeTableBuilder builder)
		{
			builder.AddCustomAttributes(typeof(Statements.ReceiveFileChanges),
				new DesignerAttribute(typeof(ReceiveFileChangesDesigner)),
				new DescriptionAttribute(Properties.Resources.ReceiveFileChangesDesigner_Description));
			//new ToolboxBitmapAttribute(typeof(ReceiveFileChangesDesigner), "ReceiveFileChanges.png")
		}
	}
}
