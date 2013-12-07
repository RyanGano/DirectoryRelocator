namespace DirectoryRelocator.Utility
{
	internal class GenericUtility
	{
		public static int InvertCompare(DirectoryDetails x, DirectoryDetails y)
		{
			return x.CompareTo(y)*-1;
		}
	}
}