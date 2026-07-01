using System.Diagnostics;
using System.Text;

namespace ILSpy.Bonobo.Tests
{
	public static class BonoboCompiler
	{
		public static async Task<List<string>> BuildAndLogErrorsAsync(string projectPath)
		{
			List<string> errorLogs = [];

			using Process process = new Process 
			{
				StartInfo = new ProcessStartInfo 
				{
					FileName = "C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			process.StartInfo.ArgumentList.Add(projectPath.Replace(".csproj", ".slnx"));
			process.StartInfo.ArgumentList.Add("/restore");
			process.StartInfo.ArgumentList.Add("/p:Configuration=Debug");
			process.StartInfo.ArgumentList.Add("/p:Platform=x64");
			process.StartInfo.ArgumentList.Add("/nologo");
			process.StartInfo.ArgumentList.Add("/v:q");
			process.StartInfo.ArgumentList.Add("/clp:ErrorsOnly");

			process.OutputDataReceived += (sender, e) => 
			{
				if (!string.IsNullOrWhiteSpace(e.Data))
				{
					errorLogs.Add(e.Data);
				}
			};

			process.ErrorDataReceived += (sender, e) => 
			{
				if (!string.IsNullOrWhiteSpace(e.Data))
				{
					errorLogs.Add(e.Data);
				}
			};

			try
			{
				process.Start();

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				await process.WaitForExitAsync();
			}
			catch (Exception ex)
			{
				errorLogs.Add($"Process execution failed: {ex.Message}");
			}

			return errorLogs;
		}

		public static async Task CleanProjectAsync(string projectPath)
		{
			CleanBuildFiles(projectPath);

			using Process process = new Process 
			{
				StartInfo = new ProcessStartInfo 
				{
					FileName = "dotnet",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			process.StartInfo.ArgumentList.Add("clean");
			process.StartInfo.ArgumentList.Add(projectPath);

			try
			{
				process.Start();

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				await process.WaitForExitAsync();
			}
			catch (Exception ex)
			{
				throw new Exception($"Project Cleanup Failed: {ex.Message}");
			}
		}

		public static void CleanBuildFiles(string projectRoot)
		{
			string[] targetFolders =
			[
				"bin",
				"obj",
				".vs",
			];

			try
			{
				var allDirectories = Directory.EnumerateDirectories(projectRoot, "*", SearchOption.AllDirectories).ToList();

				foreach (string dir in allDirectories)
				{
					string folderName = Path.GetFileName(dir);

					if (targetFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
					{
						string dependencyPattern = $"\\Dependencies\\";

						bool isDependency = dir.Contains(dependencyPattern, StringComparison.OrdinalIgnoreCase);

						if (!isDependency)
						{
							if (Directory.Exists(dir))
							{
								Directory.Delete(dir, true);
							}
						}
					}
				}
			}
			catch (Exception)
			{
				return;
			}
		}

		public static void SetupProjectFile(string projectPath, string ekRoot) 
		{
			string tempPath = $"{Path.GetTempPath()}{Path.GetFileName(projectPath)}";

			try
			{
				UTF8Encoding encoding = new UTF8Encoding(false);

				string? line;

				using (StreamReader reader = new StreamReader(projectPath, Encoding.UTF8))
				using (StreamWriter writer = new StreamWriter(tempPath, false, encoding))
				{
					bool hasHitProjectReferences = false;

					while ((line = reader.ReadLine()) != null)
					{
						if (line.Contains("<Reference") && !line.Contains("\\bin\\ManagedBlam.dll"))
						{
							if (!hasHitProjectReferences && (Program.Context?.ExternalRelativePaths.All(x => !line.Contains(x)) ?? false))
							{
								line = line
									.Replace("..\\Dependencies", ekRoot)
									.Replace("<!-- ", string.Empty)
									.Replace(" -->", string.Empty);
							}
						}

						if (line.Contains("<ProjectReference"))
						{
							hasHitProjectReferences = true;

							line = $"    <!-- {line.Replace("    ", string.Empty)} -->";
						}

						writer.WriteLine(line);
					}
				}

				File.Move(tempPath, projectPath, true);
			}
			catch (Exception)
			{
				if (File.Exists(tempPath))
				{
					File.Delete(tempPath);
				}
			}
		}

		public static void CleanupProjectFile(string projectPath, string ekRoot) 
		{
			string tempPath = $"{Path.GetTempPath()}{Path.GetFileName(projectPath)}";

			try
			{
				UTF8Encoding encoding = new UTF8Encoding(false);

				string? line;

				using (StreamReader reader = new StreamReader(projectPath, Encoding.UTF8))
				using (StreamWriter writer = new StreamWriter(tempPath, false, encoding))
				{
					bool hasHitProjectReferences = false;

					while ((line = reader.ReadLine()) != null)
					{
						if (line.Contains("<Reference") && line.Contains(ekRoot))
						{
							if (!hasHitProjectReferences && (Program.Context?.ExternalRelativePaths.All(x => !line.Contains(x)) ?? false))
							{
								line = line
									.Replace(ekRoot, "..\\Dependencies");

								line = $"    <!-- {line.Replace("    ", string.Empty)} -->";
							}
						}

						if (line.Contains("<ProjectReference"))
						{
							hasHitProjectReferences = true;

							line = line
								.Replace("<!-- ", string.Empty)
								.Replace(" -->", string.Empty);
						}

						writer.WriteLine(line);
					}
				}

				File.Move(tempPath, projectPath, true);
			}
			catch (Exception)
			{
				if (File.Exists(tempPath))
				{
					File.Delete(tempPath);
				}
			}
		}
	}
}
