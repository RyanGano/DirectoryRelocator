using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Xml;
using DirectoryRelocator.Utility;

namespace DirectoryRelocator
{
	public class DirectoryDetails : IEquatable<DirectoryDetails>, IComparable<DirectoryDetails>
	{
		public DirectoryDetails(string path) : this(path, true)
		{
		}
		
		public DirectoryStatus DirectoryStatus { get; private set; }

		public string Path { get; private set; }
		
		public string ShortPath { get; private set; }

		public long DirectorySize { get; private set; }

		public DateTime LastAccessed { get; private set; }
		
		public bool Equals(DirectoryDetails other)
		{
			return Path.Equals(other.Path, StringComparison.CurrentCultureIgnoreCase);
		}

		public int CompareTo(DirectoryDetails other)
		{
			return DirectorySize.CompareTo(other.DirectorySize);
		}

		public void Write(XmlWriter xmlWriter, DirectoryStatus status)
		{
			// Write out the information
			xmlWriter.WriteStartElement(DirectoryDetailsName);
			xmlWriter.WriteAttributeString(c_path, Path);
			xmlWriter.WriteAttributeString(c_status, status.ToString());
			xmlWriter.WriteEndElement();
		}

		public static void Read(XmlReader reader, List<DirectoryDetails> ignoredDirectories, List<DirectoryDetails> skippedDirectories)
		{
			DirectoryDetails details = new DirectoryDetails(reader.GetAttribute(c_path), false);

			DirectoryStatus status;

			if (Enum.TryParse(reader.GetAttribute(c_status), out status))
			{
				if (status == DirectoryStatus.Ignored)
					ignoredDirectories.Add(details);
				else if (status == DirectoryStatus.Skipped)
					skippedDirectories.Add(details);
			}
		}

		private DirectoryDetails(string path, bool updateStatus)
		{
			Path = path;
			ShortPath = new DirectoryInfo(path).Name;

			FileSystemInfo[] fileSystemInfos = new DirectoryInfo(path).GetFileSystemInfos();

			LastAccessed = fileSystemInfos.Select(info => info.LastAccessTime).OrderBy(accessTime => accessTime).FirstOrDefault();

			DirectorySize = DirectoryUtility.GetDirectorySize(new DirectoryInfo(path));

			if (updateStatus)
				DirectoryStatus = DirectoryUtility.GetDirectoryStatus(Path);
		}

		public static readonly IValueConverter ConvertDirectoryStatusToText = new DirectoryStatusToTextConverter();
		public static readonly IValueConverter ConvertDirectoryStatusToColor = new DirectoryStatusToColorConverter();
		public static readonly IValueConverter ConvertDirectorySizeToSmallForm = new DirectorySizeToSmallFormConverter();

		private static string c_path = "Path";
		private static string c_status = "Status";

		public const string DirectoryDetailsName = "DirectoryDetails";

	}

	[ValueConversion(typeof(DirectoryStatus), typeof(String))]
	public class DirectoryStatusToTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			switch (value as DirectoryStatus?)
			{
				case DirectoryStatus.BackupAvailable:
					return "Backup Available";
				case DirectoryStatus.JunctionAvailable:
					return "Linked to backup";
				default:
					return "No backup available";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	[ValueConversion(typeof(DirectoryStatus), typeof(String))]
	public class DirectoryStatusToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			switch (value as DirectoryStatus?)
			{
				case DirectoryStatus.BackupAvailable:
					return c_backupAvailable;
				case DirectoryStatus.JunctionAvailable:
					return c_junctionAvailable;
				default:
					return c_noBackupAvailable;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}

		private const string c_backupAvailable = "#ff00cc00";
		private const string c_junctionAvailable = "#ff0000cc";
		private const string c_noBackupAvailable = "#ffcccccc";
	}

	[ValueConversion(typeof(long), typeof(String))]
	public class DirectorySizeToSmallFormConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			long? size = value as long?;

			if (size.HasValue)
			{
				if (size < c_kiloByte)
					return size + "B";
				if (size < c_megaByte)
					return Math.Round((double) size/c_kiloByte, 2) + "KB";
				if (size < c_gigaByte)
					return Math.Round((double) size/c_megaByte, 2) + "MB";
				if (size < c_teraByte)
					return Math.Round((double) size/c_gigaByte, 2) + "GB";

				return Math.Round((double) size/c_teraByte, 2) + "TB";
			}

			return "[Unknown]";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		
		private const long c_kiloByte = 1024;
		private const long c_megaByte = c_kiloByte * c_kiloByte;
		private const long c_gigaByte = c_megaByte * c_kiloByte;
		private const long c_teraByte = c_gigaByte * c_kiloByte;
	}
}
