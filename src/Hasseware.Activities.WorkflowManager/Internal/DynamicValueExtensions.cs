using Microsoft.Activities;
using System.Activities;
using System.Activities.Expressions;

namespace Hasseware.Activities
{
	internal static class DynamicValueExtensions
	{
		public static DynamicValue EvaluateProperty(this DynamicValue value, string path)
		{
			var act = new GetDynamicValueProperty<DynamicValue>()
			{
				Source = new LambdaValue<DynamicValue>(c => value),
				PropertyName = path
			};
			var result = WorkflowInvoker.Invoke<DynamicValue>(act);
			return (result.Count == 0) ? new DynamicValue() : result;
		}
	}
}
