using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace DirectoryRelocator.Utility
{
	public class DirectoryUtility
	{
		public static DirectoryStatus CreateJunction(string originalPath)
		{
			string backupLocation = GetBackupPath(originalPath);
			
			// TODO: This should allow for the case where a backup is available (in that case it
			// should copy all changed files to the new location before creating the junction)
			if (Directory.Exists(backupLocation))
				throw new InvalidOperationException(string.Format("Path already exists: {0}", backupLocation));

			if (!Directory.Exists(originalPath))
				throw new DirectoryNotFoundException(originalPath);

			if (originalPath.StartsWith(backupLocation.Substring(0, 3)))
			{
				// Move if the path is on the same drive
				Directory.Move(originalPath, backupLocation);
			}
			else
			{
				// Otherwise we have to copy and delete
				CopyDirectory(originalPath, backupLocation);
				Directory.Delete(originalPath, true);
			}

			// Create the junction (Ideally this would use 
			ProcessStartInfo process = new ProcessStartInfo("cmd.exe", "/c mklink " + string.Format("/J \"{0}\" \"{1}\"", originalPath, backupLocation))
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				WindowStyle = ProcessWindowStyle.Hidden
			};
			Process.Start(process);

			Thread.Sleep(500);

			return GetDirectoryStatus(originalPath);
		}

		private static void CopyDirectory(string originalPath, string backupLocation)
		{
			Directory.CreateDirectory(backupLocation);

			DirectoryInfo directory = new DirectoryInfo(originalPath);

			foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
				CopyDirectory(subDirectory.FullName, Path.Combine(backupLocation, subDirectory.Name));

			foreach (FileInfo file in directory.EnumerateFiles())
				file.CopyTo(Path.Combine(backupLocation, file.Name));
		}

		public static DirectoryStatus RemoveJunction(string junctionPath)
		{
			DirectoryInfo linkDirectory = new DirectoryInfo(junctionPath);
			DirectoryInfo actualDirectory = new DirectoryInfo(GetBackupPath(junctionPath));

			if (linkDirectory.Attributes.HasFlag(FileAttributes.ReparsePoint) && FoldersActuallyMatch(linkDirectory, actualDirectory, true))
			{
				// Delete the junction directory
				linkDirectory.Delete(true);

				if (linkDirectory.FullName.StartsWith(actualDirectory.FullName.Substring(0, 3)))
				{
					actualDirectory.MoveTo(junctionPath);
				}
				else
				{
					CopyDirectory(actualDirectory.FullName, junctionPath);
					actualDirectory.Delete(true);
				}
			}

			return GetDirectoryStatus(junctionPath);
		}

		public DirectoryStatus RemoveBackup(string originalPath)
		{
			// Verify that the original path is not a junction
			if (!new DirectoryInfo(originalPath).Attributes.HasFlag(FileAttributes.ReparsePoint))
				Directory.Delete(GetBackupPath(originalPath), true);

			return GetDirectoryStatus(originalPath);
		}

		public static DirectoryStatus GetDirectoryStatus(string path)
		{
			// Is this a directory junction?
			if (new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint))
				return DirectoryStatus.JunctionAvailable;

			if (Directory.Exists(GetBackupPath(path)))
				return DirectoryStatus.BackupAvailable;

			return DirectoryStatus.StandardDirectory;
		}

		public static long GetDirectorySize(DirectoryInfo directory)
		{
			if (!directory.Exists)
				throw new DirectoryNotFoundException();
			
			long size = 0;

			List<long> directorySizes = directory.GetDirectories().Select(GetDirectorySize).ToList();
			if (directorySizes.Count != 0)
				size = directorySizes.Aggregate((first, second) => first + second);

			List<long> fileSizes = directory.EnumerateFiles().Select(file => file.Length).ToList();
			if (fileSizes.Count != 0)
				size += fileSizes.Aggregate((first, second) => first + second);
			
			return size;
		}
		
		public static List<DirectoryDetails> GetDirectoryDetails(DirectoryRelocatorViewModel viewModel, List<DirectoryDetails> ignoredDirectories, List<DirectoryDetails> skippedDirectories)
		{
			if (!Directory.Exists(viewModel.SelectedDirectoryLink.OriginalPath))
				return new List<DirectoryDetails>();

			return GetDirectoryDetails(viewModel.SelectedDirectoryLink.OriginalPath, ignoredDirectories, skippedDirectories).ToList();
		}

		private static IEnumerable<DirectoryDetails> GetDirectoryDetails(string path, IReadOnlyCollection<DirectoryDetails> ignoredDirectories, IReadOnlyCollection<DirectoryDetails> skippedDirectories)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(path);

			IEnumerable<DirectoryDetails> directories = directoryInfo.EnumerateDirectories().Select(directory => new DirectoryDetails(directory.FullName))
				.Where(directory => !ignoredDirectories.Contains(directory))
				.SelectMany(directory => skippedDirectories.Contains(directory) ? GetDirectoryDetails(directory.Path, ignoredDirectories, skippedDirectories) : new[] { directory });

			return directories;
		}

		public static string GetBackupPath(string originalPath)
		{
			MainWindow mainWindow = (Application.Current.MainWindow as MainWindow);

			if (mainWindow == null)
				throw new Exception("No main window?");

			string rootOriginalPath = mainWindow.DirectoryRelocator.SelectedDirectoryLink.OriginalPath;
			string rootBackupPath = mainWindow.DirectoryRelocator.SelectedDirectoryLink.BackupPath;

			string newPathPart = originalPath.Substring(rootOriginalPath.Length + 1);

			return Path.Combine(rootBackupPath, newPathPart);
		}

		private static bool FoldersActuallyMatch(DirectoryInfo left, DirectoryInfo right, bool isRoot)
		{
			if (!isRoot && left.Name != right.Name)
				return false;

			List<DirectoryInfo> leftSubFolders = new List<DirectoryInfo>(left.GetDirectories());
			List<DirectoryInfo> rightSubFolders = new List<DirectoryInfo>(right.GetDirectories());

			if (leftSubFolders.Count != rightSubFolders.Count)
				return false;
				
			if (leftSubFolders.Where((t, i) => !FoldersActuallyMatch(t, rightSubFolders[i], false)).Any())
				return false;

			List<FileInfo> leftFiles = new List<FileInfo>(left.GetFiles());
			List<FileInfo> rightFiles = new List<FileInfo>(right.GetFiles());

			if (leftFiles.Count != rightFiles.Count)
				return false;

			if (leftFiles.Count == 0)
				return true;

			return leftFiles.Where((t, i) => !t.Equals(rightFiles[i])).Any();
		}
	}
}
