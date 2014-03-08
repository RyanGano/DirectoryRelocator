using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using DirectoryRelocator.Utility;

namespace DirectoryRelocator
{
	public class DirectoryRelocatorViewModel : DependencyObject
	{
		public DirectoryRelocatorViewModel()
		{
			m_refreshList = new Command(RefreshList, CanRefreshList);
			m_editDirectoryLink = new Command(EditDirectoryLink, CanEditDirectoryLink);
			m_saveDirectoryLink = new Command(SaveDirectoryLink, CanSaveDirectoryLink);
			m_copyDirectoryLink = new Command(CopyDirectoryLink, CanCopyDirectoryLink);
			m_cancelEditDirectoryLink = new Command(CancelEditDirectoryLink, CanCancelEditDirectoryLink);
			m_deleteDirectoryLink = new Command(DeleteDirectoryLink, CanDeleteDirectoryLink);

			m_ignoredDirectories = new List<DirectoryDetails>();
			m_skippedDirectories = new List<DirectoryDetails>();

			DirectoryList = new List<DirectoryDetails>();

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

		public static readonly DependencyProperty IsWorkingProperty = DependencyProperty.Register(
			"IsWorking", typeof (bool), typeof (DirectoryRelocatorViewModel), new PropertyMetadata(default(bool)));

		public bool IsWorking
		{
			get { return (bool) GetValue(IsWorkingProperty); }
			set { SetValue(IsWorkingProperty, value); }
		}
		
		public Command RefreshListCommand { get { return m_refreshList; } }
		public Command EditDirectoryLinkCommand { get { return m_editDirectoryLink; } }
		public Command SaveDirectoryLinkCommand { get { return m_saveDirectoryLink; } }
		public Command CopyDirectoryLinkCommand { get { return m_copyDirectoryLink; } }
		public Command CancelEditDirectoryLinkCommand { get { return m_cancelEditDirectoryLink; } }
		public Command DeleteDirectoryLinkCommand { get { return m_deleteDirectoryLink; } }

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
			CancelEditDirectoryLinkCommand.RaiseCanExecuteChanged();
			DeleteDirectoryLinkCommand.RaiseCanExecuteChanged();
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
			foreach (var details in model.DirectoryList)
				details.PropertyChanged -= model.OnDirectoryDetailsChanged;
			
			model.DirectoryList = DirectoryUtility.GetDirectoryDetails(model, model.m_ignoredDirectories, model.m_skippedDirectories);
			model.DirectoryList.Sort(GenericUtility.InvertCompare);

			foreach (var details in model.DirectoryList)
				details.PropertyChanged += model.OnDirectoryDetailsChanged;

			model.IsWorking = true;

			List<DirectoryDetails> directories = model.DirectoryList;

			Task.Run(() => SortDirectoriesWhenLoaded(directories))
				.ContinueWith(task => model.Dispatcher.BeginInvoke(new Action(() =>
				{
					model.DirectoryList = null;
					model.DirectoryList = task.Result;
					model.IsWorking = false;
				})));
		}

		private static List<DirectoryDetails> SortDirectoriesWhenLoaded(List<DirectoryDetails> directories)
		{
			if (directories.Count == 0)
				return directories;

			Dispatcher dispatcher = directories.First().Dispatcher;
			bool keepWorking = true;

			while (keepWorking)
			{
				keepWorking = dispatcher.Invoke(() =>
				{
					directories.Sort(GenericUtility.InvertCompare);
					return directories.Any(item => item.IsWorking);
				});

				Thread.Sleep(500);
			}

			return directories;
		}

		private void OnDirectoryDetailsChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == DirectoryDetails.DirectoryStatusProperty.Name)
			{
				DirectoryDetails details = (DirectoryDetails) sender;

				if (details.DirectoryStatus == DirectoryStatus.Skipped)
					m_skippedDirectories.Add(details);
				else if (details.DirectoryStatus == DirectoryStatus.Ignored)
					m_ignoredDirectories.Add(details);

				UpdateDirectoryList(this);

				SavePreferences();
			}
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
		
		private readonly Command m_refreshList;
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
