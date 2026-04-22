using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;

using TomsToolbox.Wpf.Composition.AttributedModel;

namespace ICSharpCode.ILSpy.Options
{
	/// <summary>
	/// Interaction logic for BonoboDumperSettingsPanel.xaml
	/// </summary>
	[DataTemplate(typeof(BonoboDumperSettingsViewModel))]
	[NonShared]
	public partial class BonoboDumperSettingsPanel
	{
		public BonoboDumperSettingsPanel()
		{
			InitializeComponent();
		}
	}
}
