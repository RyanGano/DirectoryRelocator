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
			m_editDirectoryLink = new Command(EditDirectoryLink, CanEditDirectoryLink);
			m_saveDirectoryLink = new Command(SaveDirectoryLink, CanSaveDirectoryLink);
			m_copyDirectoryLink = new Command(CopyDirectoryLink, CanCopyDirectoryLink);
			m_cancelEditDirectoryLink = new Command(CancelEditDirectoryLink, CanCancelEditDirectoryLink);
			m_deleteDirectoryLink = new Command(DeleteDirectoryLink, CanDeleteDirectoryLink);

			LoadDirectoryLinks();
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
		public Command EditDirectoryLinkCommand { get { return m_editDirectoryLink; } }
		public Command SaveDirectoryLinkCommand { get { return m_saveDirectoryLink; } }
		public Command CopyDirectoryLinkCommand { get { return m_copyDirectoryLink; } }
		public Command CancelEditDirectoryLinkCommand { get { return m_cancelEditDirectoryLink; } }
		public Command DeleteDirectoryLinkCommand { get { return m_deleteDirectoryLink; } }

		private static void OnSelectedDirectoryChanged(DependencyObject caller, DependencyPropertyChangedEventArgs eventArgs)
		{
			if (!(caller is DirectoryRelocatorViewModel))
				throw new ArgumentException("DependencyObject should be a DirectoryRelocatorViewModel");

			((DirectoryRelocatorViewModel)caller).CreateJunctionCommand.RaiseCanExecuteChanged();
			((DirectoryRelocatorViewModel)caller).ClearJunctionCommand.RaiseCanExecuteChanged();
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

			SaveDirectoryLinks();
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
			SelectedDirectoryLink = StoredDirectoryLinks.FirstOrDefault();
			StoredDirectoryLinks.Remove(selected);

			SaveDirectoryLinks();
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

			model.DirectoryList = DirectoryUtility.GetDirectoryDetails(model);

			if (selectedPath != null)
				model.SelectedDirectory = model.DirectoryList.FirstOrDefault(directory => directory.Path == selectedPath);
		}

		private void OnDirectoryLinkInfoChanged(object sender, PropertyChangedEventArgs e)
		{
			UpdateDirectoryLinkEditButtons();
		}

		private void SaveDirectoryLinks()
		{
			using (Stream stream = File.Create("DirectoryLinks.txt"))
			using (XmlWriter xmlWriter = new XmlTextWriter(stream, Encoding.UTF8))
			{
				xmlWriter.WriteStartElement("DirectoryLinks");

				foreach (DirectoryLink link in StoredDirectoryLinks)
					link.Write(xmlWriter);

				xmlWriter.WriteEndElement();
			}
		}

		private void LoadDirectoryLinks()
		{
			if (!File.Exists("DirectoryLinks.txt"))
				return;

			using (Stream stream = File.OpenRead("DirectoryLinks.txt"))
			using (XmlReader xmlReader = new XmlTextReader(stream))
			{
				while (xmlReader.Read())
					if (xmlReader.Name == DirectoryLink.DirectoryLinkName)
					{
						StoredDirectoryLinks.Add(new DirectoryLink(xmlReader));
					}
			}
		}

		private readonly Command m_createJunction;
		private readonly Command m_clearJunction;
		private readonly Command m_editDirectoryLink;
		private readonly Command m_saveDirectoryLink;
		private readonly Command m_copyDirectoryLink;
		private readonly Command m_cancelEditDirectoryLink;
		private readonly Command m_deleteDirectoryLink;
		private readonly EditableDirectoryLink m_directoryLinkInEditMode = new EditableDirectoryLink();
	}
}
