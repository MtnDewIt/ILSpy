using global::Avalonia.Controls;
using global::Avalonia.Markup.Xaml;

namespace ICSharpCode.ILSpy.Options.Panels
{
	public partial class BonoboDumperSettingsPanel : UserControl
	{
		public BonoboDumperSettingsPanel()
		{
			InitializeComponent();
		}

		void InitializeComponent() => AvaloniaXamlLoader.Load(this);
	}
}
