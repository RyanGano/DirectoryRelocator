using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using DirectoryRelocator.Annotations;
using DirectoryRelocator.Utility;

namespace DirectoryRelocator
{
	public class DirectoryDetails : DependencyObject, IEquatable<DirectoryDetails>, IComparable<DirectoryDetails>, INotifyPropertyChanged
	{
		public DirectoryDetails(string path) : this(path, true)
		{
		}

		public static readonly DependencyProperty DirectoryStatusProperty =
			DependencyProperty.Register("DirectoryStatus", typeof(DirectoryStatus), typeof(DirectoryDetails));

		public DirectoryStatus DirectoryStatus
		{
			get { return (DirectoryStatus) GetValue(DirectoryStatusProperty); }
			private set { SetValue(DirectoryStatusProperty, value);}
		}

		public static readonly DependencyProperty PathProperty =
			DependencyProperty.Register("Path", typeof(string), typeof(DirectoryDetails));

		public string Path
		{
			get { return (string)GetValue(PathProperty); }
			private set { SetValue(PathProperty, value); }
		}

		public string ShortPath { get; private set; }

		public static readonly DependencyProperty DirectorySizeProperty =
			DependencyProperty.Register("DirectorySize", typeof(long), typeof(DirectoryDetails));

		public long DirectorySize
		{
			get { return (long)GetValue(DirectorySizeProperty); }
			private set { SetValue(DirectorySizeProperty, value); }
		}

		public static readonly DependencyProperty LastAccessedProperty =
			DependencyProperty.Register("LastAccessed", typeof(DateTime), typeof(DirectoryDetails));

		public DateTime LastAccessed
		{
			get { return (DateTime)GetValue(LastAccessedProperty); }
			private set
			{
				
				SetValue(LastAccessedProperty, value);
			}
		}
		
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
			
			m_createJunction = new Command(CreateJunction, CanCreateJunction);
			m_clearJunction = new Command(ClearJunction, CanClearJunction);
			m_skipDirectory = new Command(SkipDirectory, CanSkipDirectory);
			m_ignoreDirectory = new Command(IgnoreDirectory, CanIgnoreDirectory);
			
			UpdateDetails(updateStatus);
		}

		private void UpdateDetails(bool updateStatus)
		{
			FileSystemInfo[] fileSystemInfos = new DirectoryInfo(Path).GetFileSystemInfos();

			LastAccessed = fileSystemInfos.Select(info => info.LastAccessTime).OrderBy(accessTime => accessTime).FirstOrDefault();

			DirectorySize = DirectoryUtility.GetDirectorySize(new DirectoryInfo(Path));

			if (updateStatus)
				DirectoryStatus = DirectoryUtility.GetDirectoryStatus(Path);

			CreateJunctionCommand.RaiseCanExecuteChanged();
			ClearJunctionCommand.RaiseCanExecuteChanged();
		}

		private void CreateJunction()
		{
			if (!CanCreateJunction())
				return;

			DirectoryUtility.CreateJunction(Path);
			UpdateDetails(true);
		}

		private bool CanCreateJunction()
		{
			return DirectoryStatus != DirectoryStatus.JunctionAvailable;
		}

		private void ClearJunction()
		{
			if (!CanClearJunction())
				return;

			DirectoryUtility.RemoveJunction(Path);
			UpdateDetails(true);
		}

		private bool CanClearJunction()
		{
			return DirectoryStatus == DirectoryStatus.JunctionAvailable;
		}

		private void SkipDirectory()
		{
			if (!CanSkipDirectory())
				return;

			DirectoryStatus = DirectoryStatus.Skipped;
			OnPropertyChanged(DirectoryStatusProperty.Name);
		}

		private bool CanSkipDirectory()
		{
			return true;
		}

		private void IgnoreDirectory()
		{
			if (!CanIgnoreDirectory())
				return;

			DirectoryStatus = DirectoryStatus.Ignored;
			OnPropertyChanged(DirectoryStatusProperty.Name);
		}

		private bool CanIgnoreDirectory()
		{
			return true;
		}

		public Command CreateJunctionCommand { get { return m_createJunction; } }
		public Command ClearJunctionCommand { get { return m_clearJunction; } }
		public Command SkipDirectoryCommand { get { return m_skipDirectory; } }
		public Command IgnoreDirectoryCommand { get { return m_ignoreDirectory; } }
		
		public static readonly IValueConverter ConvertDirectoryStatusToText = new DirectoryStatusToTextConverter();
		public static readonly IValueConverter ConvertDirectoryStatusToColor = new DirectoryStatusToColorConverter();
		public static readonly IValueConverter ConvertDirectorySizeToSmallForm = new DirectorySizeToSmallFormConverter();

		private const string c_path = "Path";
		private const string c_status = "Status";

		public const string DirectoryDetailsName = "DirectoryDetails";
		
		private readonly Command m_createJunction;
		private readonly Command m_clearJunction;
		private readonly Command m_skipDirectory;
		private readonly Command m_ignoreDirectory;
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
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
