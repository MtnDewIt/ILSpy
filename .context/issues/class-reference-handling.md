## INCORRECT CLASS REFERENCE HANDLING

In the example class defined below, have have a class that inherits from an interface. However, the class includes variables which make use of a class with the same name as the abstract class. This will cause incorrect class references, and maybe some errors when trying to compile the class itself.

```cs
using Bonobo.PluginSystem.Custom;
using Corinth.Game.Tools;

namespace Bonobo.Plugins.ToolCommandSource
{
	internal abstract class ToolCommandParameterInfo : IToolCommandParameterInfo
	{
		private ToolCommandParameterInfo info;

		public ToolCommandParameterInfo Info => info;

		public string Name => info.Name;

		public string Explanation => info.Explanation;

		public ToolCommandParameterInfo(ToolCommandParameterInfo info)
		{
			this.info = info;
		}

		public abstract bool IsValueAllowed(string value, bool isAppliedValue);

		public abstract IToolCommandParameterValue GenerateValue();
	}
}

```

Below is the correct version of the class, where we have properly referenced the class in the variables and constructor, by directly stating the full namespace of the class, which allows us to avoid the incorrect class reference handling.

```cs
using Bonobo.PluginSystem.Custom;
using Corinth.Game.Tools;

namespace Bonobo.Plugins.ToolCommandSource
{
	internal abstract class ToolCommandParameterInfo : IToolCommandParameterInfo
	{
		private Corinth.Game.Tools.ToolCommandParameterInfo info;

		public Corinth.Game.Tools.ToolCommandParameterInfo Info => info;

		public string Name => info.Name;

		public string Explanation => info.Explanation;

		public ToolCommandParameterInfo(Corinth.Game.Tools.ToolCommandParameterInfo info)
		{
			this.info = info;
		}

		public abstract bool IsValueAllowed(string value, bool isAppliedValue);

		public abstract IToolCommandParameterValue GenerateValue();
	}
}

```

In order to solve this issue dynamically, we need to pull a list of the base namespaces used in the file itself (This would include the top level namespaces at the start of the file and the namespace that the class, struct or identifying object uses). Then as we iterate through the rest of the file, for each class reference, we need to check if a class with the same name already exists in any of those namespaces. If it does, we need to get the namespace of the current class reference and append it to the start of the class name in the code output. This should resolve any type related errors.
