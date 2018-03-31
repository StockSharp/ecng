﻿namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Security;
	using System.Security.Cryptography;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;

#if !SILVERLIGHT
	using System.Collections;
	using System.Reflection;

	using SmartFormat;
	using SmartFormat.Core.Extensions;
#endif

	public static class StringHelper
	{
		public static bool IsEmpty(this string str)
		{
			return string.IsNullOrEmpty(str);
		}

		public static string IsEmpty(this string str, string defaultValue)
		{
			return str.IsEmpty() ? defaultValue : str;
		}

		public static bool IsEmptyOrWhiteSpace(this string str)
		{
			return string.IsNullOrWhiteSpace(str);
		}

		public static string Put(this string str, params object[] args)
		{
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			return args.Length == 0 ? str : string.Format(str, args);
		}

#if !SILVERLIGHT
		private class DictionarySourceEx : ISource
		{
			private readonly SyncObject _sync = new SyncObject();
			private readonly Dictionary<Type, Type> _genericTypes = new Dictionary<Type, Type>();
			private readonly Dictionary<string, object> _keys = new Dictionary<string, object>();

			bool ISource.TryEvaluateSelector(ISelectorInfo selectorInfo)
			{
				var dictionary = selectorInfo.CurrentValue as IDictionary;

				if (dictionary == null)
					return false;

				var dictType = dictionary.GetType();

				Type type;

				lock (_sync)
				{
					if (!_genericTypes.TryGetValue(dictType, out type))
					{
						type = dictType.GetGenericType(typeof(IDictionary<,>));
						_genericTypes.Add(dictType, type);
					}
				}

				if (type == null)
					return false;

				object key;
				var text = selectorInfo.Selector.Text;

				lock (_sync)
				{
					if (!_keys.TryGetValue(text, out key))
					{
						key = text.To(type.GetGenericArguments()[0]);
						_keys.Add(text, key);
					}
				}

				selectorInfo.Result = dictionary[key];
				return true;
			}
		}

		static StringHelper()
		{
			Smart.Default.AddExtensions(new DictionarySourceEx());
		}

		public static string PutEx(this string str, params object[] args)
		{
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			return args.Length == 0 ? str : Smart.Format(str, args);
		}

		private static Type GetGenericType(this Type targetType, Type genericType)
		{
			if (targetType == null)
				throw new ArgumentNullException(nameof(targetType));

			if (genericType == null)
				throw new ArgumentNullException(nameof(genericType));

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
#endif

		public static string[] Split(this string str, bool removeEmptyEntries = true)
		{
			return str.Split(Environment.NewLine, removeEmptyEntries);
		}

		public static string[] Split(this string str, string separator, bool removeEmptyEntries = true)
		{
			if (str == null)
				throw new ArgumentNullException(nameof(str));

			if (str.Length == 0)
				return ArrayHelper.Empty<string>();

			return str.Split(new[] { separator }, removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
		}

		public static string[] SplitByComma(this string str, string comma = ",", bool removeEmptyEntries = false)
		{
			return str.Split(comma, removeEmptyEntries);
		}

		public static int LastIndexOf(this StringBuilder builder, char value)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			for (var i = builder.Length - 1; i > 0; i--)
			{
				if (builder[i] == value)
					return i;
			}

			return -1;
		}

		/// <summary>
		/// true, if is valid email address
		/// from http://www.davidhayden.com/blog/dave/archive/2006/11/30/ExtensionMethodsCSharp.aspx
		/// </summary>
		/// <param name="email">email address to test</param>
		/// <returns>true, if is valid email address</returns>
		public static bool IsValidEmailAddress(this string email)
		{
			return new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$").IsMatch(email);
		}

		/// <summary>
		/// Checks if <paramref name="url"/> is valid. 
		/// from http://www.osix.net/modules/article/?id=586
		/// and changed to match http://localhost
		/// 
		/// complete (not only http) <paramref name="url"/> regex can be found 
		/// at http://internet.ls-la.net/folklore/url-regexpr.html
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static bool IsValidUrl(this string url)
		{
			const string strRegex = "^(https?://)"
									+ "?(([0-9a-z_!~*'().&=+$%-]+: )?[0-9a-z_!~*'().&=+$%-]+@)?" //user@
									+ @"(([0-9]{1,3}\.){3}[0-9]{1,3}" // IP- 199.194.52.184
									+ "|" // allows either IP or domain
									+ @"([0-9a-z_!~*'()-]+\.)*" // tertiary domain(s)- www.
									+ @"([0-9a-z][0-9a-z-]{0,61})?[0-9a-z]" // second level domain
									+ @"(\.[a-z]{2,6})?)" // first level domain- .com or .museum is optional
									+ "(:[0-9]{1,5})?" // port number- :80
									+ "((/?)|" // a slash isn't required if there is no file name
									+ "(/[0-9a-z_!~*'().;?:@&=+$,%#-]+)+/?)$";
			return new Regex(strRegex).IsMatch(url);
		}

		/// <summary>
		/// Reverse the string from http://en.wikipedia.org/wiki/Extension_method
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string Reverse(this string input)
		{
			var chars = input.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}

		/// <summary>
		/// Reduce string to shorter preview which is optionally ended by some string (...).
		/// </summary>
		/// <param name="s">string to reduce</param>
		/// <param name="count">Length of returned string including endings.</param>
		/// <param name="endings">optional endings of reduced text</param>
		/// <example>
		/// string description = "This is very long description of something";
		/// string preview = description.Reduce(20,"...");
		/// produce -> "This is very long..."
		/// </example>
		/// <returns></returns>
		public static string Reduce(this string s, int count, string endings)
		{
			if (endings.IsEmpty())
				throw new ArgumentNullException(nameof(endings));

			if (count < endings.Length || count >= endings.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			return s.Substring(0, count - endings.Length) + endings;
		}

		public static string ReplaceWhiteSpaces(this string s)
		{
			return s.ReplaceWhiteSpaces(' ');
		}

		public static string ReplaceWhiteSpaces(this string s, char c)
		{
			if (s == null)
				return null;

			var sb = new StringBuilder(s);

			for (var i = 0; i < sb.Length; i++)
			{
				if (char.IsWhiteSpace(sb[i]))
					sb[i] = c;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Remove white space, not line end useful when parsing user input such phone,
		/// price int.Parse("1 000 000".RemoveSpaces(),.....
		/// </summary>
		/// <param name="s"></param>
		/// <returns>string without spaces</returns>
		public static string RemoveSpaces(this string s)
		{
			return s.Remove(" ");
		}

		public static string Remove(this string s, string what, bool ignoreCase = false)
		{
			if (ignoreCase)
				return s.ReplaceIgnoreCase(what, string.Empty);
			else
				return s.Replace(what, string.Empty);
		}

		/// <summary>
		/// true, if the string can be parse as Double respective Int32 spaces are not considered.
		/// </summary>
		/// <param name="s">input string</param>
		/// <param name="floatPoint">true, if Double is considered, otherwise Int32 is considered.</param>
		/// <returns>true, if the string contains only digits or float-point</returns>
		public static bool IsNumber(this string s, bool floatPoint)
		{
			var withoutWhiteSpace = s.RemoveSpaces();

			if (floatPoint)
			{
				return double.TryParse(withoutWhiteSpace, NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
			}
			else
			{
				return int.TryParse(withoutWhiteSpace, out var i);
			}
		}

		/// <summary>
		/// true, if the string contains only digits or float-point.
		/// Spaces are not considered.
		/// </summary>
		/// <param name="s">input string</param>
		/// <param name="floatPoint">true, if float-point is considered</param>
		/// <returns>true, if the string contains only digits or float-point</returns>
		public static bool IsNumberOnly(this string s, bool floatPoint)
		{
			s = s.RemoveSpaces();

			if (s.Length == 0)
				return false;

			foreach (var c in s)
			{
				if (char.IsDigit(c))
					continue;

				if (floatPoint && (c == '.' || c == ','))
					continue;

				return false;
			}

			return true;
		}

		/// <summary>
		/// Remove accent from strings 
		/// </summary>
		/// <example>
		///  input:  "Příliš žluťoučký kůň úpěl ďábelské ódy."
		///  result: "Prilis zlutoucky kun upel dabelske ody."
		/// </example>
		/// <param name="s"></param>
		/// <remarks>founded at http://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net </remarks>
		/// <returns>string without accents</returns>
		public static string RemoveDiacritics(this string s)
		{
			var stFormD = s.Normalize(NormalizationForm.FormD);
			var sb = new StringBuilder();

			foreach (var t in from t in stFormD let uc = CharUnicodeInfo.GetUnicodeCategory(t) where uc != UnicodeCategory.NonSpacingMark select t)
			{
				sb.Append(t);
			}

			return sb.ToString().Normalize(NormalizationForm.FormC);
		}

		/// <summary>
		/// Replace \r\n or \n by <br /> from http://weblogs.asp.net/gunnarpeipman/archive/2007/11/18/c-extension-methods.aspx
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string Nl2Br(this string s)
		{
			return s.Replace("\r\n", "<br />").Replace("\n", "<br />");
		}

		public static string Trim(this string value, int maxLength)
		{
			if (value != null && value.Length > maxLength)
				return value.Substring(0, maxLength) + "...";
			else
				return value;
		}

		public static string Join(this IEnumerable<string> parts, string separator)
		{
			return string.Join(separator, parts.ToArray());
		}

		public static bool EqualsIgnoreCase(this string str1, string str2)
		{
			return string.Equals(str1, str2, StringComparison.InvariantCultureIgnoreCase);
		}

		public static bool CompareIgnoreCase(this string str1, string str2)
		{
			return string.Compare(str1, str2, StringComparison.InvariantCultureIgnoreCase) == 0;
		}

		//
		// http://ppetrov.wordpress.com/2008/06/27/useful-method-6-of-n-ignore-case-on-stringcontains/
		//
		public static bool ContainsIgnoreCase(this string str1, string str2)
		{
			if (str1 == null)
				throw new ArgumentNullException(nameof(str1));

			return str1.IndexOf(str2, StringComparison.InvariantCultureIgnoreCase) >= 0;
		}

		//
		// http://ppetrov.wordpress.com/2008/06/27/useful-method-6-of-n-ignore-case-on-stringreplace/
		//
		public static string ReplaceIgnoreCase(this string original, string oldValue, string newValue)
		{
			if (oldValue == null)
				throw new ArgumentNullException(nameof(oldValue));

			if (newValue == null)
				throw new ArgumentNullException(nameof(newValue));

			if (oldValue.Length == 0)
				return original?.Length == 0 ? newValue : original;

			var result = original;

			if (oldValue != newValue)
			{
				int index;
				var lastIndex = 0;

				var buffer = new StringBuilder();

				while ((index = original.IndexOf(oldValue, lastIndex, StringComparison.InvariantCultureIgnoreCase)) >= 0)
				{
					if (lastIndex != (index - lastIndex))
						buffer.Append(original, lastIndex, index - lastIndex);

					buffer.Append(newValue);

					lastIndex = index + oldValue.Length;
				}

				buffer.Append(original, lastIndex, original.Length - lastIndex);

				result = buffer.ToString();
			}

			return result;
		}

		public static StringBuilder ReplaceIgnoreCase(this StringBuilder builder, string oldValue, string newValue)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			var str = builder.ToString().ReplaceIgnoreCase(oldValue, newValue);
			return builder
				.Clear()
				.Append(str);
		}

		public static bool StartsWithIgnoreCase(this string str1, string str2)
		{
			if (str1 == null)
				throw new ArgumentNullException(nameof(str1));

			return str1.StartsWith(str2, StringComparison.InvariantCultureIgnoreCase);
		}

		public static bool EndsWithIgnoreCase(this string str1, string str2)
		{
			if (str1 == null)
				throw new ArgumentNullException(nameof(str1));

			return str1.EndsWith(str2, StringComparison.InvariantCultureIgnoreCase);
		}

		public static int IndexOfIgnoreCase(this string str1, string str2)
		{
			if (str1 == null)
				throw new ArgumentNullException(nameof(str1));

			return str1.IndexOf(str2, StringComparison.InvariantCultureIgnoreCase);
		}

		//
		// http://ppetrov.wordpress.com/2008/06/30/useful-method-8-of-n-string-capitalize-firsttotitlecase/
		//
		public static string ToTitleCase(this string value)
		{
			var ti = Thread.CurrentThread.CurrentCulture.TextInfo;
			return ti.ToTitleCase(value);
		}

		//
		// http://ppetrov.wordpress.com/2008/06/13/useful-method-1-of-n/
		//
		public static string Times(this string value, int n)
		{
			return value.Times(n, string.Empty);
		}

		public static string Times(this string value, int n, string separator)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (n < 0)
				throw new ArgumentException("Must be a positive number.", nameof(n));

			if (value.Length > 0 && n > 0)
				return Enumerable.Repeat(value, n).Join(separator);

			return value;
		}

		////
		//// http://ppetrov.wordpress.com/2008/06/24/useful-method-4-of-n/
		////
		//public static string Aggregate<T>(this IEnumerable<T> values)
		//{
		//    return Aggregate(values, ",");
		//}

		//public static string Aggregate<T>(this IEnumerable<T> values, string separator)
		//{
		//    return values.Aggregate(x => x.To<string>(), separator);
		//}

		//public static string Aggregate<T>(this IEnumerable<T> values, Func<T, string> toString)
		//{
		//    return values.Aggregate(toString, ",");
		//}

		//public static string Aggregate<T>(this IEnumerable<T> values, Func<T, string> toString, string separator)
		//{
		//    if (values == null)
		//        throw new ArgumentNullException("values");

		//    if (toString == null)
		//        throw new ArgumentNullException("toString");

		//    var buffer = new StringBuilder();

		//    foreach (var v in values)
		//    {
		//        if (buffer.Length > 0)
		//            buffer.Append(separator);

		//        buffer.Append(toString(v));
		//    }

		//    return buffer.ToString();
		//}



		//
		// http://www.extensionmethod.net/Details.aspx?ID=123
		//

		public static string Truncate(this string text, int maxLength)
		{
			return text.Truncate(maxLength, "...");
		}

		/// <summary>
		/// Truncates the string to a specified length and replace the truncated to a ...
		/// </summary>
		/// <param name="text">string that will be truncated</param>
		/// <param name="maxLength">total length of characters to maintain before the truncate happens</param>
		/// <param name="suffix"></param>
		/// <returns>truncated string</returns>
		public static string Truncate(this string text, int maxLength, string suffix)
		{
#if !SILVERLIGHT
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength", "maxLength is negative.");
#endif

			var truncatedString = text;

			if (maxLength == 0)
				return truncatedString;

			var strLength = maxLength - suffix.Length;

			if (strLength <= 0)
				return truncatedString;

			if (text == null || text.Length <= maxLength)
				return truncatedString;

			truncatedString = text.Substring(0, strLength);
			truncatedString = truncatedString.TrimEnd();
			truncatedString += suffix;

			return truncatedString;
		}

		public static string TruncateMiddle(this string input, int limit)
		{
			if (input.IsEmpty())
				return input;

			var output = input;
			const string middle = "...";

			// Check if the string is longer than the allowed amount
			// otherwise do nothing
			if (output.Length <= limit || limit <= 0)
				return output;

			// figure out how much to make it fit...
			var left = (limit / 2) - (middle.Length / 2);
			var right = limit - left - (middle.Length / 2);

			if ((left + right + middle.Length) < limit)
			{
				right++;
			}
			else if ((left + right + middle.Length) > limit)
			{
				right--;
			}

			// cut the left side
			output = input.Substring(0, left);

			// add the middle
			output += middle;

			// add the right side...
			output += input.Substring(input.Length - right, right);

			return output;
		}

		public static string RemoveTrailingZeros(this string s)
		{
			return s.RemoveTrailingZeros(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
		}

		public static string RemoveTrailingZeros(this string s, string separator)
		{
			if (s.IsEmpty())
				throw new ArgumentNullException(nameof(s));

			if (separator.IsEmpty())
				throw new ArgumentNullException(nameof(separator));

			s = s.TrimStart('0').TrimEnd('0');

			//var index = s.IndexOf(separator);

			//if (index == -1)
			//    index = s.Length;

			//var endIndex = 0;

			//for (var i = 0; i < index; i++)
			//{
			//    if (s[i] == '0')
			//        endIndex = i + 1;
			//    else
			//        break;
			//}

			//if (endIndex > 0)
			//    s = s.Substring(endIndex);

			//for (var i = s.Length - 1; i > index; i--)
			//{
			//    if (s[i] == '0')
			//        endIndex = i - 1;
			//    else
			//        break;
			//}

			//s = s.TrimStart('0').TrimEnd('0', separator[0]);

			if (s.StartsWith(separator))
				s = "0" + s;

			if (s.EndsWith(separator))
				s = s.Substring(0, s.Length - 1);

			if (s.IsEmpty())
				s = "0";

			return s;
		}

		public static byte[] Base64(this string value)
		{
			return Convert.FromBase64String(value);
		}

		public static string Base64(this byte[] value)
		{
			return Convert.ToBase64String(value);
		}

		public static IEnumerable<string> SplitByLength(this string stringToSplit, int length)
		{
			while (stringToSplit.Length > length)
			{
				yield return stringToSplit.Substring(0, length);
				stringToSplit = stringToSplit.Substring(length);
			}

			if (stringToSplit.Length > 0)
				yield return stringToSplit;
		}

		public static string TrimStart(this string str, string sStartValue)
		{
			return str.StartsWith(sStartValue) ? str.Remove(0, sStartValue.Length) : str;
		}

		public static string TrimEnd(this string str, string sEndValue)
		{
			return str.EndsWith(sEndValue) ? str.Remove(str.Length - sEndValue.Length, sEndValue.Length) : str;
		}

		public static bool CheckBrackets(this string str, string sStart, string sEnd)
		{
			return str.StartsWith(sStart) && str.EndsWith(sEnd);
		}

		public static string StripBrackets(this string str, string sStart, string sEnd)
		{
			return str.CheckBrackets(sStart, sEnd)
				       ? str.Substring(sStart.Length, (str.Length - sStart.Length) - sEnd.Length)
				       : str;
		}

		private static readonly Dictionary<char, string> _charMap = new Dictionary<char, string>
		{
			{ 'а', "a" },
			{ 'б', "b" },
			{ 'в', "v" },
			{ 'г', "g" },
			{ 'д', "d" },
			{ 'е', "e" },
			{ 'ё', "yo" },
			{ 'ж', "zh" },
			{ 'з', "z" },
			{ 'и', "i" },
			{ 'й', "i" },
			{ 'к', "k" },
			{ 'л', "l" },
			{ 'м', "m" },
			{ 'н', "n" },
			{ 'о', "o" },
			{ 'п', "p" },
			{ 'р', "r" },
			{ 'с', "s" },
			{ 'т', "t" },
			{ 'у', "u" },
			{ 'ф', "f" },
			{ 'х', "h" },
			{ 'ц', "ts" },
			{ 'ч', "ch" },
			{ 'ш', "sh" },
			{ 'щ', "shsh" },
			{ 'ы', "y" },
			{ 'э', "eh" },
			{ 'ю', "yu" },
			{ 'я', "ya" },
			{ 'ь', "'" },
			{ 'ъ', "'" },
		};

		public static string ToLatin(this string russianTitle)
		{
			var transliter = string.Empty;

			foreach (var letter in russianTitle.ToLower())
			{
				if (_charMap.TryGetValue(letter, out var mappedLetter))
					transliter += mappedLetter;
				else
					transliter += letter;
			}

			return transliter;
		}

		public static string LightScreening(this string text)
		{
			return text.Replace(' ', '-').Remove(".").Remove("#").Remove("?").Remove(":");
		}

		public static bool ComparePaths(this string path1, string path2)
		{
			// http://stackoverflow.com/questions/2281531/how-can-i-compare-directory-paths-in-c
			return Path.GetFullPath(path1).TrimEnd('\\').CompareIgnoreCase(Path.GetFullPath(path2).TrimEnd('\\'));
		}

		public static bool Like(this string toSearch, string toFind, bool ignoreCase = true)
		{
			var option = RegexOptions.Singleline;

			if (ignoreCase)
				option = RegexOptions.IgnoreCase;

			return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", option).IsMatch(toSearch);
		}

		[CLSCompliant(false)]
		public static bool IsEmpty(this SecureString secureString)
		{
			return secureString == null
#if SILVERLIGHT
				;
#else
				|| secureString.Length == 0;
#endif
		}

#if !SILVERLIGHT
		public static bool IsEqualTo(this SecureString value1, SecureString value2)
		{
			if (value1 == null)
				throw new ArgumentNullException(nameof(value1));

			if (value2 == null)
				throw new ArgumentNullException(nameof(value2));

			if (value1.Length != value2.Length)
				return false;

			var bstr1 = IntPtr.Zero;
			var bstr2 = IntPtr.Zero;

			try
			{
				bstr1 = Marshal.SecureStringToBSTR(value1);
				bstr2 = Marshal.SecureStringToBSTR(value2);

				var length = Marshal.ReadInt32(bstr1, -4);

				for (var x = 0; x < length; ++x)
				{
					var byte1 = Marshal.ReadByte(bstr1, x);
					var byte2 = Marshal.ReadByte(bstr2, x);

					if (byte1 != byte2)
						return false;
				}

				return true;
			}
			finally
			{
				if (bstr2 != IntPtr.Zero)
					Marshal.ZeroFreeBSTR(bstr2);

				if (bstr1 != IntPtr.Zero)
					Marshal.ZeroFreeBSTR(bstr1);
			}
		}
#endif

		public static string Digest(this byte[] digest)
		{
			return BitConverter.ToString(digest).Remove("-");
		}

		private static readonly byte[] _initVectorBytes = Encoding.ASCII.GetBytes("ss14fgty650h8u82");
		private const int _keysize = 256;

		public static string Encrypt(this string data, string password)
		{
			using (var pwd = new Rfc2898DeriveBytes(password, _initVectorBytes))
			{
				var keyBytes = pwd.GetBytes(_keysize / 8);

				using (var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC })
				using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, _initVectorBytes))
				using (var memoryStream = new MemoryStream())
				using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
				{
					var bytes = Encoding.UTF8.GetBytes(data);

					cryptoStream.Write(bytes, 0, bytes.Length);
					cryptoStream.FlushFinalBlock();

					return Convert.ToBase64String(memoryStream.ToArray());
				}
			}
		}

		public static string Decrypt(this string data, string password)
		{
			using (var pwd = new Rfc2898DeriveBytes(password, _initVectorBytes))
			{
				var cipherTextBytes = Convert.FromBase64String(data);
				var keyBytes = pwd.GetBytes(_keysize / 8);

				using (var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC })
				using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, _initVectorBytes))
				using (var memoryStream = new MemoryStream(cipherTextBytes))
				using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
				{
					var plainTextBytes = new byte[cipherTextBytes.Length];
					var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
					return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
				}
			}
		}

		public static Encoding WindowsCyrillic => Encoding.GetEncoding(1251);

		public static IEnumerable<string> Duplicates(this IEnumerable<string> items)
			=> items.GroupBy(s => s, s => StringComparer.InvariantCultureIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key);
	}
}