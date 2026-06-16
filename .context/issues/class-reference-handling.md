## INCORRECT CLASS REFERENCE HANDLING

In the example class defined below, have have a class that inherits from an interface. However, the class includes variables which make use of a class with the same name as the abstract class. This will cause incorrect class references, and maybe some errors when trying to compile the class itself.

Below each original example is the correct version of the file, where we have properly referenced the class in the variables and constructor, by directly stating the full namespace of the class, which allows us to avoid the incorrect class reference handling.

### EXAMPLE 1

#### ORIGINAL

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

#### FIXED

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

### EXAMPLE 2

#### ORIGINAL

```cs
using System;
using System.Drawing;
using Corinth.Tags;
using Corinth.TicketTrack;

namespace TAE.Shared.Tags.ManagedBlam
{
	internal class GameBitmap : IGameBitmap, IDisposable
	{
		private readonly GameBitmap bitmap;

		public bool AreAxisFlipped => bitmap.AreAxisFlipped;

		public int Height => bitmap.Height;

		public int SequenceIndex => bitmap.SequenceIndex;

		public int SpriteFrameIndex => bitmap.SpriteFrameIndex;

		public int Width => bitmap.Width;

		public GameBitmap(GameBitmap bitmap)
		{
			Assert.Check(bitmap != null);
			this.bitmap = bitmap;
		}

		public Bitmap GetBitmap()
		{
			return bitmap.GetBitmap();
		}

		public Bitmap GetBitmap(GameBitmapChannel channel)
		{
			return bitmap.GetBitmap((GameBitmapChannel)channel);
		}

		public byte[] GetBitmapArgb(GameBitmapChannel channel)
		{
			return bitmap.GetBitmapArgb((GameBitmapChannel)channel);
		}

		public void Dispose()
		{
			bitmap.Dispose();
		}
	}
}
```

#### FIXED

```cs
using System;
using System.Drawing;
using Corinth.Tags;
using Corinth.TicketTrack;

namespace TAE.Shared.Tags.ManagedBlam
{
	internal class GameBitmap : IGameBitmap, IDisposable
	{
		private readonly Corinth.Tags.GameBitmap bitmap;

		public bool AreAxisFlipped => bitmap.AreAxisFlipped;

		public int Height => bitmap.Height;

		public int SequenceIndex => bitmap.SequenceIndex;

		public int SpriteFrameIndex => bitmap.SpriteFrameIndex;

		public int Width => bitmap.Width;

		public GameBitmap(Corinth.Tags.GameBitmap bitmap)
		{
			Assert.Check(bitmap != null);
			this.bitmap = bitmap;
		}

		public Bitmap GetBitmap()
		{
			return bitmap.GetBitmap();
		}

		public Bitmap GetBitmap(GameBitmapChannel channel)
		{
			return bitmap.GetBitmap((Corinth.Tags.GameBitmapChannel)channel);
		}

		public byte[] GetBitmapArgb(GameBitmapChannel channel)
		{
			return bitmap.GetBitmapArgb((Corinth.Tags.GameBitmapChannel)channel);
		}

		public void Dispose()
		{
			bitmap.Dispose();
		}
	}
}
```

### EXAMPLE 3

#### ORIGINAL

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using Corinth.Game;
using Corinth.Tags;

namespace TAE.Shared.Tags.ManagedBlam
{
	internal class TagContext
	{
		internal class WeakPair
		{
			private WeakReference firstRef = new WeakReference(null);

			private WeakReference secondRef = new WeakReference(null);

			public object First
			{
				get
				{
					return firstRef.Target;
				}
				set
				{
					firstRef.Target = value;
				}
			}

			public object Second
			{
				get
				{
					return secondRef.Target;
				}
				set
				{
					secondRef.Target = value;
				}
			}

			public GenericWeakPair<T1, T2> AsGeneric<T1, T2>() where T1 : class where T2 : class
			{
				return new GenericWeakPair<T1, T2>(this);
			}
		}

		internal class GenericWeakPair<T1, T2> where T1 : class where T2 : class
		{
			private WeakPair pair;

			public T1 First
			{
				get
				{
					return pair.First as T1;
				}
				set
				{
					pair.First = value;
				}
			}

			public T2 Second
			{
				get
				{
					return pair.Second as T2;
				}
				set
				{
					pair.Second = value;
				}
			}

			public GenericWeakPair()
			{
				pair = new WeakPair();
			}

			public GenericWeakPair(WeakPair pair)
			{
				this.pair = pair;
			}
		}

		private IDictionary<TagFieldPath, WeakPair> pairsDict = new Dictionary<TagFieldPath, WeakPair>();

		internal WeakPair GetPair(TagFieldPath key)
		{
			WeakPair value = null;
			if (!pairsDict.TryGetValue(key, out value))
			{
				value = new WeakPair();
				pairsDict.Add(new KeyValuePair<TagFieldPath, WeakPair>(key, value));
			}
			else if (value == null)
			{
				value = new WeakPair();
				pairsDict[key] = value;
			}
			return value;
		}

		internal GenericWeakPair<T1, T2> GetPair<T1, T2>(TagFieldPath key) where T1 : class where T2 : class
		{
			GenericWeakPair<T1, T2> result = null;
			WeakPair pair = GetPair(key);
			if (pair != null)
			{
				result = pair.AsGeneric<T1, T2>();
			}
			return result;
		}

		internal TagFieldPath GetTagFieldPath<T>(T source) where T : class
		{
			TagFieldPath result = null;
			if (source is TagFile)
			{
				result = new TagFieldPath("", null, -1);
			}
			else if (source is TagFileElement)
			{
				result = new TagFieldPath("", null, 0);
			}
			else if (source is TagElement)
			{
				result = Convert(((TagElement)((source is TagElement) ? source : null)).GetTagFieldPath());
			}
			else if (source is TagField)
			{
				result = Convert(((TagField)((source is TagField) ? source : null)).GetTagFieldPath());
			}
			return result;
		}

		internal R WrapTagObject<T, R>(T source) where T : class where R : class
		{
			R result = null;
			if (source != null)
			{
				TagFieldPath tagFieldPath = GetTagFieldPath(source);
				if (tagFieldPath != null)
				{
					GenericWeakPair<T, R> pair = GetPair<T, R>(tagFieldPath);
					if (pair.First == null)
					{
						pair.First = source;
					}
					if (pair.Second == null)
					{
						pair.Second = TagInterfaceFactory.New<T, R>(this, source);
					}
					return pair.Second;
				}
				return TagInterfaceFactory.New<T, R>(this, source);
			}
			return result;
		}

		internal IEnumerable<R> WrapTagObjects<T, R>(IEnumerable<T> sources) where T : class where R : class
		{
			IEnumerable<R> result = null;
			if (sources != null)
			{
				int num = 0;
				R[] array = new R[sources.Count()];
				foreach (T source in sources)
				{
					array[num++] = WrapTagObject<T, R>(source);
				}
				result = array.AsEnumerable();
			}
			return result;
		}

		internal TagPath Convert(TagPath path)
		{
			if (path != null)
			{
				return TagPath.FromPathAndExtension(path.RelativePath, path.Extension);
			}
			return null;
		}

		internal TagPath Convert(TagPath path)
		{
			if (path != (TagPath)null)
			{
				return TagPath.FromPathAndExtension(path.RelativePath, path.Extension);
			}
			return null;
		}

		internal TagFieldPath Convert(TagFieldPath path)
		{
			if (path != null)
			{
				return TagFieldPath.Parse(path.Path);
			}
			return null;
		}

		internal TagFieldPath Convert(TagFieldPath path)
		{
			if (path != (TagFieldPath)null)
			{
				return TagFieldPath.Parse(path.Path);
			}
			return null;
		}

		internal TagFieldType Convert(TagFieldType type)
		{
			return (TagFieldType)type;
		}

		internal TagFieldType Convert(TagFieldType type)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Expected I4, but got Unknown
			return (TagFieldType)type;
		}

		internal TagReference Convert(TagReference reference)
		{
			throw new NotSupportedException();
		}

		internal TagReference Convert(TagReference reference)
		{
			if (reference != null)
			{
				return new TagReference
				{
					Path = Convert(reference.Path)
				};
			}
			return null;
		}

		internal TagFieldCustomType Convert(TagFieldCustomType flags)
		{
			return (TagFieldCustomType)flags;
		}

		internal TagFieldCustomType Convert(TagFieldCustomType flags)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Expected I4, but got Unknown
			return (TagFieldCustomType)flags;
		}

		internal GameColor Convert(GameColor color)
		{
			if (color != null)
			{
				switch (color.ColorMode)
				{
				case GameColorMode.Rgb:
					return GameColor.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
				case GameColorMode.Hsv:
					return GameColor.FromAhsv(color.Alpha, color.Hue, color.Saturation, color.Value);
				}
			}
			return null;
		}

		internal GameColor Convert(GameColor color)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Invalid comparison between Unknown and I4
			if (color != null)
			{
				GameColorMode colorMode = color.ColorMode;
				if ((int)colorMode == 0)
				{
					return GameColor.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
				}
				if ((int)colorMode == 1)
				{
					return GameColor.FromAhsv(color.Alpha, color.Hue, color.Saturation, color.Value);
				}
			}
			return null;
		}

		internal GamePoint2d Convert(GamePoint2d point)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Expected O, but got Unknown
			if (point != null)
			{
				return new GamePoint2d(point.X, point.Y);
			}
			return null;
		}

		internal GamePoint2d Convert(GamePoint2d point)
		{
			if (point != null)
			{
				return new GamePoint2d(point.X, point.Y);
			}
			return null;
		}

		internal GamePoint3d Convert(GamePoint3d point)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			if (point != null)
			{
				return new GamePoint3d(point.X, point.Y, point.Z);
			}
			return null;
		}

		internal GamePoint3d Convert(GamePoint3d point)
		{
			if (point != null)
			{
				return new GamePoint3d(((GamePoint2d)point).X, ((GamePoint2d)point).Y, point.Z);
			}
			return null;
		}
	}
}
```

#### FIXED

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using Corinth.Game;
using Corinth.Tags;

namespace TAE.Shared.Tags.ManagedBlam
{
	internal class TagContext
	{
		internal class WeakPair
		{
			private WeakReference firstRef = new WeakReference(null);

			private WeakReference secondRef = new WeakReference(null);

			public object First
			{
				get
				{
					return firstRef.Target;
				}
				set
				{
					firstRef.Target = value;
				}
			}

			public object Second
			{
				get
				{
					return secondRef.Target;
				}
				set
				{
					secondRef.Target = value;
				}
			}

			public GenericWeakPair<T1, T2> AsGeneric<T1, T2>() where T1 : class where T2 : class
			{
				return new GenericWeakPair<T1, T2>(this);
			}
		}

		internal class GenericWeakPair<T1, T2> where T1 : class where T2 : class
		{
			private WeakPair pair;

			public T1 First
			{
				get
				{
					return pair.First as T1;
				}
				set
				{
					pair.First = value;
				}
			}

			public T2 Second
			{
				get
				{
					return pair.Second as T2;
				}
				set
				{
					pair.Second = value;
				}
			}

			public GenericWeakPair()
			{
				pair = new WeakPair();
			}

			public GenericWeakPair(WeakPair pair)
			{
				this.pair = pair;
			}
		}

		private IDictionary<TagFieldPath, WeakPair> pairsDict = new Dictionary<TagFieldPath, WeakPair>();

		internal WeakPair GetPair(TagFieldPath key)
		{
			WeakPair value = null;
			if (!pairsDict.TryGetValue(key, out value))
			{
				value = new WeakPair();
				pairsDict.Add(new KeyValuePair<TagFieldPath, WeakPair>(key, value));
			}
			else if (value == null)
			{
				value = new WeakPair();
				pairsDict[key] = value;
			}
			return value;
		}

		internal GenericWeakPair<T1, T2> GetPair<T1, T2>(TagFieldPath key) where T1 : class where T2 : class
		{
			GenericWeakPair<T1, T2> result = null;
			WeakPair pair = GetPair(key);
			if (pair != null)
			{
				result = pair.AsGeneric<T1, T2>();
			}
			return result;
		}

		internal TagFieldPath GetTagFieldPath<T>(T source) where T : class
		{
			TagFieldPath result = null;
			if (source is Corinth.Tags.TagFile)
			{
				result = new TagFieldPath("", null, -1);
			}
			else if (source is Corinth.Tags.TagFileElement)
			{
				result = new TagFieldPath("", null, 0);
			}
			else if (source is Corinth.Tags.TagElement)
			{
				result = Convert((source as Corinth.Tags.TagElement).GetTagFieldPath());
			}
			else if (source is Corinth.Tags.TagField)
			{
				result = Convert((source as Corinth.Tags.TagField).GetTagFieldPath());
			}
			return result;
		}

		internal R WrapTagObject<T, R>(T source) where T : class where R : class
		{
			R result = null;
			if (source != null)
			{
				TagFieldPath tagFieldPath = GetTagFieldPath(source);
				if (tagFieldPath != null)
				{
					GenericWeakPair<T, R> pair = GetPair<T, R>(tagFieldPath);
					if (pair.First == null)
					{
						pair.First = source;
					}
					if (pair.Second == null)
					{
						pair.Second = TagInterfaceFactory.New<T, R>(this, source);
					}
					return pair.Second;
				}
				return TagInterfaceFactory.New<T, R>(this, source);
			}
			return result;
		}

		internal IEnumerable<R> WrapTagObjects<T, R>(IEnumerable<T> sources) where T : class where R : class
		{
			IEnumerable<R> result = null;
			if (sources != null)
			{
				int num = 0;
				R[] array = new R[sources.Count()];
				foreach (T source in sources)
				{
					array[num++] = WrapTagObject<T, R>(source);
				}
				result = array.AsEnumerable();
			}
			return result;
		}

		internal Corinth.Tags.TagPath Convert(TagPath path)
		{
			if (path != null)
			{
				return Corinth.Tags.TagPath.FromPathAndExtension(path.RelativePath, path.Extension);
			}
			return null;
		}

		internal TagPath Convert(Corinth.Tags.TagPath path)
		{
			if (path != (Corinth.Tags.TagPath)null)
			{
				return TagPath.FromPathAndExtension(path.RelativePath, path.Extension);
			}
			return null;
		}

		internal Corinth.Tags.TagFieldPath Convert(TagFieldPath path)
		{
			if (path != null)
			{
				return Corinth.Tags.TagFieldPath.Parse(path.Path);
			}
			return null;
		}

		internal TagFieldPath Convert(Corinth.Tags.TagFieldPath path)
		{
			if (path != (Corinth.Tags.TagFieldPath)null)
			{
				return TagFieldPath.Parse(path.Path);
			}
			return null;
		}

		internal Corinth.Tags.TagFieldType Convert(TagFieldType type)
		{
			return (Corinth.Tags.TagFieldType)type;
		}

		internal TagFieldType Convert(Corinth.Tags.TagFieldType type)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Expected I4, but got Unknown
			return (TagFieldType)type;
		}

		internal TagReference Convert(TagReference reference)
		{
			throw new NotSupportedException();
		}

		internal TagReference Convert(Corinth.Tags.TagReference reference)
		{
			if (reference != null)
			{
				return new TagReference
				{
					Path = Convert(reference.Path)
				};
			}
			return null;
		}

		internal Corinth.Tags.TagFieldCustomType Convert(TagFieldCustomType flags)
		{
			return (Corinth.Tags.TagFieldCustomType)flags;
		}

		internal TagFieldCustomType Convert(Corinth.Tags.TagFieldCustomType flags)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Expected I4, but got Unknown
			return (TagFieldCustomType)flags;
		}

		internal Corinth.Game.GameColor Convert(GameColor color)
		{
			if (color != null)
			{
				switch (color.ColorMode)
				{
				case GameColorMode.Rgb:
					return Corinth.Game.GameColor.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
				case GameColorMode.Hsv:
					return Corinth.Game.GameColor.FromAhsv(color.Alpha, color.Hue, color.Saturation, color.Value);
				}
			}
			return null;
		}

		internal GameColor Convert(Corinth.Game.GameColor color)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Invalid comparison between Unknown and I4
			if (color != null)
			{
	            Corinth.Game.GameColorMode colorMode = color.ColorMode;
				if ((int)colorMode == 0)
				{
					return GameColor.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
				}
				if ((int)colorMode == 1)
				{
					return GameColor.FromAhsv(color.Alpha, color.Hue, color.Saturation, color.Value);
				}
			}
			return null;
		}

		internal Corinth.Game.GamePoint2d Convert(GamePoint2d point)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Expected O, but got Unknown
			if (point != null)
			{
				return new Corinth.Game.GamePoint2d(point.X, point.Y);
			}
			return null;
		}

		internal GamePoint2d Convert(Corinth.Game.GamePoint2d point)
		{
			if (point != null)
			{
				return new GamePoint2d(point.X, point.Y);
			}
			return null;
		}

		internal Corinth.Game.GamePoint3d Convert(GamePoint3d point)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			if (point != null)
			{
				return new Corinth.Game.GamePoint3d(point.X, point.Y, point.Z);
			}
			return null;
		}

		internal GamePoint3d Convert(Corinth.Game.GamePoint3d point)
		{
			if (point != null)
			{
				return new GamePoint3d(((Corinth.Game.GamePoint2d)point).X, ((Corinth.Game.GamePoint2d)point).Y, point.Z);
			}
			return null;
		}
	}
}
```

To solve this issue dynamically, I have written some pseudocode to outline this a little more clearly

```cs

// All top level namespaces in file
topLevelNamspaces = [];

// Namespace of the current file (class or member usually sits within this)
classOrMemberFileNamespace;

// Collection of all namespaces in the current file
allNamespaces = topLevelNamspaces + classOrMemberFileNamespace

// Collection of all class and member references in file
classAndMemberReferencesInFile = [];

// Loop through each class or member
foreach (classOrMemberReference in classAndMemberReferencesInFile)
{
	// Loop through each namespace
	foreach (currentNamespace in allNamespaces)
	{
		// Some logic to determine if a class or member with the same name as classOrMemberReference exists in currentNamespace.
		bool isInNamespace = IsClassInNamespace(classOrMemberReference, currentNamespace);

		if (isInNamespace)
		{
			// Some logic to determine if the class or member with the same name as classOrMemberReference exists in the file namespace
			bool isPartOfFileNamespace = isPartOfFileNamespace(classOrMemberFileNamespace, classOrMemberReference);

			// If the class or member is not part of the file namespace
			if (!isPartOfFileNamespace)
			{
				// Write the full class or member name to the output file, ie:
				// Corinth.Game.GamePoint3d
				// instead of
				// GamePoint3d

				// It doesn't really matter how we handle it we just need a way of marking that this specific reference needs to use the full length namespace.
			}
		}
	}
}

```