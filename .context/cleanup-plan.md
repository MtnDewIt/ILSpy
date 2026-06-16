## Issue 1 — Name-collision: short type name shadows the declaring type

### Root cause

`TypeSystemAstBuilder.ConvertTypeHelper(IType genericType, IReadOnlyList<IType> typeArguments)` contains the short-name path:

```csharp
// ICSharpCode.Decompiler/CSharp/Syntax/TypeSystemAstBuilder.cs  (~line 540)
ResolveResult rr = resolver.LookupSimpleNameOrTypeName(typeDef.Name, ...);
TypeResolveResult trr = rr as TypeResolveResult;
if (trr != null || ...)
{
    if (!trr.IsError && TypeMatches(trr.Type, typeDef, typeArguments))
    {
        SimpleType shortResult = MakeSimpleType(typeDef.Name);
        ...
        return shortResult;       // ← emits the bare short name
    }
}
```

`LookupSimpleNameOrTypeName` succeeds and the resolver resolves the short name to the *right* type—but only because the lookup is done from the *call site*, not from inside the declaring class. When the declaring class has the same simple name as the referenced type (e.g. class `ToolCommandParameterInfo` referencing `Corinth.Game.Tools.ToolCommandParameterInfo`), the C# compiler will bind the unqualified name to the *enclosing* class instead of the external one. ILSpy does not detect this clash.

### Fix location

**File:** `ICSharpCode.Decompiler/CSharp/Syntax/TypeSystemAstBuilder.cs`  
**Method:** `ConvertTypeHelper(IType genericType, IReadOnlyList<IType> typeArguments)`

### Fix logic

Immediately **before** the `return shortResult;` line, insert a clash-detection guard:

```csharp
// After confirming TypeMatches, check whether the bare name would shadow
// a type in the current declaring scope.
if (CollidesWithDeclaringScope(typeDef))
{
    // fall through to the full-qualification path below
}
else
{
    SimpleType shortResult = MakeSimpleType(typeDef.Name);
    AddTypeArguments(shortResult, ...);
    return shortResult;
}
```

**`CollidesWithDeclaringScope` helper** (private, same class):

```csharp
bool CollidesWithDeclaringScope(ITypeDefinition typeDef)
{
    if (resolver?.CurrentUsingScope == null)
        return false;

    // Walk from innermost scope outward. The first scope that
    // defines a type with the same simple name, where that type
    // is NOT the one we want, is a collision.
    for (var scope = resolver.CurrentUsingScope; scope != null; scope = scope.Parent)
    {
        string scopeFullName = scope.Namespace.FullName;

        var candidate = resolver.Compilation.FindType(
            new FullTypeName(
                string.IsNullOrEmpty(scopeFullName)
                    ? typeDef.Name
                    : scopeFullName + "." + typeDef.Name));

        if (candidate.Kind != TypeKind.Unknown
            && candidate.GetDefinition()?.FullName != typeDef.FullName)
        {
            return true;   // a different type with the same name exists in scope
        }
    }
    return false;
}
```

When the guard fires, execution falls through to the existing `MemberType` path that builds a fully-qualified `Namespace.TypeName` node — no new code path is required there.

### Affected scope

This covers all three examples in `.context/issues/class-reference-handling.md`:
- Field/property types that share a name with the enclosing class.
- Constructor parameters with the same collision.
- `is`/`as` casts in method bodies (handled by the same `ConvertTypeHelper` call path).

### Risk

Low. The change is purely additive — it only suppresses the short-name shortcut when a conflict exists. It cannot produce *wrong* output; it can only produce *longer* (fully-qualified) output than strictly necessary for edge cases where there is no conflict but the guard fires. The `FindType` call is cheap (dictionary lookup in the existing compilation).

---

## Issue 2 — Enum output formatting

Four sub-issues, all related to how enum member values are rendered.

### 2a — `[Flags]` power-of-two values → `1 << N`

**File:** `ICSharpCode.Decompiler/CSharp/Syntax/TypeSystemAstBuilder.cs`  
**Method:** `ConvertConstantValue` (the path that handles enum field initializers)

**Logic:**

```csharp
// When emitting a [Flags] enum field whose value is a pure power of two:
if (IsPowerOfTwo(longValue) && longValue > 0)
{
    int shift = (int)Math.Log2(longValue);
    // Emit: 1 << shift  as a BinaryOperatorExpression
    return new BinaryOperatorExpression(
        new PrimitiveExpression(1),
        BinaryOperatorType.ShiftLeft,
        new PrimitiveExpression(shift));
}
// Otherwise fall through to raw integer literal.
```

`IsPowerOfTwo(n)` = `n > 0 && (n & (n - 1)) == 0`.

### 2b — Combined `[Flags]` values → OR-chain

**File:** Same method, same location.

**Logic (greedy decomposition):**

```csharp
// Collect already-declared members with power-of-two values, descending order.
// Greedily subtract bits from the target value.
var flagMembers = enumType.Fields
    .Where(f => f.IsConst && IsPowerOfTwo((long)f.ConstantValue))
    .OrderByDescending(f => (long)f.ConstantValue)
    .ToList();

long remaining = targetValue;
var parts = new List<Expression>();
foreach (var member in flagMembers)
{
    long v = (long)member.ConstantValue;
    if ((remaining & v) == v)
    {
        remaining &= ~v;
        parts.Add(new MemberReferenceExpression(
            new TypeReferenceExpression(ConvertType(enumType)), member.Name));
    }
}

if (remaining == 0 && parts.Count > 0)
{
    // Fold parts into a | chain
    Expression chain = parts[0];
    for (int i = 1; i < parts.Count; i++)
        chain = new BinaryOperatorExpression(chain, BinaryOperatorType.BitwiseOr, parts[i]);
    return chain;
}
// Otherwise fall through to raw integer literal.
```

### 2c — Switch discriminant: `(int)val - offset` → `val` with named cases

**File:** `ICSharpCode.Decompiler/CSharp/StatementBuilder.cs`  
**Method:** `VisitSwitchInstruction` (or wherever `SwitchInstruction` is translated)

**Root cause:** When the IL switch table doesn't start at 0, the decompiler currently emits `(int)discriminant - baseOffset` as the switch expression and keeps the case values as-is. When the discriminant's type is an enum, this should instead:

1. Restore the switch expression to the original enum-typed variable.
2. Add `baseOffset` back to each case value and resolve the result to the named enum member.

**Logic:**

```csharp
// Detect the pattern: SwitchInstruction whose value is
// a BinaryNumericInstruction(Sub) with one operand being an integer constant.
if (switchValue is BinaryNumericInstruction { Operator: BinaryNumericOperator.Sub } sub
    && sub.Right.MatchLdcI4(out int baseOffset)
    && sub.Left.InferType() is IType { Kind: TypeKind.Enum } enumType)
{
    // Restore the switch expression to the original variable
    switchExpr = exprBuilder.Translate(sub.Left);

    // Adjust each case label: label += baseOffset, then resolve to enum member
    foreach (var section in switchSections)
    {
        section.CaseLabels = section.CaseLabels
            .Select(label => ResolveEnumLabel(enumType, label.Value + baseOffset))
            .ToList();
    }
}

// Helper
Expression ResolveEnumLabel(IType enumType, long value)
{
    var member = enumType.GetDefinition().Fields
        .FirstOrDefault(f => f.IsConst && Convert.ToInt64(f.ConstantValue) == value);
    if (member != null)
        return new MemberReferenceExpression(
            new TypeReferenceExpression(astBuilder.ConvertType(enumType)), member.Name);
    return new PrimitiveExpression(value);
}
```

### 2d — Inline `(EnumType)intLiteral` → named member

**File:** `ICSharpCode.Decompiler/CSharp/ExpressionBuilder.cs`  
**Location:** `VisitConv` / `VisitCast` — wherever an integer value is cast to an enum type.

**Logic:**

```csharp
// When the target type is an enum and the source is an integer constant:
if (targetType.Kind == TypeKind.Enum && sourceExpr is PrimitiveExpression { Value: long intVal })
{
    // Check if it's a [Flags] enum
    bool isFlags = targetType.GetDefinition()
        .GetAttributes().Any(a => a.AttributeType.FullName == "System.FlagsAttribute");

    if (isFlags)
        return TryBuildFlagsExpression(targetType, intVal) ?? fallback;
    else
    {
        var member = targetType.GetDefinition().Fields
            .FirstOrDefault(f => f.IsConst && Convert.ToInt64(f.ConstantValue) == intVal);
        if (member != null)
            return new MemberReferenceExpression(
                new TypeReferenceExpression(astBuilder.ConvertType(targetType)), member.Name);
    }
}
```

`TryBuildFlagsExpression` reuses the same greedy OR-chain logic from 2b.

### Risk for Issue 2

Medium. The enum name-resolution logic touches a heavily exercised code path. The key guard is the `remaining == 0 && parts.Count > 0` check — if decomposition is incomplete (remaining bits don't match any declared member), the fallback is the existing raw integer literal, so output is never *worse* than before.

---

## Issue 3 — Spurious casts

### 3a — `(T)(object)x` double-cast elimination

**File:** `ICSharpCode.Decompiler/CSharp/Syntax/TypeSystemAstBuilder.cs` or `ExpressionBuilder.cs`  
**Location:** The point where a `CastExpression` node is assembled (look for `AddCast` or equivalent).

**Logic:**

```csharp
// If the inner expression is already a cast to object,
// and outerTarget is assignable from x's original type,
// emit x directly (drop both casts).
if (castExpr.Expression is CastExpression inner
    && inner.Type is PrimitiveType { Keyword: "object" }
    && IsAssignableTo(originalType, outerTargetType))
{
    return inner.Expression.Clone(); // emit x directly
}
```

`IsAssignableTo` uses `IType.GetAllBaseTypes()` to check the relationship.

### 3b — `(T)null` in comparisons

**File:** `ICSharpCode.Decompiler/CSharp/ExpressionBuilder.cs`  
**Location:** Binary comparison translation (equality/inequality operators).

**Logic:**

```csharp
// Pattern: BinaryOperator(CastExpression(null, T), someExpr) or vice versa
// Replace CastExpression(null, T) with NullReferenceExpression directly.
if (left is CastExpression { Expression: NullReferenceExpression })
    left = new NullReferenceExpression();
if (right is CastExpression { Expression: NullReferenceExpression })
    right = new NullReferenceExpression();
```

This also applies to constructor call arguments of the form `(IEnumerable<T>)null`.

### 3c — `((object)value).ToString()` boxing before method call

**File:** `ICSharpCode.Decompiler/CSharp/CallBuilder.cs`  
**Location:** Virtual call translation — wherever the receiver is assembled.

**Logic:**

```csharp
// If the receiver is (object)expr, and the called method is ToString/GetHashCode/Equals
// (virtual, inherited from object), strip the (object) cast.
if (receiver is CastExpression cast
    && cast.Type is PrimitiveType { Keyword: "object" }
    && IsMethodInheritedFromObject(calledMethod))
{
    receiver = cast.Expression.Clone();
}
```

`IsMethodInheritedFromObject` checks `calledMethod.DeclaringType.IsKnownType(KnownTypeCode.Object)` or that the method chain reaches `System.Object`.

### Risk for Issue 3

Low for 3b and 3c. 3a is slightly higher risk because it relies on assignability logic — test against interface casts (e.g. the `IWeakEvent<T>` example) to ensure `IsAssignableTo` correctly returns true for interface implementations.

---

## Issue 4 — XAML/BAML decompilation

### 4a — Property-element wrappers not inlined as attributes

**File:** `ICSharpCode.BamlDecompiler/Rewrite/MarkupExtensionRewritePass.cs`  
**Method:** `RewriteElement`

**Root cause:** `RewriteElement` has this guard at the top:

```csharp
if (elem.Name != key)
{
    if (property == null || type == null)
        return false;
    ...
}
```

The `type` comes from `parent.Annotation<XamlType>()`. When the parent is a property element like `<UIElement.Visibility>`, it has a `XamlProperty` annotation but *no* `XamlType` annotation — so `type == null` and the method returns `false` before checking the child.

**Fix:**

Extend `RewriteElement` with a second pass that checks: if `elem` is a property element (has a `XamlProperty` annotation) containing exactly one child that *is* a markup extension, promote the inlined extension to an attribute on `parent` (the grandparent element).

```csharp
bool RewriteElement(XamlContext ctx, XElement parent, XElement elem)
{
    // ... existing logic (handles direct markup extension children) ...

    // NEW: handle the case where elem is a property element
    // (e.g. <UIElement.Visibility>) wrapping a single markup extension child.
    var elemProperty = elem.Annotation<XamlProperty>();
    if (elemProperty != null && type == null)
    {
        // elem is a property element; check its single child
        if (elem.Elements().Count() == 1
            && !elem.Attributes().Any(a => a.Name.Namespace != XNamespace.Xmlns))
        {
            var innerValue = elem.Elements().Single();
            if (CanInlineExt(ctx, innerValue))
            {
                var ext = InlineExtension(ctx, innerValue);
                if (ext != null)
                {
                    var grandparentType = parent.Annotation<XamlType>();
                    if (grandparentType != null)
                    {
                        var extValue = ext.ToString(ctx, parent);
                        var attrName = elemProperty.ToXName(ctx, parent,
                            elemProperty.IsAttachedTo(grandparentType));
                        if (!parent.Attributes(attrName).Any())
                        {
                            parent.Add(new XAttribute(attrName, extValue));
                        }
                        elem.Remove();
                        return true;
                    }
                }
            }
        }
    }

    return false;
}
```

**`Extension` suffix stripping:** This already happens inside `XamlExtension.ToString` — the `typeName.EndsWith("Extension")` check strips the suffix when building the `{...}` string. No changes needed in `MarkupExtensionRewritePass` itself for this.

### 4b — Root element replacement and attribute normalization

**File:** `ICSharpCode.BamlDecompiler/Rewrite/XClassRewritePass.cs`  
**Method:** `RewriteClass`

**Root cause (Example 8):** `RewriteClass` already replaces the element name with the base class and inserts `x:Class`. The remaining failures are:

1. `fields:ViewItemPanel.Name="userControl"` is not converted to `x:Name="userControl"`.
2. `Control.FontFamily="Segoe UI"` keeps its class-qualified prefix instead of being simplified to `FontFamily="Segoe UI"`.
3. `<!--Unknown connection ID: 1-->` comment is left in the output (from `ConnectionIdRewritePass` when no matching field/event is found for a connection ID).

**Fix for (1) and (2)** — add a normalization step at the end of `RewriteClass`:

```csharp
void NormalizeRootAttributes(XamlContext ctx, XElement elem, ITypeDefinition typeDef)
{
    var xNameAttr = ctx.GetKnownNamespace("Name", XamlContext.KnownNamespace_Xaml, elem);
    var attrsToProcess = elem.Attributes().ToList();
    var newAttrs = new List<XAttribute>();

    foreach (var attr in attrsToProcess)
    {
        var localName = attr.Name.LocalName;

        // Pattern: SomeClass.PropertyName="value"
        // If SomeClass matches the resolved type (or a base type), strip the prefix.
        if (localName.Contains('.'))
        {
            int dot = localName.IndexOf('.');
            string className = localName.Substring(0, dot);
            string propName  = localName.Substring(dot + 1);

            // fields:ViewItemPanel.Name → x:Name
            if (propName == "Name"
                && IsClassInHierarchy(typeDef, className))
            {
                newAttrs.Add(new XAttribute(xNameAttr, attr.Value));
                continue;
            }

            // Control.FontFamily → FontFamily (if className is in type hierarchy)
            if (IsClassInHierarchy(typeDef, className))
            {
                newAttrs.Add(new XAttribute(propName, attr.Value));
                continue;
            }
        }

        newAttrs.Add(attr);
    }

    elem.ReplaceAttributes(newAttrs);
}

bool IsClassInHierarchy(ITypeDefinition typeDef, string simpleName)
{
    for (var t = typeDef; t != null; t = t.DirectBaseTypes.FirstOrDefault()?.GetDefinition())
        if (t.Name == simpleName)
            return true;
    return false;
}
```

Call `NormalizeRootAttributes` at the end of `RewriteClass`, after the existing attribute rewriting.

**Fix for (3) — suppress unknown connection ID comments:** In `ConnectionIdRewritePass.ProcessConnectionIds`, change the `!found` branch:

```csharp
// Before:
if (!found)
    element.Add(new XComment($"Unknown connection ID: {annotation.Id}"));

// After: silently discard unmatched IDs
// (they are artifacts of the IComponentConnector.Connect switch
// cases that handle self-registration and default; they are not errors)
```

### Risk for Issue 4

Medium-high. The markup extension inlining (`4a`) touches the core BAML rewrite pipeline. The fix must preserve the existing behaviour for all cases where `RewriteElement` currently works correctly. Key invariant: the new branch only fires when `type == null` (parent is a property element), so it cannot interfere with the existing markup-extension path. Recommend running the full `ILSpy.BamlDecompiler.Tests` suite after each sub-change.

---

## Issue 5 — XAML partial class / duplicate member output

This issue depends on Issues 4a/4b being resolved first (so `XClassNames` and `GeneratedMembers` are correctly populated by the BAML passes).

### 5.1 — Mark class `partial`

**File:** `ICSharpCode.Decompiler/CSharp/ProjectDecompiler/WholeProjectDecompiler.cs`  
**Location:** `WriteCodeFilesInProject` → the `ProcessFiles` lambda → after `decompiler.DecompileTypes(declaredTypes)` is called.

**Logic:**

```csharp
// After BAML decompilation has run and XClassNames is populated,
// walk the resulting SyntaxTree and add 'partial' to any TypeDeclaration
// whose full name is in ctx.XClassNames.
foreach (var typeDecl in syntaxTree.Descendants.OfType<TypeDeclaration>())
{
    var resolvedType = (typeDecl.GetResolveResult() as TypeResolveResult)?.Type;
    if (resolvedType != null && xClassNames.Contains(resolvedType.FullName))
    {
        if (!typeDecl.Modifiers.HasFlag(Modifiers.Partial))
            typeDecl.Modifiers |= Modifiers.Partial;
    }
}
```

`xClassNames` is the `HashSet<string>` accumulated from all BAML decompilation runs during `WriteResourceFilesInProject`.

### 5.2 — Exclude generated members

**File:** Same — `WriteCodeFilesInProject` lambda, or via an `IAstTransform`.

**Logic:** Pass the `GeneratedMembers` set (populated by `ConnectionIdRewritePass`) to a new post-processing transform that removes matching members from the AST:

```csharp
// New IAstTransform: RemoveGeneratedXamlMembers
public class RemoveGeneratedXamlMembers : IAstTransform
{
    readonly HashSet<EntityHandle> generatedMembers;
    public RemoveGeneratedXamlMembers(HashSet<EntityHandle> generatedMembers)
        => this.generatedMembers = generatedMembers;

    public void Run(AstNode rootNode, TransformContext context)
    {
        foreach (var member in rootNode.Descendants
            .OfType<EntityDeclaration>().ToList())
        {
            var token = member.GetSymbol() is IMember m ? m.MetadataToken : default;
            if (!token.IsNil && generatedMembers.Contains(token))
                member.Remove();
        }
    }
}
```

Register this transform in `CreateDecompiler` when the type is in `XClassNames`:

```csharp
if (xClassNames.Contains(typeDef.FullName))
    decompiler.AstTransforms.Add(
        new RemoveGeneratedXamlMembers(generatedMembersForType));
```

Members removed: `InitializeComponent`, `IComponentConnector.Connect`, `_contentLoaded` field, and any field assigned in `Connect` (the `internal BitmapImportSection userControl` example).

### 5.3 — Fix base class reference (fully-qualified)

When a XAML-linked class like `BitmapImportSection : TagCustomSection` is being emitted and `TagCustomSection` is in a *different* namespace but has the same simple name as the declaring type's enclosing scope, the same fix from Issue 1 (`CollidesWithDeclaringScope`) applies. No separate code is needed — Issue 1's fix covers this automatically.

### 5.4 — `((BaseClass)this).Method()` → `base.Method()`

This is already handled by ILSpy's existing `IntroduceExtensionMethods` / `ReplaceMethodCallsWithOperators` transform chain (specifically the `base.` rewriting transform). Once the class is correctly marked `partial` with the right base class (fixes 5.1 and 5.3), the `((TagCustomSection)this).Close()` pattern should automatically resolve to `base.Close()`. No new code expected; verify by running the test case.

### Risk for Issue 5

Medium. The `partial` modifier addition is low-risk AST mutation. The member exclusion transform needs to be robust against `MetadataToken` being `nil` for synthesized members. The `EntityHandle` set must be thread-safe if `ProcessFiles` runs in parallel (use `ConcurrentDictionary` or copy to an immutable `HashSet` per decompilation job).

---

## Implementation Order

The recommended sequence minimises risk and enables incremental testing at each step:

| Step | Issue | Why this order |
|------|-------|---------------|
| 1 | 3b — `(T)null` cast | Smallest change, no dependencies |
| 2 | 3c — `((object)x).Method()` | Small, isolated to CallBuilder |
| 3 | 3a — `(T)(object)x` double-cast | Slightly larger, test IWeakEvent cases |
| 4 | 1 — Name-collision short-naming | Isolated to TypeSystemAstBuilder |
| 5 | 2a — `[Flags]` `1 << N` notation | Enum formatting, low blast radius |
| 6 | 2b — `[Flags]` OR-chains | Depends on 2a infrastructure |
| 7 | 2d — Inline `(EnumType)intLiteral` | Reuses 2b logic |
| 8 | 2c — Switch discriminant restore | Larger change to StatementBuilder |
| 9 | 4a — XAML property-element inlining | Run BamlDecompiler tests after |
| 10 | 4b — Root element normalisation + comment removal | Same test suite |
| 11 | 5.1 + 5.2 — Partial class + member exclusion | Depends on 4a/4b being stable |
| 12 | 5.3 / 5.4 — Verify base-class ref + base. call | Verification only, no new code |

---

## File Edit Summary

| File | Issue(s) |
|------|----------|
| `ICSharpCode.Decompiler/CSharp/Syntax/TypeSystemAstBuilder.cs` | 1, 2a, 2b, 3a, 3b |
| `ICSharpCode.Decompiler/CSharp/ExpressionBuilder.cs` | 2d, 3b, 3c |
| `ICSharpCode.Decompiler/CSharp/StatementBuilder.cs` | 2c |
| `ICSharpCode.Decompiler/CSharp/CallBuilder.cs` | 3c |
| `ICSharpCode.Decompiler/CSharp/ProjectDecompiler/WholeProjectDecompiler.cs` | 5.1, 5.2 |
| `ICSharpCode.BamlDecompiler/Rewrite/MarkupExtensionRewritePass.cs` | 4a |
| `ICSharpCode.BamlDecompiler/Rewrite/XClassRewritePass.cs` | 4b |
| `ICSharpCode.BamlDecompiler/Rewrite/ConnectionIdRewritePass.cs` | 4b (comment suppression) |
| *(new file)* `ICSharpCode.Decompiler/CSharp/Transforms/RemoveGeneratedXamlMembers.cs` | 5.2 |

---

## Testing Checkpoints

For each step above, the following tests should be run before moving on:

- **Issues 1–3:** `ICSharpCode.Decompiler.Tests` project — specifically `CSharpDecompilerTests` and `TypeSystemAstBuilderTests` if they exist. Add new round-trip test fixtures mirroring the examples in the `.context/issues/` documents.
- **Issue 2 (enums):** Add test assemblies containing `AssetBrowserViewFlags`, `TagFieldType`, and `ArrowEnds` enum examples. Assert the emitted text matches the expected `1 << N` and OR-chain forms.
- **Issues 4–5:** `ILSpy.BamlDecompiler.Tests` project. All eight XAML examples from `.context/issues/xaml-decompilation.md` should be used as reference output fixtures.
- **Issue 5:** Create a test assembly with a `BitmapImportSection`-style class linked to a `.xaml`. Verify `partial`, no duplicate members, correct base class, and `base.Close()`.

---

## Key Invariants to Preserve

1. `ConvertTypeHelper` must never produce *incorrect* output — only more verbose. The collision check may over-qualify (emit `Ns.Foo` when `Foo` would have been fine), but it must never produce a short name that would bind to the wrong type.
2. The enum decomposition (2b/2d) must fall back to a raw integer literal if the greedy decomposition leaves a non-zero remainder. This preserves semantic correctness.
3. The BAML rewrite passes are idempotent and run in a fixed sequence. The new code in `4a` must not break the existing `ProcessElement` recursion by modifying the element tree in a way that confuses subsequent iterations.
4. `RemoveGeneratedXamlMembers` must be scoped to types that appear in `XClassNames`. Applying it globally would incorrectly remove synthesized members from non-XAML classes.