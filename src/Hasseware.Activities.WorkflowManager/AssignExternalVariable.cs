using Microsoft.Activities;
using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;

namespace Hasseware.Activities
{
	public sealed class AssignExternalVariable<T> : NativeActivity
	{
		[DefaultValue(null)]
		[RequiredArgument]
		public InArgument<T> Value { get; set; }

		[DefaultValue(null)]
		[RequiredArgument]
		public string Name { get; set; }

		private Activity implementationChild;

		protected override void CacheMetadata(NativeActivityMetadata metadata)
		{
			if (String.IsNullOrWhiteSpace(this.Name))
				metadata.AddValidationError(Properties.Resources.ExternalVariableNameRequired);
			else
			{
				this.implementationChild = new Assign<T>
				{
					To = new OutArgument<T>(new ExternalVariableReference<T>() { Name = this.Name }),
					Value = new System.Activities.Expressions.LambdaValue<T>(ctx => this.Value.Get(ctx))
				};
				metadata.AddImplementationChild(implementationChild);
			}
			metadata.AddArgument(new RuntimeArgument("Value", typeof(T), ArgumentDirection.In, true));
		}

		protected override void Execute(NativeActivityContext context)
		{
			context.ScheduleActivity(this.implementationChild);
		}
	}
}
