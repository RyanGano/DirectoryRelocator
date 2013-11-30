using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml;
using DirectoryRelocator.Annotations;

namespace DirectoryRelocator
{
	public class DirectoryLink : DependencyObject, INotifyPropertyChanged, IComparable<DirectoryLink>
	{
		public DirectoryLink()
		{
		}

		public DirectoryLink(XmlReader reader)
		{
			Name = reader.GetAttribute(c_name);
			OriginalPath = reader.GetAttribute(c_originalPath);
			BackupPath = reader.GetAttribute(c_backupPath);
		}

		public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
			"Name", typeof(string), typeof(DirectoryLink));

		public string Name
		{
			get { return (string) GetValue(NameProperty); }
			set {SetValue(NameProperty, value);}
		}

		public static readonly DependencyProperty OriginalPathProperty = DependencyProperty.Register(
			"OriginalPath", typeof(string), typeof(DirectoryLink));

		public string OriginalPath
		{
			get { return (string) GetValue(OriginalPathProperty); }
			set {SetValue(OriginalPathProperty, value);}
		}
		
		public static readonly DependencyProperty BackupPathProperty = DependencyProperty.Register(
			"BackupPath", typeof(string), typeof(DirectoryLink));

		public string BackupPath
		{
			get { return (string) GetValue(BackupPathProperty); }
			set {SetValue(BackupPathProperty, value);}
		}

		public virtual bool IsEditing {
			get { return false; }
		}
		
		public int CompareTo(DirectoryLink other)
		{
			return String.Compare(Name, other.Name, StringComparison.Ordinal);
		}

		public void Copy(DirectoryLink other)
		{
			Name = other.Name;
			OriginalPath = other.OriginalPath;
			BackupPath = other.BackupPath;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Write(XmlWriter xmlWriter)
		{
			xmlWriter.WriteStartElement(DirectoryLinkName);
			xmlWriter.WriteAttributeString(c_name, Name);
			xmlWriter.WriteAttributeString(c_originalPath, OriginalPath);
			xmlWriter.WriteAttributeString(c_backupPath, BackupPath);
			xmlWriter.WriteEndElement();
		}

		public const string DirectoryLinkName = "DirectoryLink";

		private const string c_name = "Name";
		private const string c_originalPath = "OriginalPath";
		private const string c_backupPath = "BackupPath";
	}

	public class EditableDirectoryLink : DirectoryLink
	{
		public override bool IsEditing
		{
			get { return true; }
		}

		public DirectoryLink OriginalDirectoryLink { get; set; }
	}
}