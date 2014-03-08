using System.Collections.Generic;

namespace DirectoryRelocator.Utility
{
	internal static class GenericUtility
	{
		public static int InvertCompare(DirectoryDetails x, DirectoryDetails y)
		{
			return x.CompareTo(y)*-1;
		}

		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> self)
		{
			return self ?? new List<T>();
		}
	}
}