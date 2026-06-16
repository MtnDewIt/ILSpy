Now let me read the key files mentioned in the issues:Now let me look at where `GetUnderlyingEnumType` is defined, and the ExpressionBuilder for enum and cast handling:Now I have the root of the `EnumUnderlyingTypeResolveException`. Let me look at the remaining key files:Now let me look at the BAML decompiler and also check for enum-related handling in the expression builder:Now let me look at the CSharp decompiler for partial class handling and the OutputVisitor for enum/cast handling:I now have a thorough understanding of the codebase. Let me compile the full implementation plan:Here is the full implementation plan, broken down per issue with the exact files, root causes, and concrete changes needed.

## Issue 1 — Incorrect class reference when class name collides with declaring type

**Root:** `TypeSystemAstBuilder.cs` → `ConvertTypeHelper(IType, IReadOnlyList<IType>)`. When the resolver finds a short name that resolves successfully, it emits `SimpleType(typeDef.Name)`. But it doesn't check whether that short name *also* refers to the current class being declared — causing ambiguity when a field type has the same name as the containing class.

**Fix (`TypeSystemAstBuilder.cs` in `ConvertTypeHelper`):** After the resolver successfully short-names a type, add a conflict check: if any of the *active namespaces* (the file's `using` directives plus the current class's own namespace) expose a different type with the same simple name, force a fully-qualified path instead. In practice, the simplest implementation is:

Before emitting the `SimpleType`, check:
```cs
// Does the type's namespace differ from the current class's namespace,
// yet the simple name would collide with a type in the current namespace?
bool collidesWithCurrentScope = resolver.CurrentUsingScope != null
    && typeDef.Namespace != resolver.CurrentUsingScope.NamespaceName
    && resolver.Compilation.FindType(
           new FullTypeName(resolver.CurrentUsingScope.NamespaceName + "." + typeDef.Name))
           .Kind != TypeKind.Unknown;

if (collidesWithCurrentScope)
{
    // Fall through to fully-qualified MemberType output
}
else
{
    // Existing short-name emit path
}
```

The "fall through" means the existing `MemberType` path at the bottom of the method (which builds `Namespace.TypeName`) is used instead. No new helpers needed — the full-qualification code already exists; you're just bypassing the short-name shortcut when there is a collision.

---

## Issue 2 — Enum output formatting (four sub-issues)

### 2a — `[Flags]` power-of-two values should use `1 << N` notation

**Root:** Wherever `TypeSystemAstBuilder` (or the output visitor) emits enum member initializer values.

**Fix:** When writing the value of a `[Flags]` enum field, if the value is a power of two, emit `1 << (int)Math.Log2(value)` as a `BinaryOperatorExpression`. If `Math.Log2(value)` is not an integer (i.e., `value` is not a pure power of two), fall through to the existing integer literal emit.

### 2b — Combined `[Flags]` values should use OR-chains

**Fix:** For `[Flags]` enum members whose value is *not* a power of two, decompose into the set of already-defined enum members that OR together to produce it (greedy bitmask match from highest to lowest). Emit as `Flag1 | Flag2 | ...` rather than a raw integer.

### 2c — Enum-typed switch discriminants cast as `(int)val - offset`

**Root:** `StatementBuilder.cs` (or wherever switch statements are translated). The IL switch is on an integer; when the decompiler can't fully match enum values, it subtracts an offset and casts to `int`.

**Fix:** When the switch variable's type is an enum, reconstruct each case label by adding the offset back to each case value and resolving the result to a named enum member (`EnumType.MemberName`). Change the discriminant from `(int)val - N` back to `val` (typed as the enum). This requires knowing the enum type at the point of switch translation.

### 2d — Inline `(EnumType)intLiteral` expressions

**Root:** `ExpressionBuilder.cs` — when it emits a value of an enum type, it sometimes emits an integer cast instead of a named member.

**Fix:** Wherever a `Conv` or `Cast` node produces an enum-typed result, check whether the integer value matches a named member and emit `EnumType.MemberName` instead. For `[Flags]` types, decompose using the same OR-chain logic as 3b.

---

## Issue 3 — Spurious casts

All three sub-cases are in `ExpressionBuilder.cs` and `CallBuilder.cs` during cast/conversion generation.

### 3a — `(T)(object)x` double-cast

**Fix:** When emitting a `CastExpression`, if the inner expression is *also* a `CastExpression` to `object`, and the outer cast target is a reference type or interface that `x`'s original type is already assignable to, drop both casts. Pattern: `if inner cast target == object && outerTarget is assignable from x.Type → emit x directly`.

### 3b — `(T)null` typed null

**Fix:** Wherever a `null` literal is being cast to a specific type for a comparison against `null`, detect the pattern `BinaryOperator(CastExpression(null, T), null)` or equivalent and replace with `BinaryOperator(originalExpr, null)`.

### 3c — `((object)value).ToString()` boxing before method call

**Fix:** In `CallBuilder.cs`, when the receiver of a virtual call (e.g. `ToString()`) is a `CastExpression` to `object`, and the cast's inner expression is a value type or class whose `ToString()` would be inherited, strip the `(object)` cast from the receiver.

---

## Issue 4 — XAML/BAML decompilation

**Root:** `ICSharpCode.BamlDecompiler/Rewrite/MarkupExtensionRewritePass.cs` and `XClassRewritePass.cs`.

### 4a — `<Type.Property><Ctor>value</Ctor>...</Type.Property>` children not inlined as attributes

**Root (`MarkupExtensionRewritePass.cs`):** The `RewriteElement` and `InlineExtension` methods already handle `<Ctor>` inside markup extensions, but the pass only fires when the *parent element itself* is a markup extension. When a `<UIElement.Visibility>` property element wraps a `<Binding>` that has a `<Ctor>`, the outer property element isn't a markup extension — so the pass skips it.

**Fix:** Extend `RewriteElement` to also handle the case where a property element (e.g. `<UIElement.Visibility>`) contains a single child that *is* a markup extension (e.g. `<Binding>`). In that case, inline the whole property element as an attribute on the parent, including converting `<Ctor>` children to the positional constructor arg of the `{Binding ...}` syntax.

The `ConverterExtension` → `Converter` name stripping also needs to happen: when inlining a nested type whose name ends in `Extension` (e.g. `VisibilityConverterExtension` → `VisibilityConverter`), strip the suffix in the inlined form, following standard XAML markup extension conventions.

### 4b — Root element not replaced with base class + `x:Class`

**Root (`XClassRewritePass.cs`):** `RewriteClass` already replaces the element name with the base class and inserts `x:Class`. Review the existing pass against the failing examples — specifically Example 8, where `fields:ViewItemPanel.Name="userControl"` needs to become `x:Name="userControl"` and `Control.FontFamily` needs to lose its qualified prefix. These are attribute namespace normalization issues in the same pass or a companion pass.

**Fix:** Add a normalization step that:
1. Converts `ClassName.PropertyName="value"` attributes on the root element to plain `PropertyName="value"` (removing the class-qualifier prefix) when that class is the resolved type's own type.
2. Replaces `fields:ViewItemPanel.Name` with `x:Name`.
3. Removes the `<!--Unknown connection ID: N-->` comment left by `ConnectionIdRewritePass` (it currently adds this for unmatched IDs; either suppress it, or handle it cleanly).

---

## Issue 5 — XAML partial class / duplicate member output

**Root:** `CSharpDecompiler.cs` / `WholeProjectDecompiler.cs` — when a type has a matching XAML file (`XClassNames` contains its full name after BAML decompilation), the C# output should be marked `partial` and should exclude members that are generated by the XAML build tasks.

**Fix:**
1. **Mark the class `partial`:** After BAML decompilation populates `ctx.XClassNames`, in `WholeProjectDecompiler`, when writing the `.cs` file for a type whose full name appears in `XClassNames`, add the `partial` modifier to the `TypeDeclaration`.
2. **Exclude generated members:** `ConnectionIdRewritePass` already populates `ctx.GeneratedMembers` with the metadata tokens of `InitializeComponent`, `Connect`, and `_contentLoaded`. Pass this set through to the C# decompiler for the same type, and skip emitting any member whose `MetadataToken` is in that set.
3. **Fix base class reference:** When the class name matches the XAML root type's name (e.g. `TagCustomSection`), apply the same fully-qualified base class fix as Issue 2 to avoid the collision between the class being declared and the type it inherits from.
4. **Fix `((BaseClass)this).Method()` → `base.Method()`:** The pattern `((TagCustomSection)this).Close()` should already be handled by ILSpy's existing `base.` transform — verify it fires correctly once the partial class and correct base type are in place.

---

## Summary of files to edit

| File | Issues addressed |
|---|---|
| `TypeSystem/TypeProvider.cs` | 1 |
| `TypeSystem/Implementation/CustomAttribute.cs` | 1 (safety net only) |
| `CSharp/Syntax/TypeSystemAstBuilder.cs` | 2, 3a, 3b |
| `CSharp/ExpressionBuilder.cs` | 3c, 3d, 4a, 4c |
| `CSharp/StatementBuilder.cs` | 3c |
| `CSharp/CallBuilder.cs` | 4c |
| `CSharp/ProjectDecompiler/WholeProjectDecompiler.cs` | 6 |
| `ICSharpCode.BamlDecompiler/Rewrite/MarkupExtensionRewritePass.cs` | 5a |
| `ICSharpCode.BamlDecompiler/Rewrite/XClassRewritePass.cs` | 5b |
| `ICSharpCode.BamlDecompiler/Rewrite/ConnectionIdRewritePass.cs` | 6 (member exclusion) |

Issues 1, 3a, 3b, and 3c are the smallest/lowest-risk changes. Issues 2 (enum formatting) and 4 (XAML rewrite) are the most complex and should be developed and tested independently. Issue 5 (partial class) depends on Issue 4 being solved first since it relies on `XClassNames` and `GeneratedMembers` being correctly populated.