## INCORRECT ENUM HANDLING

---

Below are two enums which we will use as examples.

```cs
public enum AssetSetType
{
	All = 0,
	Prefab = 1,
	GameAsset = 2,
	DecalAsset = 3,
	DecoratorAsset = 4,
	BitmapAsset = 5,
	ShaderAsset = 6,
	EffectAsset = 7,
	RefIOAsset = 8
}

[Flags]
public enum AssetBrowserViewFlags
{
	Prefab = 1,
	GameAsset = 2,
	DecalAsset = 4,
	DecoratorAsset = 8,
	BitmapAsset = 0x10,
	ShaderAsset = 0x20,
	EffectAsset = 0x40,
	RefIOAsset = 0x80
}
```

There a minor formatting issue where each value of the flags enum is output using an integer value.
I would to change it so that it equals `1 << log(enum_integer_value)` so that way the flags enum specified below would actually look like. If the result of the log operation is a non integer value, we'll simply just use enum's raw integer value. 

```cs
[Flags]
public enum AssetBrowserViewFlags
{
	Prefab = 1 << 0,
	GameAsset = 1 << 1,
	DecalAsset = 1 << 2,
	DecoratorAsset = 1 << 3,
	BitmapAsset = 1 << 4,
	ShaderAsset = 1 << 5,
	EffectAsset = 1 << 6,
	RefIOAsset = 1 << 7,
}
```

We should also account for certain values where the enum value's actual integer value is defined using a combination of other flags values. Below is one such example.

```cs
[Flags]
public enum ArrowEnds
{
	None = 0,
	Start = 1,
	End = 2,
	Both = 3
}
```

This is what we should actually be outputting it as.

```cs
[Flags]
public enum ArrowEnds
{
	None = 0,
	Start = 1,
	End = 2,
	Both = Start | End,
}
```

So currently when an enum gets called or referenced inside of switch statement, for some reason it gets cast as an integer value and parsed that way. 

```cs
private unsafe AssetBrowserViewFlags GetViewFlags(AssetSetType type)
{
	AssetSetType val = type;
	AssetSetType val2 = val;
	switch ((int)val2)
	{
	case 0:
		return (AssetBrowserViewFlags)30;
	case 5:
		return (AssetBrowserViewFlags)16;
	case 3:
		return (AssetBrowserViewFlags)4;
	case 4:
		return (AssetBrowserViewFlags)8;
	case 2:
		return (AssetBrowserViewFlags)2;
	case 1:
		return (AssetBrowserViewFlags)1;
	case 6:
		return (AssetBrowserViewFlags)32;
	case 7:
		return (AssetBrowserViewFlags)64;
	case 8:
		return (AssetBrowserViewFlags)128;
	default:
		throw new NotImplementedException("GetViewFlags for " + ((object)(*(AssetSetType*)(&type))/*cast due to constrained. prefix*/).ToString());
	}
}
```

Here is what we should be outputting using the actual enum values and accounting for when a value is equal to a combination of flags instead of a singular flag.

```cs
private AssetBrowserViewFlags GetViewFlags(AssetSetType type)
{
	switch (type)
	{
		case AssetSetType.All:
            return AssetBrowserViewFlags.GameAsset | AssetBrowserViewFlags.DecalAsset | AssetBrowserViewFlags.DecoratorAsset | AssetBrowserViewFlags.BitmapAsset; 
		case AssetSetType.BitmapAsset:
            return AssetBrowserViewFlags.BitmapAsset; 
		case AssetSetType.DecalAsset:
            return AssetBrowserViewFlags.DecalAsset;
		case AssetSetType.DecoratorAsset:
            return AssetBrowserViewFlags.DecoratorAsset; 
		case AssetSetType.GameAsset:
            return AssetBrowserViewFlags.GameAsset; 
		case AssetSetType.Prefab:
            return AssetBrowserViewFlags.Prefab;
		case AssetSetType.ShaderAsset:
            return AssetBrowserViewFlags.ShaderAsset; 
		case AssetSetType.EffectAsset:
            return AssetBrowserViewFlags.EffectAsset; 
		case AssetSetType.RefIOAsset:
            return AssetBrowserViewFlags.RefIOAsset;
		default:
            throw new NotImplementedException("GetViewFlags for " + type); 
	};
}
```

This also happends when an enum is referenced directly as a variable.

```cs
public AssetSetType Type => (AssetSetType)5;
```

Should be formatted as

```cs
public AssetSetType Type => AssetSetType.BitmapAsset;
```

---

There is also a minor issues with the formatting for enums in switch cases, where the enum is cast as an integer value, it'll be shifted down by subracting from the enum, instead of just shifting all the cases down by said value, and using the enum values directly.

```cs
public enum TagFieldType
{
    String,
    LongString,
    StringId,
    OldStringId,
    CharInteger,
    ShortInteger,
    LongInteger,
    Int64Integer,
    Angle,
    Tag,
    CharEnum,
    ShortEnum,
    LongEnum,
    Flags,
    WordFlags,
    ByteFlags,
    Point2d,
    Rectangle2d,
    RgbPixel32,
    ArgbPixel32,
    Real,
    RealFraction,
    RealPoint2d,
    RealPoint3d,
    RealVector2d,
    RealVector3d,
    RealQuaternion,
    RealEulerAngles2d,
    RealEulerAngles3d,
    RealPlane2d,
    RealPlane3d,
    RealRgbColor,
    RealArgbColor,
    RealHsvColor,
    RealAhsvColor,
    ShortIntegerBounds,
    AngleBounds,
    RealBounds,
    RealFractionBounds,
    Reference,
    Block,
    BlockFlags,
    WordBlockFlags,
    ByteBlockFlags,
    CharBlockIndex,
    CharBlockIndexCustomSearch,
    ShortBlockIndex,
    ShortBlockIndexCustomSearch,
    LongBlockIndex,
    LongBlockIndexCustomSearch,
    Data,
    VertexBuffer,
    Pad,
    UselessPad,
    Skip,
    RuntimeHandle,
    Explanation,
    Custom,
    Struct,
    Array,
    Resource,
    Interop,
    Terminator,
    ByteInteger,
    WordInteger,
    DwordInteger,
    QwordInteger
}
```

Below is an example of a switch statement where this occurs

```cs
TagFieldType fieldType;
switch ((int)fieldType - 16)
{
case 0:
case 1:
case 6:
case 7:
case 8:
case 9:
case 10:
case 11:
case 12:
case 13:
case 14:
case 19:
case 20:
case 21:
case 22:
	flag = false;
	break;
case 2:
case 3:
case 15:
case 16:
case 17:
case 18:
	flag = true;
	break;
}
```

Here is how it should be handled

```cs
TagFieldType fieldType;
switch (fieldType)
{
case TagFieldType.Point2d:
case TagFieldType.Rectangle2d:
case TagFieldType.RealPoint2d:
case TagFieldType.RealPoint3d:
case TagFieldType.RealVector2d:
case TagFieldType.RealVector3d:
case TagFieldType.RealQuaternion:
case TagFieldType.RealEulerAngles2d:
case TagFieldType.RealEulerAngles3d:
case TagFieldType.RealPlane2d:
case TagFieldType.RealPlane3d:
case TagFieldType.ShortIntegerBounds:
case TagFieldType.AngleBounds:
case TagFieldType.RealBounds:
case TagFieldType.RealFractionBounds:
	flag = false;
	break;
case TagFieldType.RgbPixel32:
case TagFieldType.ArgbPixel32:
case TagFieldType.RealRgbColor:
case TagFieldType.RealArgbColor:
case TagFieldType.RealHsvColor:
case TagFieldType.RealAhsvColor:
	flag = true;
	break;
}
```
