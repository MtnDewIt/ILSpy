using System;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using ICSharpCode.ILSpy.Commands.Bonobo;
using ICSharpCode.ILSpyX.Settings;

namespace ICSharpCode.ILSpy.Options
{
	public class BonoboDumperSettings : ObservableObject, ISettingsSection
	{
		PlatformType platform = PlatformType.Invalid;
		string outputPath = string.Empty;

		public string OutputPath {
			get => outputPath;
			set => SetProperty(ref outputPath, value);
		}

		public PlatformType Platform {
			get => platform;
			set => SetProperty(ref platform, value);
		}

		public XName SectionName => "BonoboDumperSettings";

		public void LoadFromXml(XElement e)
		{
			Platform = PlatformType.Invalid;

			if (Enum.TryParse(e.Attribute(nameof(Platform))?.Value, out PlatformType platform))
			{
				Platform = platform;
			}

			OutputPath = e.Attribute(nameof(OutputPath))?.Value ?? string.Empty;
		}

		public XElement SaveToXml()
		{
			var section = new XElement(SectionName);

			section.SetAttributeValue(nameof(Platform), Platform);
			section.SetAttributeValue(nameof(OutputPath), OutputPath);

			return section;
		}
	}
}
