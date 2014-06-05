namespace Ecng.Reflection
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using MemberType = System.Tuple<string, System.Reflection.MemberTypes, MemberSignature>;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection.Emit;
#if !SILVERLIGHT
	using Ecng.Reflection.Path;
#endif

	public static class ReflectionHelper
	{
        public const AttributeTargets Members = AttributeTargets.Field | AttributeTargets.Property;
        public const AttributeTargets Types = AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface;

		public const BindingFlags AllStaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		public const BindingFlags AllInstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		public const BindingFlags AllMembers = AllStaticMembers | AllInstanceMembers;

		#region ProxyTypes

		private readonly static Dictionary<Type, Type> _proxyTypes = new Dictionary<Type, Type>();

		public static IDictionary<Type, Type> ProxyTypes
		{
			get { return _proxyTypes; }
		}

		#endregion

		#region GetParameterTypes

		public static Type[] GetParameterTypes(this MethodBase method)
		{
			return method.GetParameterTypes(false);
		}

		public static Type[] GetParameterTypes(this MethodBase method, bool removeRef)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			return method.GetParameters().Select(param =>
			{
				if (removeRef && IsOutput(param))
					return param.ParameterType.GetElementType();
				else
					return param.ParameterType;
			}).ToArray();
		}

		#endregion

		#region GetGenericType

		private static readonly SynchronizedDictionary<Tuple<Type, Type>, Type> _genericTypeCache = new SynchronizedDictionary<Tuple<Type, Type>, Type>();

		public static Type GetGenericType(this Type targetType, Type genericType)
		{
			return _genericTypeCache.SafeAdd(new Tuple<Type, Type>(targetType, genericType), key => key.Item1.GetGenericTypeInternal(key.Item2));
		}

		private static Type GetGenericTypeInternal(this Type targetType, Type genericType)
		{
			if (targetType == null)
				throw new ArgumentNullException("targetType");

			if (genericType == null)
				throw new ArgumentNullException("genericType");

			if (!genericType.IsGenericTypeDefinition)
				throw new ArgumentException("genericType");

			if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == genericType)
				return targetType;
			else
			{
				if (genericType.IsInterface)
				{
					var findedInterfaces = targetType.GetInterfaces()
						.Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == genericType)
						.ToList();

					if (findedInterfaces.Count > 1)
						throw new AmbiguousMatchException("Too many inerfaces was founded.");
					else if (findedInterfaces.Count == 1)
						return findedInterfaces[0];
					else
						return null;
				}
				else
				{
					return targetType.BaseType != null ? GetGenericType(targetType.BaseType, genericType) : null;
				}
			}
		}

		#endregion

		#region GetGenericTypeArg

		public static Type GetGenericTypeArg(this Type targetType, Type genericType, int index)
		{
			genericType = GetGenericType(targetType, genericType);

			if (genericType == null)
				throw new ArgumentException("targetType");
			else
				return genericType.GetGenericArguments()[index];
		}

		#endregion

		#region GetIndexer

		public static PropertyInfo GetIndexer(this Type type, params Type[] additionalTypes)
		{
			return GetMember<PropertyInfo>(type, "Item", ReflectionHelper.AllInstanceMembers, additionalTypes);
		}

		#endregion

		#region GetIndexers

		public static PropertyInfo[] GetIndexers(this Type type, params Type[] additionalTypes)
		{
			return GetMembers<PropertyInfo>(type, ReflectionHelper.AllInstanceMembers, true, "Item", additionalTypes);
		}

		#endregion

		#region CreateInstance

		public static T CreateInstance<T>(this Type type, object arg)
		{
			return (T)type.CreateInstance(arg);
		}

		public static object CreateInstance(this Type type)
		{
			return type.CreateInstance(null);
		}

		public static object CreateInstance(this Type type, object arg)
		{
			return GetMember<ConstructorInfo>(type, GetArgTypes(arg)).CreateInstance<object>(arg);
		}

		public static T CreateInstance<T>(this ConstructorInfo ctor, object arg)
		{
			return (T)FastInvoker.Create(ctor).Ctor(arg);
		}

		public static TInstance CreateInstance<TInstance>()
		{
			return CreateInstance<object[], TInstance>(null);
		}

		public static TInstance CreateInstance<TArg, TInstance>(TArg arg)
		{
			return CreateInstance<TArg, TInstance>(GetMember<ConstructorInfo>(typeof(TInstance), GetArgTypes(arg)), arg);
		}

		public static TInstance CreateInstance<TArg, TInstance>(ConstructorInfo ctor, TArg arg)
		{
			return FastInvoker<VoidType, TArg, TInstance>.Create(ctor).Ctor(arg);
		}

		#endregion

		#region GetArgTypes

		public static Type[] GetArgTypes<TArg>(TArg arg)
		{
			return arg.IsNull() ? Type.EmptyTypes : arg.To<Type[]>();
		}

		#endregion

		#region SetValue

		public static void SetValue<TInstance, TValue>(this TInstance instance, string memberName, TValue value)
		{
			instance.SetValue(memberName, ReflectionHelper.AllInstanceMembers, value);
		}

		public static void SetValue<TInstance, TValue>(this TInstance instance, string memberName, BindingFlags flags, TValue value)
		{
			if (instance.IsNull())
				throw new ArgumentNullException("instance");

			instance.SetValue(instance.GetType().GetMember<MemberInfo>(memberName, flags, GetArgTypes(value)), value);
		}

		public static void SetValue<TValue>(this Type type, string memberName, TValue value)
		{
			type.SetValue(memberName, ReflectionHelper.AllStaticMembers, value);
		}

		public static void SetValue<TValue>(this Type type, string memberName, BindingFlags flags, TValue value)
		{
			type.GetMember<MemberInfo>(memberName, flags, GetArgTypes(value)).SetValue(value);
		}

		public static void SetValue<TValue>(this MemberInfo member, TValue value)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			if (member is PropertyInfo)
				FastInvoker<VoidType, TValue, VoidType>.Create((PropertyInfo)member, false).SetValue(value);
			else if (member is FieldInfo)
				FastInvoker<VoidType, TValue, VoidType>.Create((FieldInfo)member, false).SetValue(value);
			else if (member is MethodInfo)
				FastInvoker<VoidType, TValue, VoidType>.Create((MethodInfo)member).VoidInvoke(value);
			else if (member is EventInfo)
				FastInvoker<VoidType, TValue, VoidType>.Create((EventInfo)member, false).VoidInvoke(value);
			else
				throw new ArgumentException("member");
		}

		public static TInstance SetValue<TInstance, TValue>(this TInstance instance, MemberInfo member, TValue value)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			if (member is PropertyInfo)
			{
				if (IsIndexer(member))
					FastInvoker<TInstance, TValue, VoidType>.Create((PropertyInfo)member, false).VoidInvoke(instance, value);
				else
					return FastInvoker<TInstance, TValue, VoidType>.Create((PropertyInfo)member, false).SetValue(instance, value);
			}
			else if (member is FieldInfo)
				return FastInvoker<TInstance, TValue, VoidType>.Create((FieldInfo)member, false).SetValue(instance, value);
			else if (member is MethodInfo)
				FastInvoker<TInstance, TValue, VoidType>.Create((MethodInfo)member).VoidInvoke(instance, value);
			else if (member is EventInfo)
				FastInvoker<TInstance, TValue, VoidType>.Create((EventInfo)member, false).VoidInvoke(instance, value);
			else
				throw new ArgumentException("member");

			return instance;
		}

		#endregion

		#region GetValue

		public static TValue GetValue<TInstance, TArg, TValue>(this TInstance instance, string memberName, TArg arg)
		{
			return instance.GetValue<TInstance, TArg, TValue>(memberName, ReflectionHelper.AllInstanceMembers, arg);
		}

		public static TValue GetValue<TInstance, TArg, TValue>(this TInstance instance, string memberName, BindingFlags flags, TArg arg)
		{
			if (instance.IsNull())
				throw new ArgumentNullException("instance");

			return instance.GetValue<TInstance, TArg, TValue>(GetMember<MemberInfo>(instance.GetType(), memberName, flags, GetArgTypes(arg)), arg);
		}

		public static TValue GetValue<TArg, TValue>(this Type type, string memberName, TArg arg)
		{
			return type.GetValue<TArg, TValue>(memberName, ReflectionHelper.AllStaticMembers, arg);
		}

		public static TValue GetValue<TArg, TValue>(this Type type, string memberName, BindingFlags flags, TArg arg)
		{
			return GetMember<MemberInfo>(type, memberName, flags, GetArgTypes(arg)).GetValue<TArg, TValue>(arg);
		}

		public static TValue GetValue<TArg, TValue>(this MemberInfo member, TArg arg)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			TValue value;

			if (member is PropertyInfo)
				value = FastInvoker<VoidType, VoidType, TValue>.Create((PropertyInfo)member, true).GetValue();
			else if (member is FieldInfo)
				value = FastInvoker<VoidType, VoidType, TValue>.Create((FieldInfo)member, true).GetValue();
			else if (member is MethodInfo)
				value = FastInvoker<VoidType, TArg, TValue>.Create((MethodInfo)member).ReturnInvoke(arg);
			else if (member is EventInfo)
			{
				FastInvoker<VoidType, TArg, VoidType>.Create((EventInfo)member, true).VoidInvoke(arg);
				value = default(TValue);
			}
			else
				throw new ArgumentException("member");

			return value;
		}

		public static TValue GetValue<TInstance, TArg, TValue>(this TInstance instance, MemberInfo member, TArg arg)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			TValue value;

			if (member is PropertyInfo)
			{
				value = IsIndexer(member) ? FastInvoker<TInstance, TArg, TValue>.Create((PropertyInfo)member, true).ReturnInvoke(instance, arg) : FastInvoker<TInstance, VoidType, TValue>.Create((PropertyInfo)member, true).GetValue(instance);
			}
			else if (member is FieldInfo)
				value = FastInvoker<TInstance, VoidType, TValue>.Create((FieldInfo)member, true).GetValue(instance);
			else if (member is MethodInfo)
				value = FastInvoker<TInstance, TArg, TValue>.Create((MethodInfo)member).ReturnInvoke(instance, arg);
			else if (member is EventInfo)
			{
				FastInvoker<TInstance, TArg, VoidType>.Create((EventInfo)member, true).VoidInvoke(instance, arg);
				value = default(TValue);
			}
			else
				throw new ArgumentException("member");

			return value;
		}

		#endregion

		#region GetMember

		public static T GetMember<T>(this Type type, params Type[] additionalTypes)
			where T : ConstructorInfo
		{
			return type.GetMember<T>(".ctor", AllInstanceMembers, additionalTypes);
		}

		public static T GetMember<T>(this Type type, string memberName, params Type[] additionalTypes)
			where T : MemberInfo
		{
			return type.GetMember<T>(memberName, AllMembers, additionalTypes);
		}

		public static T GetMember<T>(this Type type, string memberName, BindingFlags flags, params Type[] additionalTypes)
			where T : MemberInfo
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (memberName.IsEmpty())
				throw new ArgumentNullException("memberName");

			var members = type.GetMembers<T>(flags, true, memberName, additionalTypes);

			if (members.Length > 1)
				members = FilterMembers(members, additionalTypes).ToArray();

			if (members.Length != 1)
				throw new ArgumentException("Type '{0}' has '{1}' members with name '{2}'".Put(type, members.Length, memberName));

			return members[0];
		}

		#endregion

		#region GetMembers

		//public static T[] GetMembers<T>(this Type type)
		//    where T : MemberInfo
		//{
		//    return type.GetMembers<T>(Type.EmptyTypes);
		//}

		public static T[] GetMembers<T>(this Type type, params Type[] additionalTypes)
			where T : MemberInfo
		{
			return type.GetMembers<T>(ReflectionHelper.AllMembers, additionalTypes);
		}

		public static T[] GetMembers<T>(this Type type, BindingFlags flags, params Type[] additionalTypes)
			where T : MemberInfo
		{
			return type.GetMembers<T>(flags, true, additionalTypes);
		}

		public static T[] GetMembers<T>(this Type type, BindingFlags flags, bool inheritance, params Type[] additionalTypes)
			where T : MemberInfo
		{
			return type.GetMembers<T>(flags, inheritance, null, additionalTypes);
		}

		public static T[] GetMembers<T>(this Type type, BindingFlags flags, bool inheritance, string memberName, params Type[] additionalTypes)
			where T : MemberInfo
		{
			if (type == null)
				throw new ArgumentNullException("type");

			Type proxyType;
			if (_proxyTypes.TryGetValue(type, out proxyType))
				type = proxyType;

#if !SILVERLIGHT
			if (typeof(T) != typeof(MemberProxy))
			{
#endif
				var members = type.GetMembers<T>(memberName, flags, inheritance);

				if (!members.IsEmpty() && additionalTypes.Length > 0)
					members = FilterMembers(members, additionalTypes);

				return members.ToArray();
#if !SILVERLIGHT
			}
			else
				return new [] { MemberProxy.Create(type, memberName) as T };
#endif
		}

		private static IEnumerable<T> GetMembers<T>(this Type type, string memberName, BindingFlags flags, bool inheritance)
			where T : MemberInfo
		{
			var members = new Dictionary<MemberType, ICollection<T>>();

			if (inheritance)
			{
				foreach (Type item in type.GetInterfaces().Concat(new[] { type } ))
				{
					var allMembers = memberName.IsEmpty() ? item.GetMembers(flags) : item.GetMember(memberName, flags);

					foreach (var member in allMembers)
					{
						if (member is T && !(member is Type))
							members.AddMember(member);
					}
				}
			}
			else
			{
				var allMembers = memberName.IsEmpty() ? type.GetMembers(flags) : type.GetMember(memberName, flags);

				foreach (var member in allMembers)
				{
					if (member is T && !(member is Type) && member.ReflectedType == type)
						members.AddMember(member);
				}
			}

			if (type.IsValueType && (typeof(T) == typeof(ConstructorInfo) || memberName == ".ctor"))
			{
#if !SILVERLIGHT
				MemberInfo member = new DefaultConstructorInfo(type);
				members.AddMember(member);
#else
				throw new NotSupportedException();
#endif
			}

			if (inheritance)
			{
				if (type.BaseType != null)
				{
					foreach (var member in type.BaseType.GetMembers<T>(memberName, flags, true))
						members.AddMember(member);
				}

				foreach (var pair in members.Where(arg => arg.Value.Count > 1))
				{
					var sortedMembers = pair.Value.OrderBy(((x, y) =>
					{
						int result = x.ReflectedType.Compare(y.ReflectedType);

						if (result == 0)
							result = x.DeclaringType.Compare(y.DeclaringType);

						return result;
					})).ToArray();

					for (int i = 1; i < sortedMembers.Length; i++)
						members[pair.Key].Remove(sortedMembers[i]);
						//members.Remove(pair.Key, sortedMembers[i]);

					if (members[pair.Key].IsEmpty())
						members.Remove(pair.Key);
				}
			}

			var retVal = new List<T>();

			foreach (var collection in members.Values)
				retVal.AddRange(collection);

			return retVal;
		}

		private static void AddMember<T>(this Dictionary<MemberType, ICollection<T>> members, MemberInfo member)
		{
			if (members == null)
				throw new ArgumentNullException("members");

			if (member == null)
				throw new ArgumentNullException("member");

			members.SafeAdd(new MemberType(member.Name, member.MemberType, new MemberSignature(member)), delegate
			{
				return new List<T>();
			}).Add(member.To<T>());
		}

		#endregion

		#region FilterMembers

		public static IEnumerable<T> FilterMembers<T>(this IEnumerable<T> members, params Type[] additionalTypes)
			where T : MemberInfo
		{
			var ms = FilterMembers(members, false, additionalTypes);
			return ms.IsEmpty() ? FilterMembers(members, true, additionalTypes) : ms;
		}

		public static IEnumerable<T> FilterMembers<T>(this IEnumerable<T> members, bool useInheritance, params Type[] additionalTypes)
			where T : MemberInfo
		{
			if (members == null)
				throw new ArgumentNullException("members");

			if (additionalTypes == null)
				throw new ArgumentNullException("additionalTypes");

			return members.Where(arg =>
			{
				if (IsIndexer(arg) && additionalTypes.Length > 0)
				{
					//return GetIndexerType(arg as PropertyInfo).IsAssignableFrom(additionalTypes[0]);
					//return GetIndexerType(arg as PropertyInfo) == additionalTypes[0];

					if (GetIndexerType(arg as PropertyInfo).Compare(additionalTypes[0], useInheritance))
					{
						if (additionalTypes.Length == 2)
							return GetMemberType(arg).Compare(additionalTypes[1], useInheritance);

						return true;
					}
				}
				else if (additionalTypes.Length == 1 && (arg is FieldInfo || arg is PropertyInfo || arg is EventInfo))
				{
					//return GetMemberType(arg).IsAssignableFrom(additionalTypes[0]);
					//return GetMemberType(arg) == additionalTypes[0];
					return GetMemberType(arg).Compare(additionalTypes[0], useInheritance);
				}
				else if (arg is MethodBase)
				{
					return (arg as MethodBase).GetParameterTypes(true).SequenceEqual(additionalTypes,
					(paramType, additionalType) =>
					{
						if (additionalType == typeof(void))
							return true;
						else
							//return paramType.IsAssignableFrom(additionalType);
							//return paramType == additionalType;
							return paramType.Compare(additionalType, useInheritance);
					});
				}
				
				return false;
			});
		}

		#endregion

		#region IsAbstract

		private static readonly Dictionary<MemberInfo, bool> _isAbstractCache = new Dictionary<MemberInfo, bool>();

		public static bool IsAbstract(this MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			return _isAbstractCache.SafeAdd(member, delegate
			{
				if (member is MethodBase)
					return ((MethodBase)member).IsAbstract;
				else if (member is Type)
					return ((Type)member).IsAbstract;
				else if (member is PropertyInfo)
				{
					var prop = (PropertyInfo)member;
					return (prop.CanRead && prop.GetGetMethod(true).IsAbstract) || (prop.CanWrite && prop.GetSetMethod(true).IsAbstract);
				}
				else if (member is EventInfo)
				{
					var evt = (EventInfo)member;
					return evt.GetAddMethod(true).IsAbstract || evt.GetRemoveMethod(true).IsAbstract;
				}
				else
					return false;	
			});
		}

		#endregion

		#region IsVirtual

		private static readonly Dictionary<MemberInfo, bool> _isVirtualCache = new Dictionary<MemberInfo, bool>();

		public static bool IsVirtual(this MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			return _isVirtualCache.SafeAdd(member, delegate
			{
				if (member is MethodBase)
					return ((MethodBase)member).IsVirtual;
				//else if (member is Type)
				//	return ((Type)member).IsVirtual;
				else if (member is PropertyInfo)
				{
					var prop = (PropertyInfo)member;
					return (prop.CanRead && prop.GetGetMethod(true).IsVirtual) || (prop.CanWrite && prop.GetSetMethod(true).IsVirtual);
				}
				else if (member is EventInfo)
				{
					var evt = (EventInfo)member;
					return evt.GetAddMethod(true).IsVirtual || evt.GetRemoveMethod(true).IsVirtual;
				}
				else
					return false;
			});
		}

		#endregion

		#region IsOverloadable

		public static bool IsOverloadable(this MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			return member is ConstructorInfo || member.IsAbstract() || member.IsVirtual();
		}

		#endregion

		#region IsIndexer

		public static bool IsIndexer(this MemberInfo member)
		{
			if (member is PropertyInfo)
				return ((PropertyInfo)member).IsIndexer();
			else
				return false;
		}

		public static bool IsIndexer(this PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			return (property.GetIndexParameters().Length > 0);
		}

		#endregion

		#region GetIndexerType

		public static Type GetIndexerType(this PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			var accessor = property.GetGetMethod(true) ?? property.GetSetMethod(true);

			if (accessor == null)
				throw new ArgumentException("property");

			return accessor.GetParameters()[0].ParameterType;
		}

		#endregion

		#region MemberIs

		public static bool MemberIs(this MemberInfo member, params MemberTypes[] types)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			return types.Any(type => member.MemberType == type);
		}

		#endregion

		#region IsOutput

		public static bool IsOutput(this ParameterInfo param)
		{
			if (param == null)
				throw new ArgumentNullException("param");

			return (param.IsOut || param.ParameterType.IsByRef);
		}

		#endregion

		#region GetMemberType

		public static Type GetMemberType(this MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			if (member is PropertyInfo)
				return ((PropertyInfo)member).PropertyType;
			else if (member is FieldInfo)
				return ((FieldInfo)member).FieldType;
			else if (member is MethodInfo)
				return ((MethodInfo)member).ReturnType;
			else if (member is EventInfo)
				return ((EventInfo)member).EventHandlerType;
			else if (member is ConstructorInfo)
				return member.ReflectedType;
			else
				throw new ArgumentException("member");
		}

		#endregion

		#region IsCollection

		private static readonly Dictionary<Type, bool> _isCollectionCache = new Dictionary<Type, bool>();

		public static bool IsCollection(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return _isCollectionCache.SafeAdd(type, delegate
			{
				return typeof(ICollection).IsAssignableFrom(type)
							|| type.GetGenericType(typeof(ICollection<>)) != null
							|| typeof(IEnumerable).IsAssignableFrom(type)
							|| type.GetGenericType(typeof(IEnumerable<>)) != null;
			});

			
		}

		#endregion

		#region IsStatic

		private static readonly Dictionary<MemberInfo, bool> _isStaticCache = new Dictionary<MemberInfo, bool>();

		public static bool IsStatic(this MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			return _isStaticCache.SafeAdd(member, delegate
			{
				if (member is MethodBase)
					return ((MethodBase)member).IsStatic;
				else if (member is PropertyInfo)
				{
					var prop = (PropertyInfo)member;

					if (prop.CanRead)
						return IsStatic(prop.GetGetMethod(true));
					else if (prop.CanWrite)
						return IsStatic(prop.GetSetMethod(true));
					else
						throw new ArgumentException("member");
				}
				else if (member is FieldInfo)
					return ((FieldInfo)member).IsStatic;
				else if (member is EventInfo)
				{
					var evt = (EventInfo)member;
					return IsStatic(evt.GetAddMethod(true));
				}
				else if (member is Type)
				{
					var type = (Type)member;
					return type.IsAbstract && type.IsSealed;
				}
				else
					throw new ArgumentException("member");
			});
		}

		#endregion

		#region GetItemType

		private static readonly Dictionary<Type, Type> _getItemTypeCache = new Dictionary<Type, Type>();

		public static Type GetItemType(this Type collectionType)
		{
			if (collectionType == null)
				throw new ArgumentNullException("collectionType");

			return _getItemTypeCache.SafeAdd(collectionType, delegate
			{
				var interfaceType = collectionType.GetGenericType(typeof(ICollection<>)) ?? collectionType.GetGenericType(typeof(IEnumerable<>));

				if (interfaceType != null)
					return interfaceType.GetGenericArguments()[0];
				else
					throw new InvalidOperationException("Type '{0}' isn't collection.".Put(collectionType));
			});
		}

		#endregion

		#region MakePropertyName

		public static string MakePropertyName(this string accessorName)
		{
			if (accessorName.IsEmpty())
				throw new ArgumentNullException("accessorName");

			return accessorName
							.Replace("get_", string.Empty)
							.Replace("set_", string.Empty)
							.Replace("add_", string.Empty)
							.Replace("remove_", string.Empty);
		}

		#endregion

		#region GetAccessorOwner

		private static readonly Dictionary<MethodInfo, MemberInfo> _getAccessorOwnerCache = new Dictionary<MethodInfo, MemberInfo>();

		public static MemberInfo GetAccessorOwner(this MethodInfo method)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			return _getAccessorOwnerCache.SafeAdd(method, delegate
			{
				var flags = method.IsStatic ? AllStaticMembers : AllInstanceMembers;

				if (method.Name.Contains("get_") || method.Name.Contains("set_"))
				{
					var name = MakePropertyName(method.Name);

					return GetMembers<PropertyInfo>(method.ReflectedType, flags, true, name)
						.FirstOrDefault(property => property.GetGetMethod(true) == method || property.GetSetMethod(true) == method);
				}
				else if (method.Name.Contains("add_") || method.Name.Contains("remove_"))
				{
					var name = MakePropertyName(method.Name);

					return GetMembers<EventInfo>(method.ReflectedType, flags, true, name)
						.FirstOrDefault(@event => @event.GetAddMethod(true) == method || @event.GetRemoveMethod(true) == method);
				}

				return null;
			});
		}

		#endregion

		#region GetGenericArgs

		public static IEnumerable<GenericArg> GetGenericArgs(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (!type.IsGenericTypeDefinition)
				throw new ArgumentException("type");

			return type.GetGenericArguments().GetGenericArgs();
		}

		public static IEnumerable<GenericArg> GetGenericArgs(this MethodInfo method)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			if (!method.IsGenericMethodDefinition)
				throw new ArgumentException("method");

			return method.GetGenericArguments().GetGenericArgs();
		}

		private static IEnumerable<GenericArg> GetGenericArgs(this IEnumerable<Type> genericParams)
		{
			if (genericParams == null)
				throw new ArgumentNullException("genericParams");

			var genericArgs = new List<GenericArg>();

			foreach (var genericParam in genericParams)
			{
				var constraints = genericParam.GetGenericParameterConstraints()
					.Select(constraintBaseType => new Constraint(constraintBaseType))
					.ToList();

				if (genericParam.GenericParameterAttributes != GenericParameterAttributes.None)
					constraints.Add(new Constraint(genericParam.GenericParameterAttributes));

				genericArgs.Add(new GenericArg(genericParam, genericParam.Name, constraints));
			}

			return genericArgs;
		}

		#endregion

		public static MethodInfo Make(this MethodInfo method, params Type[] types)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			return method.MakeGenericMethod(types);
		}

		public static bool IsRuntimeType(this Type type)
		{
			return type.BaseType == typeof(Type);
		}

#if !SILVERLIGHT
		public static bool IsAssembly(this string dllName)
		{
			return dllName.VerifyAssembly() != null;
		}

		public static Assembly VerifyAssembly(this string dllName)
		{
			try
			{
				return Assembly.ReflectionOnlyLoadFrom(dllName);
			}
			catch (BadImageFormatException)
			{
				return null;
			}
		}
#endif
	}
}