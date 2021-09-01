namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Reflection.Emit;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	#endregion

	public class AttributeGenerator
	{
		#region Using Directives

		private CustomAttributeBuilder _builder;

		#endregion

		#region AttributeGenerator.ctor()

		public AttributeGenerator(CustomAttributeData data)
		{
			var props = new Dictionary<PropertyInfo, object>();
			var fields = new Dictionary<FieldInfo, object>();

			foreach (var arg in data.NamedArguments)
			{
				if (arg.MemberInfo is PropertyInfo info)
					props.Add(info, arg.TypedValue.Value);
				else
					fields.Add((FieldInfo)arg.MemberInfo, arg.TypedValue.Value);
			}

			var ctorArgs = data.ConstructorArguments.Select(typedArg =>
			{
				if (typedArg.Value is ICollection<CustomAttributeTypedArgument> arguments)
					return arguments.Select(arg => arg.Value).ToArray();
				else
					return typedArg.Value;
			});
			Init(data.Constructor, ctorArgs.ToArray(), props.Keys.ToArray(), props.Values.ToArray(), fields.Keys.ToArray(), fields.Values.ToArray());
		}

		public AttributeGenerator(Type type, params object[] ctorArgs)
			: this(type.GetMember<ConstructorInfo>(ctorArgs.To<Type[]>()), ctorArgs)
		{
		}

		public AttributeGenerator(ConstructorInfo ctor, params object[] ctorArgs)
		{
			Init(ctor, ctorArgs, null, null, null, null);
		}

		public AttributeGenerator(ConstructorInfo ctor, object[] ctorArgs, PropertyInfo[] props, object[] propValues, FieldInfo[] fields, object[] fieldValues)
		{
			Init(ctor, ctorArgs, props, propValues, fields, fieldValues);
		}

		#endregion

		#region Init

		private void Init(ConstructorInfo ctor, object[] ctorArgs, PropertyInfo[] props, object[] propValues, FieldInfo[] fields, object[] fieldValues)
		{
			if (props is null)
				props = Array.Empty<PropertyInfo>();

			if (propValues is null)
				propValues = Array.Empty<object>();

			if (fields is null)
				fields = Array.Empty<FieldInfo>();

			if (fieldValues is null)
				fieldValues = Array.Empty<object>();

			if (props.HasNullItem())
				throw new ArgumentException(nameof(props));

			if (fields.HasNullItem())
				throw new ArgumentException(nameof(fields));

			if (props.Length != propValues.Length)
				throw new ArgumentOutOfRangeException(nameof(propValues));

			if (fields.Length != fieldValues.Length)
				throw new ArgumentOutOfRangeException(nameof(fieldValues));

			Ctor = ctor ?? throw new ArgumentNullException(nameof(ctor));
			CtorArgs = ctorArgs;
			Props = props;
			PropValues = propValues;
			Fields = fields;
			FieldValues = fieldValues;

			_builder = new CustomAttributeBuilder(ctor, ctorArgs, props, propValues, fields, fieldValues);
		}

		#endregion

		public ConstructorInfo Ctor { get; private set; }
		public object[] CtorArgs { get; private set; }
		public PropertyInfo[] Props { get; private set; }
		public object[] PropValues { get; private set; }
		public FieldInfo[] Fields { get; private set; }
		public object[] FieldValues { get; private set; }

		#region SetCustomAttribute

		internal void SetCustomAttribute(object owner)
		{
			owner.SetValue("SetCustomAttribute", _builder);
		}

		#endregion

		public static IEnumerable<AttributeGenerator> CreateAttrs(MemberInfo member)
		{
			var attributes = new List<AttributeGenerator>();

			foreach (var data in CustomAttributeData.GetCustomAttributes(member))
			{
				if (!data.Constructor.DeclaringType.IsPublic)
					throw new ArgumentException(nameof(member));

				attributes.Add(new AttributeGenerator(data));
			}

			return attributes;
		}

		public static IEnumerable<AttributeGenerator> CreateAttrs(ParameterInfo parameter)
		{
			var attributes = new List<AttributeGenerator>();

			foreach (var data in CustomAttributeData.GetCustomAttributes(parameter))
				attributes.Add(new AttributeGenerator(data));

			return attributes;
		}
	}
}