using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;

namespace Hasseware.Activities
{
	internal static class ConstraintHelper
	{
		// Create a validation to verify that there are no Persist activites inside of a NoPersistScope
		public static Constraint VerifiyNoChildPersistActivity<T>() where T : NativeActivity
		{
			DelegateInArgument<T> element = new DelegateInArgument<T>();
			DelegateInArgument<ValidationContext> context = new DelegateInArgument<ValidationContext>();
			DelegateInArgument<Activity> child = new DelegateInArgument<Activity>();
			Variable<bool> result = new Variable<bool>();

			string validationMessage = string.Format("{0} activity can't contain a Persist activity", typeof(T).Name);

			return new Constraint<T>
			{
				Body = new ActivityAction<T, ValidationContext>
				{
					Argument1 = element,
					Argument2 = context,
					Handler = new Sequence
					{
						Variables =
                        {
                            result 
                        },
						Activities =
                        {
                            new ForEach<Activity>
                            {                                
                                Values = new GetChildSubtree
                                {
                                    ValidationContext = context                                    
                                },
                                Body = new ActivityAction<Activity>
                                {                                    
                                    Argument = child, 
                                    Handler = new If()
                                    {                                          
                                        Condition = new InArgument<bool>((env) => object.Equals(child.Get(env).GetType(),typeof(Persist))),
                                        Then = new Assign<bool> { Value = true,To = result }
                                    }
                                }                                
                            },
                            new AssertValidation
                            {
                                Assertion = new InArgument<bool>(env => !result.Get(env)),
                                Message = new InArgument<string> (validationMessage),
                                PropertyName = new InArgument<string>((env) => element.Get(env).DisplayName)
                            }
                        }
					}
				}
			};
		}
	}
}
