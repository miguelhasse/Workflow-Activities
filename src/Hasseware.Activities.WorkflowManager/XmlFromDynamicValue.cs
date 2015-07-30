using Microsoft.Activities;
using System;
using System.Activities;
using System.Text;
using System.Xml.Linq;

namespace Hasseware.Activities
{
	public sealed class XmlFromDynamicValue : AsyncCodeActivity<string>
	{
		[RequiredArgument]
		public InArgument<DynamicValue> Source { get; set; }

		public InArgument<Encoding> Encoding { get; set; }

		public string RootName { get; set; }

		public XmlFromDynamicValue()
		{
		}

		public XmlFromDynamicValue(string rootName, InArgument<DynamicValue> source, InArgument<Encoding> encoding)
		{
			this.RootName = rootName;
			this.Encoding = encoding;
			this.Source = source;
		}

		protected override void CacheMetadata(CodeActivityMetadata metadata)
		{
			if (String.IsNullOrWhiteSpace(this.RootName))
				metadata.AddValidationError(Properties.Resources.RootNameNotFound);

			metadata.AddArgument(new RuntimeArgument("Source", typeof(DynamicValue), ArgumentDirection.In, true));
			metadata.AddArgument(new RuntimeArgument("Encoding", typeof(Encoding), ArgumentDirection.In, false));
		}

		protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
		{
			DynamicValue source = this.Source.Get(context);
			Encoding encoding = this.Encoding.Get(context) ?? System.Text.Encoding.UTF8;

			var settings = new System.Xml.XmlWriterSettings()
			{
				Encoding = encoding,
				OmitXmlDeclaration = false,
				ConformanceLevel = System.Xml.ConformanceLevel.Document,
				Indent = true
			};
			Func<string> action = () => InternalExecute(this.RootName, source, settings);
			context.UserState = action;

			return action.BeginInvoke(callback, state);
		}

		protected override string EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
		{
			return ((Func<string>)context.UserState).EndInvoke(result);
		}

		private static string InternalExecute(string rootName, DynamicValue value, System.Xml.XmlWriterSettings settings)
		{
			var xdoc = new XDocument(new XDeclaration("1.0", settings.Encoding.WebName, "yes"), new XElement(rootName,
				new XAttribute(XNamespace.Xmlns.GetName("xsd"), XNamespace.Get("http://www.w3.org/2001/XMLSchema").NamespaceName),
				new XAttribute(XNamespace.Xmlns.GetName("xsi"), XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance").NamespaceName)));
			BuildFromValue(xdoc.Root, value);

			using (var ms = new System.IO.MemoryStream())
			{
				using (var xw = System.Xml.XmlTextWriter.Create(ms, settings))
				{
					xdoc.Save(xw);
					xw.Flush();
				}
				using (var sr = new System.IO.StreamReader(ms))
				{
					ms.Seek(0, System.IO.SeekOrigin.Begin);
					return sr.ReadToEnd();
				}
			}
		}

		private static void BuildFromValue(XElement parent, DynamicValue value)
		{
			if (value.Keys.Count > 0)
			{
				foreach (var key in value.Keys)
				{
					var child = value[key];
					if (child.Keys.Count == 0)
					{
						if (child.Values.Count == 0)
						{
							var attribute = new XAttribute(key, child);
							parent.Add(attribute);
						}
						else foreach (var val in child.Values)
						{
							var element = new XElement(key);
							BuildFromValue(element, val);
							parent.Add(element);
						}
					}
					else
					{
						var element = new XElement(key);
						BuildFromValue(element, child);
						parent.Add(element);
					}
				}
			}
		}
	}
}
