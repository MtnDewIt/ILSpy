using System;
using System.IO;

namespace ICSharpCode.ILSpy.Commands.Bonobo.Extensions
{
	public static class DirectoryHelper
	{
		public static void CopyDirectory(string sourceDir, string destinationDir, Func<FileInfo, bool>? fileFilter = null) 
		{
			var directoryInfo = new DirectoryInfo(sourceDir);

			if (!directoryInfo.Exists)
			{
				throw new DirectoryNotFoundException($"Source directory not found: {directoryInfo.FullName}");
			}

			DirectoryInfo[] dirs = directoryInfo.GetDirectories();

			Directory.CreateDirectory(destinationDir);

			foreach (FileInfo file in directoryInfo.GetFiles())
			{
				if (fileFilter == null || fileFilter(file))
				{
					string targetFilePath = Path.Combine(destinationDir, file.Name);

					file.CopyTo(targetFilePath, true);
				}
			}

			foreach (DirectoryInfo subDir in dirs)
			{
				string newDestinationDir = Path.Combine(destinationDir, subDir.Name);

				CopyDirectory(subDir.FullName, newDestinationDir, fileFilter);
			}
		}

		public static void MoveFiles(string sourceDirName, string destDirName)
		{
			if (Directory.Exists(destDirName))
			{
				foreach (string dirPath in Directory.GetDirectories(sourceDirName, "*", SearchOption.AllDirectories))
				{
					Directory.CreateDirectory(dirPath.Replace(sourceDirName, destDirName));
				}

				foreach (string filePath in Directory.GetFiles(sourceDirName, "*.*", SearchOption.AllDirectories))
				{
					string destFile = filePath.Replace(sourceDirName, destDirName);
					File.Copy(filePath, destFile, overwrite: true);
				}

				Directory.Delete(sourceDirName, recursive: true);
			}
			else
			{
				Directory.Move(sourceDirName, destDirName);
			}
		}

		public static void Rename(string sourceDirName, string newName)
		{
			string destinationPath = Path.Combine(Path.GetDirectoryName(sourceDirName)!, newName);

			if (string.Equals(sourceDirName, destinationPath, StringComparison.OrdinalIgnoreCase))
			{
				string tempPath = Path.Combine(Path.GetDirectoryName(sourceDirName)!, Guid.NewGuid().ToString());
				Directory.Move(sourceDirName, tempPath);
				Directory.Move(tempPath, destinationPath);
			}
			else
			{
				Directory.Move(sourceDirName, destinationPath);
			}
		}
	}
}
