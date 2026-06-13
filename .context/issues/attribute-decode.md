## ATTRIBUTE DECODING FAILURE

Currently, there are issues when decoding certain fields inside of attribute classes. One such example is the BonoboPluginAttributes class (Defined below).

```cs
using System;

namespace Bonobo.PluginSystem
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class BonoboPluginAttribute : Attribute
	{
		private string name;

		public string Name => name;

		public InitializationPriority Priority { get; set; }

		public bool IsRequired { get; set; }

		public BonoboPluginAttribute(string name)
		{
			this.name = name;
			Priority = InitializationPriority.Normal;
		}
	}
}
```

Currently, when the attribute gets written to the file, all we get is the following output.

```cs
[BonoboPlugin(/*Could not decode attribute arguments.*/)]
```

This is due to the the handling specified in ``ConvertAttribute()`` in ``ICSharpCode.Decompiler\CSharp\Syntax\TypeSystemAstBuilder.cs``
On line 810, it checks if there were decode errors, and simply replaces all the attributes with the error comment.

This is due to an `EnumUnderlyingTypeResolveException` in `ICSharpCode.Decompiler\TypeSystem\Implementation\CustomAttribute.cs` inside of
`Decode` on line 92. This is most likely due to issues that occur whe attempting to parse the Priority field in the BonoboPluginAttribute.

The fix would be to add proper handling for resolving the underlying enum type. In the event that said enum does not have an underlying type, 
we need to have it default to the int type, since that is the default underlying type that all enums in C# will use if no underlying type is specified.
