namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Drawing;

	public static class CompareHelper
	{
		public static int Compare(this IPAddress first, IPAddress second)
		{
			return first.To<long>().CompareTo(second.To<long>());
		}

		public static bool Compare(this Type first, Type second, bool useInheritance)
		{
			if (first == null)
				throw new ArgumentNullException("first");

			if (second == null)
				throw new ArgumentNullException("second");

			if (useInheritance)
				return first.IsAssignableFrom(second);
			else
				return first == second;
		}

		public static int Compare(this Type first, Type second)
		{
			if (first == null)
				throw new ArgumentNullException("first");

			if (second == null)
				throw new ArgumentNullException("second");

			if (first == second)
				return 0;
			else if (first.IsAssignableFrom(second))
				return 1;
			else
				return -1;
		}

		public static bool Compare(this Color first, Color second)
		{
			return first.ToArgb() == second.ToArgb();
		}

		public static int Compare(this object value1, object value2)
		{
			if (value1 == null && value2 == null)
				return 0;

			if (value1 == null)
				return -1;

			if (value2 == null)
				return 1;

			if (value1.GetType() != value2.GetType())
				throw new ArgumentException("The values must be a same types.", "value2");

			var compare1 = value1 as IComparable;

			if (compare1 != null)
				return compare1.CompareTo(value2);

			throw new ArgumentException("The values must be IComparable.");
		}

		public static bool IsDefault<T>(this T value)
		{
			return EqualityComparer<T>.Default.Equals(value, default(T));
		}

		public static bool IsRuntimeDefault<T>(this T value)
		{
			return EqualityComparer<T>.Default.Equals(value, (T)value.GetType().GetDefaultValue());
		}

		public static int Compare(this Version first, Version second)
		{
			if (first == null)
				return -1;

			if (second == null)
				return 1;

			var firstBuild = first.Build != -1 ? first.Build : 0;
			var firstRevision = first.Revision != -1 ? first.Revision : 0;

			var secondBuild = second.Build != -1 ? second.Build : 0;
			var secondRevision = second.Revision != -1 ? second.Revision : 0;

			if (first.Major != second.Major)
				return first.Major > second.Major ? 1 : -1;
			
			if (first.Minor != second.Minor)
				return first.Minor > second.Minor ? 1 : -1;

			if (firstBuild != secondBuild)
				return firstBuild > secondBuild ? 1 : -1;

			if (firstRevision == secondRevision)
				return 0;

			return firstRevision > secondRevision ? 1 : -1;
		}
	}
}