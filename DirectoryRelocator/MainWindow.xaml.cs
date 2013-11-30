using System.Linq;

namespace DirectoryRelocator
{
		/// <summary>
		/// Interaction logic for MainWindow.xaml
		/// </summary>
		public partial class MainWindow
		{
				public MainWindow()
				{
					InitializeComponent();

					DirectoryRelocator = new DirectoryRelocatorViewModel();

					if (DirectoryRelocator.StoredDirectoryLinks.Count == 0)
					{
						DirectoryRelocator.AddStoredDirectoryLink(new DirectoryLink
						{
							Name = @"Test Folder",
							OriginalPath = @"E:\TestFolder\Start Location",
							BackupPath = @"E:\TestFolder\End Location"
						});
						DirectoryRelocator.AddStoredDirectoryLink(new DirectoryLink
						{
							Name = @"Games",
							OriginalPath = @"C:\Games\Steam\SteamApps\common",
							BackupPath = @"E:\GamesBackup"
						});
					}

					DirectoryRelocator.SelectedDirectoryLink = DirectoryRelocator.StoredDirectoryLinks.FirstOrDefault();

					DirectoryRelocatorView.Content = DirectoryRelocator;
				}

				public DirectoryRelocatorViewModel DirectoryRelocator { get; private set; }
		}
}
