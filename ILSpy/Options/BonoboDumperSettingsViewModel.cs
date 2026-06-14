using System;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ICSharpCode.ILSpy.Commands;
using ICSharpCode.ILSpy.Commands.Bonobo;

namespace ICSharpCode.ILSpy.Options
{
	[ExportOptionPage(Order = 50)]
	[NonShared]
	public partial class BonoboDumperSettingsViewModel : ObservableObject, IOptionPage
	{
		public string Title => Properties.Resources.Bonobo_Dumper;

		[ObservableProperty]
		BonoboDumperSettings settings = null!;

		public PlatformType[] Platforms => [.. Enum.GetValues<PlatformType>().Where(x => x != PlatformType.Invalid)];

		public void Load(SettingsService service)
		{
			Settings = service.BonoboSettings;
		}

		public void LoadDefaults()
		{
			Settings.LoadFromXml(new XElement("dummy"));
		}

		[RelayCommand]
		async Task OutputAsync()
		{
			var folder = await FilePickers.PickFolderAsync("Select Output Folder");
			if (folder != null)
			{
				Settings.OutputPath = folder;
			}
		}
	}
}
