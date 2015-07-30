using System;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.PropertyEditing;
using System.ComponentModel;
using Microsoft.Activities.Design.PropertyEditors;

namespace Hasseware.Activities.Presentation
{
    public sealed class DesignerMetadata : IRegisterMetadata
    {
        void IRegisterMetadata.Register()
        {
            var builder = new AttributeTableBuilder();

            // Register metadata for PublishNotification activity
            /*
            Type type = typeof(Messaging.PublishNotification);
            builder.AddCustomAttributes(type, "Properties", new Attribute[] { BrowsableAttribute.Yes,
				PropertyValueEditor.CreateEditorAttribute(typeof(ArgumentDictionaryPropertyEditor)) });
            builder.AddCustomAttributes(type, "Content", new Attribute[] { BrowsableAttribute.Yes,
				PropertyValueEditor.CreateEditorAttribute(typeof(ArgumentDictionaryPropertyEditor)) });
            builder.AddCustomAttributes(type, "Metadata", new Attribute[] { BrowsableAttribute.Yes,
				PropertyValueEditor.CreateEditorAttribute(typeof(ArgumentDictionaryPropertyEditor)) });
            */
            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
