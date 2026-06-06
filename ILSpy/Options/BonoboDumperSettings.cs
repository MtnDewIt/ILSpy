using System;
using System.Xml.Linq;

using ICSharpCode.ILSpy.Commands.Bonobo;
using ICSharpCode.ILSpyX.Settings;

using TomsToolbox.Wpf;

namespace ICSharpCode.ILSpy.Options
{
	public class BonoboDumperSettings : ObservableObjectBase, ISettingsSection
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

			if (Enum.TryParse((string)e.Attribute(nameof(Platform)), out PlatformType platform))
			{
				Platform = platform;
			}

			OutputPath = (string)e.Attribute(nameof(OutputPath)) ?? string.Empty;
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
