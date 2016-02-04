using Microsoft.Activities;
using System;
using System.Activities;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace Hasseware.Activities
{
	public sealed class XmlParseDynamicValue : CodeActivity<DynamicValue>
	{
		[DefaultValue(null), RequiredArgument]
		public InArgument<string> Source { get; set; }

		public XmlParseDynamicValue() { }

		public XmlParseDynamicValue(InArgument<string> source, OutArgument<DynamicValue> result)
		{
			this.Source = source;
			base.Result = result;
		}

		protected override void CacheMetadata(CodeActivityMetadata metadata)
		{
			metadata.AddArgument(new RuntimeArgument("Source", typeof(string), ArgumentDirection.In, true));
		}

		protected override DynamicValue Execute(CodeActivityContext context)
		{
			string encodedValue = this.Source.Get(context);
			return (String.IsNullOrEmpty(encodedValue)) ? new DynamicValue() :
				ParseElement(XDocument.Parse(encodedValue).Root);
		}

		private static DynamicValue ParseElement(XElement element)
		{
			var value = new DynamicValue();
			if (element.HasAttributes)
			{
				foreach (var attribute in element.Attributes())
				{
					if (!attribute.IsNamespaceDeclaration)
						value.Add(attribute.Name.LocalName, new DynamicValue(attribute.Value));
				}
			}
			foreach (var group in element.Elements().GroupBy(x => x.Name))
			{
				var childValue = new DynamicValue();
				foreach (XNode node in group)
				{
					if (node.GetType() != typeof(XElement))
					{
						if (node.GetType() == typeof(XText))
							childValue.Add(new DynamicValue(((XText)node).Value));
					}
					else childValue.Add(ParseElement((XElement)node));
				}
				value.Add(group.Key.LocalName, childValue);
			}
			return value;
		}
	}
}
