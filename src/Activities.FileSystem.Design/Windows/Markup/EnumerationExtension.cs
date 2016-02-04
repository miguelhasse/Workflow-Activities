using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;

namespace Hasseware.Windows.Markup
{
	public class EnumerationExtension : MarkupExtension
	{
		private Type _enumType;

		public EnumerationExtension(Type enumType)
		{
			if (enumType == null)
				throw new ArgumentNullException("enumType");

			enumType = Nullable.GetUnderlyingType(enumType) ?? enumType;

			if (enumType.IsEnum == false)
				throw new ArgumentException("Type must be an Enum.");

			this._enumType = enumType;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			var enumValues = from object enumValue in Enum.GetValues(this._enumType)
				select new EnumerationMember
				{
					Value = enumValue,
					Description = GetDescription(enumValue)
				};
			return enumValues.ToArray();
		}

		private string GetDescription(object enumValue)
		{
			var descriptionAttribute = this._enumType.GetField(enumValue.ToString())
			  .GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

			return descriptionAttribute != null ? descriptionAttribute.Description : enumValue.ToString();
		}

		public class EnumerationMember
		{
			public string Description { get; set; }
			public object Value { get; set; }
		}
	}
}
