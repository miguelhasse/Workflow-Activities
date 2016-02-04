using System.Activities;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace Hasseware.Activities.Presentation
{
	// Interaction logic for CopyFileDesigner.xaml
	public partial class FileCopyDesigner
	{
        public FileCopyDesigner()
		{
			InitializeComponent();
		}

		protected override void OnModelItemChanged(object newItem)
		{
			var modelProgressProperty = this.ModelItem.Properties["OnProgress"];

			if (modelProgressProperty.Value == null)
			{
				modelProgressProperty.SetValue(new ActivityAction<int>
				{
					Argument = new DelegateInArgument<int> { Name = "progress" }
				});
			}
			base.OnModelItemChanged(newItem);
		}

		internal static void RegisterMetadata(AttributeTableBuilder builder)
		{
			builder.AddCustomAttributes(typeof(Statements.FileCopy),
                new DesignerAttribute(typeof(FileCopyDesigner)),
                new DescriptionAttribute(Properties.Resources.FileCopyDesigner_Description));
            //new ToolboxBitmapAttribute(typeof(FileCopyDesigner), "FileCopy.png")
		}
	}
}
