using System;
using System.Xml.Linq;

using ICSharpCode.ILSpy.Commands.Bonobo;
using ICSharpCode.ILSpyX.Settings;

using TomsToolbox.Wpf;

namespace ICSharpCode.ILSpy.Options
{
	public class BonoboDumperSettings : ObservableObjectBase, ISettingsSection
	{
		BuildType build = BuildType.Invalid;
		PlatformType platform = PlatformType.Invalid;
		string outputPath = string.Empty;
		string bonoboPath = string.Empty;

		public string OutputPath {
			get => outputPath;
			set => SetProperty(ref outputPath, value);
		}

		public string BonoboPath {
			get => bonoboPath;
			set => SetProperty(ref bonoboPath, value);
		}

		public PlatformType Platform {
			get => platform;
			set => SetProperty(ref platform, value);
		}

		public BuildType Build {
			get => build;
			set => SetProperty(ref build, value);
		}

		public XName SectionName => "BonoboDumperSettings";

		public void LoadFromXml(XElement e)
		{
			Build = BuildType.Invalid;
			Platform = PlatformType.Invalid;

			if (Enum.TryParse((string)e.Attribute(nameof(Build)), out BuildType build))
			{
				Build = build;
			}

			if (Enum.TryParse((string)e.Attribute(nameof(Platform)), out PlatformType platform))
			{
				Platform = platform;
			}

			OutputPath = (string)e.Attribute(nameof(OutputPath)) ?? string.Empty;
			BonoboPath = (string)e.Attribute(nameof(BonoboPath)) ?? string.Empty;
		}

		public XElement SaveToXml()
		{
			var section = new XElement(SectionName);

			section.SetAttributeValue(nameof(Build), Build);
			section.SetAttributeValue(nameof(Platform), Platform);
			section.SetAttributeValue(nameof(OutputPath), OutputPath);
			section.SetAttributeValue(nameof(BonoboPath), BonoboPath);

			return section;
		}
	}
}
