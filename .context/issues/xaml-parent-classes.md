## XAML PARENT CLASS ISSUES

---

So currently there is an issue when exporting certain classes. If the class in question is linked to a xaml file, ie, MyClass.xaml would have a file called MyClass.xaml.cs, for some reason the class does not get export with the correct info.

Below is an example in question. In this example, the class is attached to a xaml file and as such should be marked as a partial class, especially since it also inherits from both a class and an interface. However, the output does not mark it as a partial class and also does not include the base class or interface in the output.

There is also the issue of private and internal variables that are attached to said inherited classes being included in the class. This will cause duplicate variable errors when attempting to compile said class. This is also an issue with the `DebuggerNonUserCode` functions being included in the output, which also causes duplicate function errors when attempting to compile the class, since most of these functions are XAML related and are included in the base class.

```cs
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Bonobo.PluginSystem;
using Bonobo.PluginSystem.Custom;
using Corinth.Tags;

namespace Bonobo.Plugins.TagCustomSection
{
	public class BitmapImportSection : TagCustomSection, IComponentConnector
	{
		private IPluginHost _pluginHost;

		private TagFile _tagFile;

		private TagFieldCustomToolCommand _field;

		private bool _allParametersSupplied;

		private IToolCommandInfo _commandInfo;

		private ToolParameterSet _parameters;

		public static DependencyProperty HasChangedProperty = DependencyProperty.Register("HasChanged", typeof(bool), typeof(BitmapImportSection));

		internal BitmapImportSection userControl;

		private bool _contentLoaded;

		public bool HasChanged
		{
			get
			{
				return (bool)((DependencyObject)this).GetValue(HasChangedProperty);
			}
			set
			{
				((DependencyObject)this).SetValue(HasChangedProperty, (object)value);
			}
		}

		public override bool LockedToTop => true;

		public override string DisplayName => "Bitmap import preview";

		public override bool IsCollapsible => false;

		public BitmapImportSection()
		{
			InitializeComponent();
		}

		public BitmapImportSection(TagFile tagFile, IPluginHost pluginHost)
			: this()
		{
			_pluginHost = pluginHost;
			_tagFile = tagFile;
			SetupField();
		}

		public override void Close()
		{
			((TagCustomSection)this).Close();
			_tagFile = null;
		}

		private void SetupField()
		{
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Expected O, but got Unknown
			_field = _tagFile.SelectFieldType<TagFieldCustomToolCommand>("Custom:show bitmap");
			IToolCommandSource val = _pluginHost.FindSingleInterface<IToolCommandSource>();
			_commandInfo = val.GetToolCommandByName(_field.ToolCommandName);
			if (_commandInfo == null)
			{
				return;
			}
			_parameters = new ToolParameterSet(_commandInfo);
			IEnumerable<IToolCommandParameterInfo> parameters = _commandInfo.GetParameters();
			if (parameters == null)
			{
				return;
			}
			_allParametersSupplied = parameters.Count() == 0;
			if (_field.ArgumentList != null)
			{
				_allParametersSupplied = parameters.Count() == _field.ArgumentList.Count;
				for (int i = 0; i < _field.ArgumentList.Count; i++)
				{
					IToolCommandParameterInfo val2 = parameters.ElementAt(i);
					_parameters.AddApplied(val2, _field.ArgumentList[i]);
				}
			}
		}

		public override void ReceiveTagChanged(TagField tagField)
		{
			((TagCustomSection)this).ReceiveTagChanged(tagField);
			HasChanged = true;
		}

		private void btnImport_Click(object sender, RoutedEventArgs e)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Expected O, but got Unknown
			if (_commandInfo != null)
			{
				base.previewTagAction.Fire(new PreviewTagActionEventArgs(true));
				IToolCommandRunner val = _pluginHost.FindSingleInterface<IToolCommandRunner>();
				Action<bool> action = null;
				val.RunCommand(_commandInfo, _parameters, (IEnumerable<string>)null, !_allParametersSupplied, action);
			}
			else
			{
				MessageBox.Show($"Could not find tool command: {_field.ToolCommandName}");
			}
		}

		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		public void InitializeComponent()
		{
			if (!_contentLoaded)
			{
				_contentLoaded = true;
				Uri resourceLocator = new Uri("/BlamPlugin;component/tagcustomsection/bitmapimportsection.xaml", UriKind.Relative);
				Application.LoadComponent(this, resourceLocator);
			}
		}

		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		void IComponentConnector.Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 1:
				userControl = (BitmapImportSection)target;
				break;
			case 2:
				((Button)target).Click += btnImport_Click;
				break;
			default:
				_contentLoaded = true;
				break;
			}
		}
	}
}

```

This is the currect functional output for the class in question. 

```cs
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Bonobo.PluginSystem;
using Bonobo.PluginSystem.Custom;
using Corinth.Tags;

namespace Bonobo.Plugins.TagCustomSection
{
	public partial class BitmapImportSection : Bonobo.PluginSystem.Custom.TagCustomSection, IComponentConnector
	{
		private IPluginHost _pluginHost;

		private TagFile _tagFile;

		private TagFieldCustomToolCommand _field;

		private bool _allParametersSupplied;

		private IToolCommandInfo _commandInfo;

		private ToolParameterSet _parameters;

		public static DependencyProperty HasChangedProperty = DependencyProperty.Register("HasChanged", typeof(bool), typeof(BitmapImportSection));

		public bool HasChanged
		{
			get
			{
				return (bool)((DependencyObject)this).GetValue(HasChangedProperty);
			}
			set
			{
				((DependencyObject)this).SetValue(HasChangedProperty, (object)value);
			}
		}

		public override bool LockedToTop => true;

		public override string DisplayName => "Bitmap import preview";

		public override bool IsCollapsible => false;

		public BitmapImportSection()
		{
			InitializeComponent();
		}

		public BitmapImportSection(TagFile tagFile, IPluginHost pluginHost)
			: this()
		{
			_pluginHost = pluginHost;
			_tagFile = tagFile;
			SetupField();
		}

		public override void Close()
		{
			base.Close();
			_tagFile = null;
		}

		private void SetupField()
		{
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Expected O, but got Unknown
			_field = _tagFile.SelectFieldType<TagFieldCustomToolCommand>("Custom:show bitmap");
			IToolCommandSource val = _pluginHost.FindSingleInterface<IToolCommandSource>();
			_commandInfo = val.GetToolCommandByName(_field.ToolCommandName);
			if (_commandInfo == null)
			{
				return;
			}
			_parameters = new ToolParameterSet(_commandInfo);
			IEnumerable<IToolCommandParameterInfo> parameters = _commandInfo.GetParameters();
			if (parameters == null)
			{
				return;
			}
			_allParametersSupplied = parameters.Count() == 0;
			if (_field.ArgumentList != null)
			{
				_allParametersSupplied = parameters.Count() == _field.ArgumentList.Count;
				for (int i = 0; i < _field.ArgumentList.Count; i++)
				{
					IToolCommandParameterInfo val2 = parameters.ElementAt(i);
					_parameters.AddApplied(val2, _field.ArgumentList[i]);
				}
			}
		}

		public override void ReceiveTagChanged(TagField tagField)
		{
			base.ReceiveTagChanged(tagField);
			HasChanged = true;
		}

		private void btnImport_Click(object sender, RoutedEventArgs e)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Expected O, but got Unknown
			if (_commandInfo != null)
			{
				base.previewTagAction.Fire(new PreviewTagActionEventArgs(true));
				IToolCommandRunner val = _pluginHost.FindSingleInterface<IToolCommandRunner>();
				Action<bool> action = null;
				val.RunCommand(_commandInfo, _parameters, (IEnumerable<string>)null, !_allParametersSupplied, action);
			}
			else
			{
				MessageBox.Show($"Could not find tool command: {_field.ToolCommandName}");
			}
		}
	}
}

```
