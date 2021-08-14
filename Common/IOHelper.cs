namespace Ecng.Common
{
	using System;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using System.Threading;
	using System.Linq;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.ComponentModel;

	public static class IOHelper
	{
		public static DirectoryInfo ClearDirectory(string releasePath, Func<string, bool> filter = null)
		{
			var releaseDir = new DirectoryInfo(releasePath);

			foreach (var file in releaseDir.GetFiles())
			{
				if (filter != null && !filter(file.FullName))
					continue;

				file.Delete();
			}

			foreach (var dir in releaseDir.GetDirectories())
				dir.Delete(true);

			return releaseDir;
		}

		public static void CopyDirectory(string sourcePath, string destPath)
		{
			Directory.CreateDirectory(destPath);

			foreach (var fileName in Directory.GetFiles(sourcePath))
			{
				CopyAndMakeWritable(fileName, destPath);
			}

			foreach (var directory in Directory.GetDirectories(sourcePath))
			{
				CopyDirectory(directory, Path.Combine(destPath, Path.GetFileName(directory)));
			}
		}

		public static string CopyAndMakeWritable(string fileName, string destPath)
		{
			var destFile = Path.Combine(destPath, Path.GetFileName(fileName));

			File.Copy(fileName, destFile, true);
			new FileInfo(destFile).IsReadOnly = false;

			return destFile;
		}

		public static string ToFullPath(this string path)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));

			return Path.GetFullPath(path);
		}

		public static string AddRelative(this string path, string relativePart)
		{
			return (path + relativePart).ToFullPath();
		}

		private static void ReadProcessOutput(TextReader reader, Action<string> action, object actionSync)
		{
			do
			{
				var str = reader.ReadLine();
				if (str is null)
					break;

				if (!str.IsEmptyOrWhiteSpace())
				{
					lock(actionSync)
						action(str);
				}
			} while (true);
		}

		public static int Execute(string fileName, string arg, Action<string> output, Action<string> error, Action<ProcessStartInfo> infoHandler = null, TimeSpan waitForExit = default, string stdInput = null)
		{
			if (output is null)
				throw new ArgumentNullException(nameof(output));

			if (error is null)
				throw new ArgumentNullException(nameof(error));

			var input = !stdInput.IsEmpty();

			var procInfo = new ProcessStartInfo(fileName, arg)
			{
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = input,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};

			infoHandler?.Invoke(procInfo);

			using var process = new Process
			{
				EnableRaisingEvents = true,
				StartInfo = procInfo
			};

			process.Start();
			var locker = new object();

			if (input)
			{
				process.StandardInput.WriteLine(stdInput);
				process.StandardInput.Close();
			}

			// ReSharper disable once AccessToDisposedClosure
			var outputTask = Task.Run(() => ReadProcessOutput(process.StandardOutput, output, locker));

			// ReSharper disable once AccessToDisposedClosure
			var errorTask = Task.Run(() => ReadProcessOutput(process.StandardError, error, locker));

			outputTask.Wait();
			errorTask.Wait();
			process.WaitForExit(waitForExit == default ? int.MaxValue : (int) waitForExit.TotalMilliseconds);

			return process.ExitCode;
		}

		public static bool CreateDirIfNotExists(this string fullPath)
		{
			var directory = Path.GetDirectoryName(fullPath);

			if (directory.IsEmpty() || Directory.Exists(directory))
				return false;

			Directory.CreateDirectory(directory);
			return true;
		}

		private static readonly string[] _suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB

		public static string ToHumanReadableFileSize(this long byteCount)
		{
			int place;
			int num;

			if (byteCount == 0)
			{
				num = 0;
				place = 0;
			}
			else
			{
				var bytes = byteCount.Abs();
				place = (int)Math.Log(bytes, 1024).Floor();
				num = (int)(Math.Sign(byteCount) * Math.Round(bytes / Math.Pow(1024, place), 1));
			}

			return num + " " + _suf[place];
		}

		public static void SafeDeleteDir(this string path)
		{
			if (!Directory.Exists(path))
				return;

			Directory.Delete(path, true);
		}

		public static string CreateTempDir()
		{
			var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Remove("-"));

			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			return path;
		}

		public static bool CheckInstallation(string path)
		{
			if (path.IsEmpty())
				return false;

			if (!Directory.Exists(path))
				return false;

			var files = Directory.GetFiles(path);
			var directories = Directory.GetDirectories(path);
			return files.Any() || directories.Any();
		}

		public static string GetRelativePath(this string fileFull, string folder)
		{
			var pathUri = new Uri(fileFull);

			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
				folder += Path.DirectorySeparatorChar;

			var folderUri = new Uri(folder);
			return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}

		public static long GetDiskFreeSpace(string driveName)
		{
			return new DriveInfo(driveName).TotalFreeSpace;
		}

		public static void CreateFile(string rootPath, string relativePath, string fileName, byte[] content)
		{
			if (relativePath.IsEmpty())
			{
				File.WriteAllBytes(Path.Combine(rootPath, fileName), content);
			}
			else
			{
				var fullPath = Path.Combine(rootPath, relativePath, fileName);
				var fileInfo = new FileInfo(fullPath);
				fileInfo.Directory.Create();
				File.WriteAllBytes(fullPath, content);
			}
		}

		// https://stackoverflow.com/a/2811746/8029915
		public static void DeleteEmptyDirs(string dir)
		{
			if (dir.IsEmpty())
				throw new ArgumentNullException(nameof(dir));

			try
			{
				foreach (var d in Directory.EnumerateDirectories(dir))
				{
					DeleteEmptyDirs(d);
				}

				var entries = Directory.EnumerateFileSystemEntries(dir);

				if (!entries.Any())
				{
					try
					{
						Directory.Delete(dir);
					}
					catch (UnauthorizedAccessException) { }
					catch (DirectoryNotFoundException) { }
				}
			}
			catch (UnauthorizedAccessException) { }
		}

		public static string ToFullPathIfNeed(this string path)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));

			return path.ReplaceIgnoreCase("%Documents%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
		}

		// http://social.msdn.microsoft.com/Forums/eu/windowssearch/thread/55582d9d-77ea-47d9-91ce-cff7ca7ef528
		public static bool BlockDeleteDir(string dir, bool isRecursive = false, int iterCount = 1000, int sleep = 0)
		{
			if (isRecursive)
			{
				// https://stackoverflow.com/a/329502/8029915

				var files = Directory.GetFiles(dir);
				var dirs = Directory.GetDirectories(dir);

				foreach (var file in files)
				{
					File.SetAttributes(file, FileAttributes.Normal);
					File.Delete(file);
				}

				foreach (var sub in dirs)
				{
					BlockDeleteDir(sub, true, iterCount, sleep);
				}
			}

			// https://stackoverflow.com/a/1703799/8029915

			try
			{
				Directory.Delete(dir, false);
			}
			catch (IOException)
			{
				Directory.Delete(dir, false);
			}
			catch (UnauthorizedAccessException)
			{
				Directory.Delete(dir, false);
			}

			var limit = iterCount;

			while (Directory.Exists(dir) && limit-- > 0)
				Thread.Sleep(sleep);

			return Directory.Exists(dir);
		}

		public static bool OpenLink(this string url, bool raiseError)
		{
			if (url.IsEmpty())
				throw new ArgumentNullException(nameof(url));

			// https://stackoverflow.com/a/21836079

			try
			{
				// https://github.com/dotnet/wpf/issues/2566

				var procInfo = new ProcessStartInfo(url)
				{
					UseShellExecute = true,
				};

				Process.Start(procInfo);
				return true;
			}
			catch (Win32Exception)
			{
				try
				{
					var launcher = url.StartsWithIgnoreCase("http") ? "IExplore.exe" : "explorer.exe";
					Process.Start(launcher, url);
					return true;
				}
				catch
				{
					if (raiseError)
						throw;

					return false;
				}
			}
		}

		public static IEnumerable<string> GetDirectories(string path,
			string searchPattern = "*",
			SearchOption searchOption = SearchOption.TopDirectoryOnly)
		{
			return !Directory.Exists(path)
				? Enumerable.Empty<string>()
				: Directory.EnumerateDirectories(path, searchPattern, searchOption);
		}

		public static DateTime GetTimestamp(this Assembly assembly)
		{
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));

			return GetTimestamp(assembly.Location);
		}

		public static DateTime GetTimestamp(string filePath)
		{
			var b = new byte[2048];

			using (var s = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				s.Read(b, 0, b.Length);

			const int peHeaderOffset = 60;
			const int linkerTimestampOffset = 8;
			var i = BitConverter.ToInt32(b, peHeaderOffset);
			var secondsSince1970 = (long)BitConverter.ToInt32(b, i + linkerTimestampOffset);

			return secondsSince1970.FromUnix().ToLocalTime();
		}

		public static bool IsDirectory(this string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);
	}
}