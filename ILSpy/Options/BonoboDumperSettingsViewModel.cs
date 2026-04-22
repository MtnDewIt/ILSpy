using System;
using System.Composition;
using System.Linq;
using System.Windows.Input;
using System.Xml.Linq;

using ICSharpCode.ILSpy.Commands.Bonobo;

using Microsoft.Win32;

using TomsToolbox.Wpf;

namespace ICSharpCode.ILSpy.Options
{
	[ExportOptionPage(Order = 50)]
	[NonShared]
	public class BonoboDumperSettingsViewModel : ObservableObjectBase, IOptionPage
	{
		private BonoboDumperSettings settings;
		public BonoboDumperSettings Settings 
		{
			get => settings;
			set => SetProperty(ref settings, value);
		}

		public ICommand OutputCommand => new Commands.DelegateCommand(Output);
		private void Output()
		{
			var dlg = new OpenFolderDialog() 
			{
				Title = "Select Dump Output Path",
			};

			if (!string.IsNullOrEmpty(settings.OutputPath))
			{
				dlg.InitialDirectory = settings.OutputPath;
			}

			if (dlg.ShowDialog() == true)
			{
				settings.OutputPath = dlg.FolderName;
			}
		}

		public ICommand BonoboCommand => new Commands.DelegateCommand(Bonobo);
		private void Bonobo()
		{
			var dlg = new OpenFolderDialog()
			{
				Title = "Select Editing Kit Path",
			};

			if (!string.IsNullOrEmpty(settings.BonoboPath))
			{
				dlg.InitialDirectory = settings.BonoboPath;
			}

			if (dlg.ShowDialog() == true)
			{
				settings.BonoboPath = dlg.FolderName;
			}
		}

		public BuildType[] Builds => [.. Enum.GetValues<BuildType>().Where(x => x != BuildType.Invalid)];
		public PlatformType[] Platforms => [.. Enum.GetValues<PlatformType>().Where(x => x != PlatformType.Invalid)];

		public string Title => Properties.Resources.Bonobo_Dumper;

		public void Load(SettingsSnapshot settings)
		{
			Settings = settings.GetSettings<BonoboDumperSettings>();
		}

		public void LoadDefaults()
		{
			Settings.LoadFromXml(new XElement("dummy"));
		}
	}
}
