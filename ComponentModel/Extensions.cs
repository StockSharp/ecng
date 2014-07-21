namespace Ecng.ComponentModel
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;

	public static class Extensions
	{
		public static string GetDisplayName(this Type type, string defaultValue = null)
		{
			var attr = type.GetAttribute<DisplayNameAttribute>();
			return attr == null ? defaultValue ?? type.Name : attr.DisplayName;
		}

		public static string GetDescription(this Type type, string defaultValue = null)
		{
			var attr = type.GetAttribute<DescriptionAttribute>();
			return attr == null ? defaultValue : attr.Description;
		}

		public static string GetDisplayName(this object field)
		{
			if (field == null)
				throw new ArgumentNullException("field");

			if (!(field is Enum))
				throw new ArgumentException("field");

			var fieldInfo = field.GetType().GetField(field.ToString());

			if (fieldInfo == null)
				throw new ArgumentException(field.ToString(), "field");

			var name = fieldInfo.Name;

			var attr = fieldInfo.GetAttribute<EnumDisplayNameAttribute>();
			if (attr != null/* && !attr.DisplayName.IsEmpty()*/)
				name = attr.DisplayName;

			return name;
		}
	}
}