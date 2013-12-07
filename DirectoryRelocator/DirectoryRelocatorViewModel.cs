using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Xml;
using DirectoryRelocator.Utility;

namespace DirectoryRelocator
{
	public class DirectoryRelocatorViewModel : DependencyObject
	{
		public DirectoryRelocatorViewModel()
		{
			m_createJunction = new Command(CreateJunction, CanCreateJunction);
			m_clearJunction = new Command(ClearJunction, CanClearJunction);
			m_refreshList = new Command(RefreshList, CanRefreshList);
			m_skipSelectedDirectory = new Command(SkipSelectedDirectory, CanSkipSelectedDirectory);
			m_ignoreSelectedDirectory = new Command(IgnoreSelectedDirectory, CanIgnoreSelectedDirectory);
			m_editDirectoryLink = new Command(EditDirectoryLink, CanEditDirectoryLink);
			m_saveDirectoryLink = new Command(SaveDirectoryLink, CanSaveDirectoryLink);
			m_copyDirectoryLink = new Command(CopyDirectoryLink, CanCopyDirectoryLink);
			m_cancelEditDirectoryLink = new Command(CancelEditDirectoryLink, CanCancelEditDirectoryLink);
			m_deleteDirectoryLink = new Command(DeleteDirectoryLink, CanDeleteDirectoryLink);

			m_ignoredDirectories = new List<DirectoryDetails>();
			m_skippedDirectories = new List<DirectoryDetails>();

			LoadPreferences();
		}

		public static readonly DependencyProperty DirectoryListProperty = DependencyProperty.Register(
			"DirectoryList", typeof(List<DirectoryDetails>), typeof(DirectoryRelocatorViewModel), new PropertyMetadata(null));

		public List<DirectoryDetails> DirectoryList
		{
			get
			{
				return (List<DirectoryDetails>) GetValue(DirectoryListProperty);
			}
			set
			{
				SetValue(DirectoryListProperty, value);
			}
		}

		public static readonly DependencyProperty SelectedDirectoryProperty = DependencyProperty.Register(
			"SelectedDirectory", typeof(DirectoryDetails), typeof(DirectoryRelocatorViewModel), new PropertyMetadata(null, OnSelectedDirectoryChanged));

		public DirectoryDetails SelectedDirectory
		{
			get
			{
				return (DirectoryDetails)GetValue(SelectedDirectoryProperty);
			}
			set
			{
				SetValue(SelectedDirectoryProperty, value);
			}
		}

		public static readonly DependencyProperty StoredDirectoryLinksProperty =
			DependencyProperty.Register("StoredDirectoryLinks", typeof(ObservableCollection<DirectoryLink>), typeof(DirectoryRelocatorViewModel), new PropertyMetadata(new ObservableCollection<DirectoryLink>()));

		public ObservableCollection<DirectoryLink> StoredDirectoryLinks
		{
			get { return (ObservableCollection<DirectoryLink>)GetValue(StoredDirectoryLinksProperty); }
		}

		public void AddStoredDirectoryLink(DirectoryLink link)
		{
			StoredDirectoryLinks.Add(link);
		}

		public static readonly DependencyProperty SelectedDirectoryLinkProperty = DependencyProperty.Register(
			"SelectedDirectoryLink", typeof(DirectoryLink), typeof(DirectoryRelocatorViewModel), new PropertyMetadata(null, OnSelectedDirectoryLinkChanged));

		public DirectoryLink SelectedDirectoryLink
		{
			get
			{
				return (DirectoryLink)GetValue(SelectedDirectoryLinkProperty);
			}
			set
			{
				SetValue(SelectedDirectoryLinkProperty, value);
			}
		}

		public Command CreateJunctionCommand { get { return m_createJunction; } }
		public Command ClearJunctionCommand { get { return m_clearJunction; } }
		public Command RefreshListCommand { get { return m_refreshList; } }
		public Command SkipSelectedDirectoryCommand { get { return m_skipSelectedDirectory; } }
		public Command IgnoreSelectedDirectoryCommand { get { return m_ignoreSelectedDirectory; } }
		public Command EditDirectoryLinkCommand { get { return m_editDirectoryLink; } }
		public Command SaveDirectoryLinkCommand { get { return m_saveDirectoryLink; } }
		public Command CopyDirectoryLinkCommand { get { return m_copyDirectoryLink; } }
		public Command CancelEditDirectoryLinkCommand { get { return m_cancelEditDirectoryLink; } }
		public Command DeleteDirectoryLinkCommand { get { return m_deleteDirectoryLink; } }

		private static void OnSelectedDirectoryChanged(DependencyObject caller, DependencyPropertyChangedEventArgs eventArgs)
		{
			if (!(caller is DirectoryRelocatorViewModel))
				throw new ArgumentException("DependencyObject should be a DirectoryRelocatorViewModel");

			((DirectoryRelocatorViewModel)caller).UpdateDirectoryLinkEditButtons();
		}
		private static void OnSelectedDirectoryLinkChanged(DependencyObject caller, DependencyPropertyChangedEventArgs eventArgs)
		{
			if (!(caller is DirectoryRelocatorViewModel))
				throw new ArgumentException("DependencyObject should be a DirectoryRelocatorViewModel");

			DirectoryRelocatorViewModel viewModel = caller as DirectoryRelocatorViewModel;

			if (eventArgs.OldValue as DirectoryLink != null)
				(eventArgs.OldValue as DirectoryLink).PropertyChanged -= viewModel.OnDirectoryLinkInfoChanged;

			if (eventArgs.NewValue as DirectoryLink != null)
				(eventArgs.NewValue as DirectoryLink).PropertyChanged += viewModel.OnDirectoryLinkInfoChanged;

			UpdateDirectoryList(viewModel);
		}

		private void CreateJunction()
		{
			if (!CanCreateJunction())
				return;

			DirectoryUtility.CreateJunction(SelectedDirectory.Path);

			UpdateDirectoryList(this);
		}

		private bool CanCreateJunction()
		{
			return SelectedDirectory != null && SelectedDirectory.DirectoryStatus != DirectoryStatus.JunctionAvailable;
		}

		private void ClearJunction()
		{
			if (!CanClearJunction())
				return;

			DirectoryUtility.RemoveJunction(SelectedDirectory.Path);

			UpdateDirectoryList(this);
		}

		private bool CanClearJunction()
		{
			return SelectedDirectory != null && SelectedDirectory.DirectoryStatus == DirectoryStatus.JunctionAvailable;
		}

		private void RefreshList()
		{
			if (!CanRefreshList())
				return;

			UpdateDirectoryList(this);
		}

		private bool CanRefreshList()
		{
			return true;
		}

		private void SkipSelectedDirectory()
		{
			if (!CanSkipSelectedDirectory())
				return;

			m_skippedDirectories.Add(SelectedDirectory);
			SavePreferences();

			UpdateDirectoryList(this);
		}

		private bool CanSkipSelectedDirectory()
		{
			return SelectedDirectory != null;
		}

		private void IgnoreSelectedDirectory()
		{
			if (!CanIgnoreSelectedDirectory())
				return;

			m_ignoredDirectories.Add(SelectedDirectory);
			SavePreferences();

			UpdateDirectoryList(this);
		}

		private bool CanIgnoreSelectedDirectory()
		{
			return SelectedDirectory != null;
		}

		private void EditDirectoryLink()
		{
			if (!CanEditDirectoryLink())
				return;

			m_directoryLinkInEditMode.Copy(SelectedDirectoryLink);
			m_directoryLinkInEditMode.OriginalDirectoryLink = SelectedDirectoryLink;
			SelectedDirectoryLink = m_directoryLinkInEditMode;

			UpdateDirectoryLinkEditButtons();
		}

		private bool CanEditDirectoryLink()
		{
			return SelectedDirectoryLink != null && !SelectedDirectoryLink.IsEditing;
		}

		private void CopyDirectoryLink()
		{
			if (!CanCopyDirectoryLink())
				return;

			m_directoryLinkInEditMode.Copy(SelectedDirectoryLink);
			m_directoryLinkInEditMode.Name += " - Copy";

			SelectedDirectoryLink = m_directoryLinkInEditMode;

			UpdateDirectoryLinkEditButtons();
		}

		private bool CanCopyDirectoryLink()
		{
			return SelectedDirectoryLink != null && !SelectedDirectoryLink.IsEditing;
		}

		private void SaveDirectoryLink()
		{
			if (!CanSaveDirectoryLink())
				return;

			if (m_directoryLinkInEditMode.OriginalDirectoryLink != null)
			{
				m_directoryLinkInEditMode.OriginalDirectoryLink.Copy(m_directoryLinkInEditMode);
				SelectedDirectoryLink = m_directoryLinkInEditMode.OriginalDirectoryLink;
			}
			else
			{
				DirectoryLink newLink = new DirectoryLink();
				newLink.Copy(m_directoryLinkInEditMode);
				AddStoredDirectoryLink(newLink);
				SelectedDirectoryLink = newLink;
			}

			UpdateDirectoryLinkEditButtons();

			SavePreferences();
		}

		private bool CanSaveDirectoryLink()
		{
			return SelectedDirectoryLink != null && SelectedDirectoryLink.IsEditing;
		}

		private void CancelEditDirectoryLink()
		{
			if (!CanCancelEditDirectoryLink())
				return;

			if (m_directoryLinkInEditMode.OriginalDirectoryLink != null)
				SelectedDirectoryLink = m_directoryLinkInEditMode.OriginalDirectoryLink;

			SelectedDirectoryLink = StoredDirectoryLinks.FirstOrDefault();

			UpdateDirectoryLinkEditButtons();
		}

		private void UpdateDirectoryLinkEditButtons()
		{
			EditDirectoryLinkCommand.RaiseCanExecuteChanged();
			SaveDirectoryLinkCommand.RaiseCanExecuteChanged();
			CopyDirectoryLinkCommand.RaiseCanExecuteChanged();
			RefreshListCommand.RaiseCanExecuteChanged();
			SkipSelectedDirectoryCommand.RaiseCanExecuteChanged();
			IgnoreSelectedDirectoryCommand.RaiseCanExecuteChanged();
			CancelEditDirectoryLinkCommand.RaiseCanExecuteChanged();
			DeleteDirectoryLinkCommand.RaiseCanExecuteChanged();
			CreateJunctionCommand.RaiseCanExecuteChanged();
			ClearJunctionCommand.RaiseCanExecuteChanged();
		}

		private bool CanCancelEditDirectoryLink()
		{
			return SelectedDirectoryLink != null && SelectedDirectoryLink.IsEditing;
		}

		private void DeleteDirectoryLink()
		{
			if (!CanDeleteDirectoryLink())
				return;

			DirectoryLink selected = SelectedDirectoryLink;
			SelectedDirectoryLink = StoredDirectoryLinks.FirstOrDefault(link => !Equals(link, selected));
			StoredDirectoryLinks.Remove(selected);

			SavePreferences();
		}

		private bool CanDeleteDirectoryLink()
		{
			return SelectedDirectoryLink != null && StoredDirectoryLinks.Count > 1;
		}

		private static void UpdateDirectoryList(DirectoryRelocatorViewModel model)
		{
			string selectedPath = null;

			if (model.SelectedDirectory != null)
				selectedPath = model.SelectedDirectory.Path;

			model.DirectoryList = DirectoryUtility.GetDirectoryDetails(model, model.m_ignoredDirectories, model.m_skippedDirectories);
			model.DirectoryList.Sort(GenericUtility.InvertCompare);

			if (selectedPath != null)
				model.SelectedDirectory = model.DirectoryList.FirstOrDefault(directory => directory.Path == selectedPath);
		}

		private void OnDirectoryLinkInfoChanged(object sender, PropertyChangedEventArgs e)
		{
			UpdateDirectoryLinkEditButtons();
		}

		private void SavePreferences()
		{
			using (Stream stream = File.Create("Preferences.txt"))
			using (XmlWriter xmlWriter = new XmlTextWriter(stream, Encoding.UTF8))
			{
				xmlWriter.WriteStartElement("Preferences");
				
				foreach (DirectoryLink link in StoredDirectoryLinks)
					link.Write(xmlWriter);

				foreach (DirectoryDetails info in m_ignoredDirectories)
					info.Write(xmlWriter, DirectoryStatus.Ignored);

				foreach (DirectoryDetails info in m_skippedDirectories)
					info.Write(xmlWriter, DirectoryStatus.Skipped);

				xmlWriter.WriteEndElement();
			}
		}

		private void LoadPreferences()
		{
			if (!File.Exists("Preferences.txt"))
				return;

			using (Stream stream = File.OpenRead("Preferences.txt"))
			using (XmlReader xmlReader = new XmlTextReader(stream))
			{
				while (xmlReader.Read())
				{
					if (xmlReader.Name == DirectoryLink.DirectoryLinkName)
						StoredDirectoryLinks.Add(new DirectoryLink(xmlReader));

					if (xmlReader.Name == DirectoryDetails.DirectoryDetailsName)
						DirectoryDetails.Read(xmlReader, m_ignoredDirectories, m_skippedDirectories);
				}
			}
		}
		
		private readonly Command m_createJunction;
		private readonly Command m_clearJunction;
		private readonly Command m_refreshList;
		private readonly Command m_skipSelectedDirectory;
		private readonly Command m_ignoreSelectedDirectory;
		private readonly Command m_editDirectoryLink;
		private readonly Command m_saveDirectoryLink;
		private readonly Command m_copyDirectoryLink;
		private readonly Command m_cancelEditDirectoryLink;
		private readonly Command m_deleteDirectoryLink;
		private readonly EditableDirectoryLink m_directoryLinkInEditMode = new EditableDirectoryLink();

		private readonly List<DirectoryDetails> m_ignoredDirectories;
		private readonly List<DirectoryDetails> m_skippedDirectories;
	}
}
