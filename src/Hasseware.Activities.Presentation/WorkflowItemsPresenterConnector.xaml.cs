using System;
using System.Activities.Presentation;
using System.Activities.Presentation.Hosting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace Hasseware.Activities.Presentation
{
	/// <summary>
	/// Interaction logic for WorkflowItemsPresenterConnector.xaml
	/// </summary>
	public partial class WorkflowItemsPresenterConnector : UserControl, IComponentConnector
	{
		public static readonly DependencyProperty AllowedItemTypeProperty;
		public static readonly DependencyProperty ContextProperty;

		public WorkflowItemsPresenterConnector()
		{
			InitializeComponent();
		}

		static WorkflowItemsPresenterConnector()
		{
			AllowedItemTypeProperty = DependencyProperty.Register("AllowedItemType", typeof(Type),
				typeof(WorkflowItemsPresenterConnector), new UIPropertyMetadata(typeof(object)));
			ContextProperty = DependencyProperty.Register("Context", typeof(EditingContext),
				typeof(WorkflowItemsPresenterConnector));
		}

		private void CheckAnimate(DragEventArgs e, string storyboardResourceName)
		{
			if (!e.Handled)
			{
				if (this.Context.Items.GetValue<ReadOnlyState>().IsReadOnly ||
					!DragDropHelper.AllowDrop(e.Data, this.Context, new Type[] { this.AllowedItemType }))
				{
					e.Effects = DragDropEffects.None;
					e.Handled = true;
				}
				else base.BeginStoryboard((Storyboard)base.Resources[storyboardResourceName]);
			}
		}

		protected override void OnDragEnter(DragEventArgs e)
		{
			this.CheckAnimate(e, "Expand");
			this.dropTarget.Visibility = Visibility.Visible;
		}

		protected override void OnDragLeave(DragEventArgs e)
		{
			this.CheckAnimate(e, "Collapse");
			this.dropTarget.Visibility = Visibility.Collapsed;
		}

		protected override void OnDrop(DragEventArgs e)
		{
			this.dropTarget.Visibility = Visibility.Collapsed;
			base.OnDrop(e);
		}

		public Type AllowedItemType
		{
			get { return (Type)base.GetValue(AllowedItemTypeProperty); }
			set { base.SetValue(AllowedItemTypeProperty, value); }
		}

		public EditingContext Context
		{
			get { return (EditingContext)base.GetValue(ContextProperty); }
			set { base.SetValue(ContextProperty, value); }
		}
	}

}
