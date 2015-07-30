using Microsoft.Activities;
using System;
using System.Activities;
using System.Text;
using System.Windows.Markup;

namespace Hasseware.Activities
{
	[ContentProperty("Format")]
	public sealed class FormatDynamicValue : CodeActivity<string>
	{
		[RequiredArgument]
		public InArgument<DynamicValue> Source { get; set; }

		[RequiredArgument]
		public InArgument<string> Format { get; set; }

		protected override void CacheMetadata(CodeActivityMetadata metadata)
		{
			metadata.AddArgument(new RuntimeArgument("Source", typeof(DynamicValue), ArgumentDirection.In, true));
			metadata.AddArgument(new RuntimeArgument("Format", typeof(string), ArgumentDirection.In, true));
		}

		protected override string Execute(CodeActivityContext context)
		{
			var source = this.Source.Get(context);
			return (source != null) ? ExecuteFormatting(this.Format.Get(context), source) : null;
		}

		private static string ExecuteFormatting(string format, DynamicValue source)
		{
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			StringBuilder result = new StringBuilder(format.Length * 2);

			using (var reader = new System.IO.StringReader(format))
			{
				StringBuilder expression = new StringBuilder();
				State state = State.OutsideExpression;
				int @char = -1;

				do
				{
					switch (state)
					{
						case State.OutsideExpression:
							@char = reader.Read();
							switch (@char)
							{
								case -1:
									state = State.End;
									break;
								case '{':
									state = State.OnOpenBracket;
									break;
								case '}':
									state = State.OnCloseBracket;
									break;
								default:
									result.Append((char)@char);
									break;
							}
							break;
						case State.OnOpenBracket:
							@char = reader.Read();
							switch (@char)
							{
								case -1:
									throw new FormatException();
								case '{':
									result.Append('{');
									state = State.OutsideExpression;
									break;
								default:
									expression.Append((char)@char);
									state = State.InsideExpression;
									break;
							}
							break;
						case State.InsideExpression:
							@char = reader.Read();
							switch (@char)
							{
								case -1:
									throw new FormatException();
								case '}':
									result.Append(source.EvaluateProperty(expression.ToString()).ToString());
									expression.Length = 0;
									state = State.OutsideExpression;
									break;
								default:
									expression.Append((char)@char);
									break;
							}
							break;
						case State.OnCloseBracket:
							@char = reader.Read();
							switch (@char)
							{
								case '}':
									result.Append('}');
									state = State.OutsideExpression;
									break;
								default:
									throw new FormatException();
							}
							break;
						default:
							throw new InvalidOperationException("Invalid state.");
					}
				}
				while (state != State.End);
			}
			return result.ToString();
		}

		private enum State
		{
			OutsideExpression, OnOpenBracket, InsideExpression, OnCloseBracket, End
		}
	}
}
