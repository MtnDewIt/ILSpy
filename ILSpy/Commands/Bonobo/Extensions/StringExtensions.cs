using System;
using System.Linq;

public static class StringExtensions
{
	public static string PathToPascalCase(this string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return path;
		}

		char separator = path.Contains('/') ? '/' : '\\';

		string[] segments = path.Split(new[] { '\\', '/' });

		for (int i = 0; i < segments.Length; i++)
		{
			string segment = segments[i];

			if (string.IsNullOrWhiteSpace(segment) || segment.EndsWith(":"))
			{
				continue;
			}

			segments[i] = ToPascalCase(segment);

			// Common Dependency Folder Names
			switch (segments[i])
			{
				case "Tagview":
					segments[i] = "TagView";
					break;
				case "Ps":
					segments[i] = "PS";
					break;
				case "Tagfilelist":
					segments[i] = "TagFileList";
					break;
				case "Toolcommand":
					segments[i] = "ToolCommand";
					break;
			}
		}

		return string.Join(separator.ToString(), segments);
	}

	public static string ToPascalCase(this string original)
	{
		if (string.IsNullOrWhiteSpace(original))
		{
			return original;
		}

		string[] words = original.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries);

		var pascalCaseWords = words.Select(word =>
			char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()
		);

		return string.Join(string.Empty, pascalCaseWords);
	}
}
