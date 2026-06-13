### GENERAL ISSUES

---

There are some general type casting issues in the code output, where for some reason it'll attempt to cast as an object, then as the parent type. Ideally, we shouldn't need to do this.

```cs
using System.Windows.Controls;
using Corinth.Tags;
using Corinth.Utilities;

namespace Bonobo.PluginSystem.Custom
{
	public class TagCustomSection : UserControl, ITagCustomSection
	{
		public IWeakEvent<TagChangedEventArgs> SendTagChanged => (IWeakEvent<TagChangedEventArgs>)(object)sendTagChanged;

		public IWeakEvent<PreviewTagActionEventArgs> PreviewTagAction => (IWeakEvent<PreviewTagActionEventArgs>)(object)previewTagAction;
	}
}
```

This should be the correct output.

```cs
using System.Windows.Controls;
using Corinth.Tags;
using Corinth.Utilities;

namespace Bonobo.PluginSystem.Custom
{
	public class TagCustomSection : UserControl, ITagCustomSection
	{
		public IWeakEvent<TagChangedEventArgs> SendTagChanged => sendTagChanged;

		public IWeakEvent<PreviewTagActionEventArgs> PreviewTagAction => previewTagAction;
	}
}
```

---

There is also another issue where for some rreason when we attempt to perform a comparison operation between two objects, if we are comparing against a null object, it'll cast the null as the type of the object we are comparing against.

```cs
if (Value != (AssetPropertyValue)null)
```

It should just be the null object itself without the cast.

```cs
if (Value != null)
```

---

This issue can also occur with variables inside of constructors for classes.

```cs
public AssetSetLibrary(string displayName, IEnumerable<IAssetSet> children)
	: base(displayName, children, (IEnumerable<AssetDescription>)null)
{
}
```

---

Another issue is it casting values as an object when realistically, it doesn't need to be.

```cs
public string DisplayValue;
public AssetPropertyValue Value;

DisplayValue = ((object)Value).ToString();
```

We should just use the value itself, without any casts.

```cs
public string DisplayValue;
public AssetPropertyValue Value;

DisplayValue = Value.ToString();
```

---

