using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Win32;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
	public class RegistryHandler
	{
		private const string muiCache = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";

		private static readonly string[] baseTargets =
		[
			"sapien.exe",
			"tool.exe",
		];

		private static readonly Dictionary<string, BuildType> tagTestTargets = new()
		{
			{ "halo_tag_test.exe",    BuildType.Blam },
			{ "halo2_tag_test.exe",   BuildType.Prophets },
			{ "halo3_tag_test.exe",   BuildType.Forerunner },
			{ "atlas_tag_test.exe",   BuildType.Atlas },
			{ "reach_tag_test.exe",   BuildType.Omaha },
			{ "halo4_tag_test.exe",   BuildType.Midnight },
			{ "halo2a_tag_test.exe",  BuildType.Groundhog }
		};

		public static Dictionary<BuildType, string> FindEKPaths()
		{
			using (RegistryKey muiCacheKeys = Registry.CurrentUser.OpenSubKey(muiCache, false))
			{
				List<string> filePaths = [.. muiCacheKeys.GetValueNames()
					.Where(path => baseTargets.Any(target => path.Contains(target, StringComparison.OrdinalIgnoreCase) || tagTestTargets.Any(target => path.Contains(target.Key, StringComparison.OrdinalIgnoreCase))))
					.Select(path => path.Replace(".FriendlyAppName", string.Empty))
					.Select(path => path.Replace(".ApplicationCompany", string.Empty))
					.Distinct(StringComparer.OrdinalIgnoreCase)];

				HashSet<string> targetFiles = baseTargets
					.Concat(tagTestTargets.Keys)
					.ToHashSet(StringComparer.OrdinalIgnoreCase);

				Dictionary<BuildType, string> results = [];

				foreach (string filePath in filePaths)
				{
					if (string.IsNullOrWhiteSpace(filePath))
					{
						continue;
					}

					string fileName = Path.GetFileName(filePath);

					if (targetFiles.Contains(fileName))
					{
						string path = Path.GetDirectoryName(filePath);

						// Dumb fix until we can parse individual files.
						if (path != null && !path.Contains("pooka", StringComparison.OrdinalIgnoreCase))
						{
							BuildType buildType = GetBuildType(path);

							if ((baseTargets.All(x => File.Exists(Path.Combine(path, x))) || 
								tagTestTargets.Any(x => File.Exists(Path.Combine(path, x.Key)))) &&
								!results.ContainsKey(buildType))
							{
								results.Add(buildType, path);
							}
						}
					}
				}

				return results;
			}
		}

		private static BuildType GetBuildType(string path) 
		{
			foreach (var (fileName, buildType) in tagTestTargets)
			{
				if (Path.Exists(Path.Combine(path, fileName)))
				{
					return buildType;
				}
			}

			return BuildType.Invalid;
		}
	}
}
