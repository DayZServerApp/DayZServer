using System;
using System.Collections.Generic;
using System.Linq;

namespace DayZServer 
{

	public static class ExtensionMethods
	{
		public static int TryInt(this string val)
		{
			int result;
			if (int.TryParse(val, out result))
			{
				return result;
			}
			return 0;
		}

		public static int? TryIntNullable(this string val)
		{
			int result;
			if (int.TryParse(val, out result))
			{
				return result;
			}
			return null;
		}

		public static List<T> ToList<T>(this IEnumerable<T> items, Action<T> action)
		{
			List<T> list = items.ToList();
			foreach (T item in list)
			{
				action(item);
			}
			return list;
		}

		public static bool None<T>(this IEnumerable<T> items, Func<T, bool> predicate)
		{
			return !items.Any(predicate);
		}

		public static bool In(this string value, params string[] values)
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}

			foreach (string s in values)
			{
				if (value.Equals(s, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		public static bool EndsWithAny(this string value, params string[] values)
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}
			foreach (string s in values)
			{
				if (value.EndsWith(s, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		public static bool SafeContainsIgnoreCase(this string value, string contains)
		{
			if (string.IsNullOrEmpty(value))
				return false;

			return value.IndexOf(contains, StringComparison.CurrentCultureIgnoreCase) > -1;
		}
	}
}