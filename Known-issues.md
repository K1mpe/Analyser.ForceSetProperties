# Known Issues

Confirmed gaps in the current analyzer, found while stress-testing it against records and
structs. Most entries were reproduced directly against `ForceSetPropertiesDriver`/the real
generator (not just reasoned about), so the example is a fact, not a guess - entries reported from
manual testing rather than an automated repro are marked as such. None of these are fixed yet -
this file exists to track them until they are.

Status legend: 🔴 open (not started) · 🟡 in progress / low severity · ✅ fixed & validated

## Priority order

Ordered by how much real impact each one has (most important first) - not by issue number, which
just reflects discovery order. Work down from the top.

| # | Issue | Status |
| - | ----- | ------ |
| 13 | Assigning a property's private backing field directly is never recognized as satisfying that property | 🔴 open |
| 9  | The generic attribute form (`ForceSetProperties<T>`) is never discovered by the real generator | ✅ fixed |
| 2  | Positional records / record structs are never recognized as fully set | ✅ fixed |
| 3  | `with`-expressions produce false positives for untouched properties | ✅ fixed |
| 8  | No error when a destination type has zero assignable properties | ✅ fixed |
| 6  | Inherited properties from a base class are skipped - only direct properties are checked | ✅ fixed |
| 1  | Async methods (`Task<T>` / `ValueTask<T>`) don't validate the right type | ✅ fixed |
| 10 | Implicit inference through an unrecognized generic type validates the wrong thing | ✅ fixed |
| 5  | Interface destination types don't meaningfully validate anything | ✅ fixed |
| 12 | Applying the attribute to a method with no body always reports every property as missing | ✅ fixed |
| 4  | `[method: ForceSetProperties]` on a record primary constructor is rejected as unsupported | ✅ fixed |
| 11 | Combining any two of the three ways to specify a destination type doesn't merge or error | ✅ fixed |
| 14 | Listing the same type twice in `Types = [...]` produces duplicate diagnostics | ✅ fixed |
| 7  | No rule catalog shown in Solution Explorer (Dependencies > Analyzers) | ✅ fixed |

Reasoning for the top of the list: #13 is now the only open issue left - assigning a manually-backed
property's own backing field directly is a common pattern that silently fails validation with no
error. (#2, #3, #9, #6, #1, #10, #5, #12, #4, #11, #14, and now #7, formerly at or near the top of
this list, are fixed - see below.)

---

## ✅ 1. Async methods (`Task<T>` / `ValueTask<T>`) don't validate the right type

**Fixed, together with issue #10 (both are the same `UnwrapDelegateReturnType` code path).**
`UnwrapDelegateReturnType` (`Services/ContextBuilder.cs`) now also unwraps
`System.Threading.Tasks.Task<T>` and `System.Threading.Tasks.ValueTask<T>`, the same way it already
unwrapped `Expression<TDelegate>`/`Func<...>` - recursively, so `Task<Func<T>>`-shaped nesting still
resolves down to `T`.

```csharp
[ForceSetProperties]
public async Task<DtoModel> CreateAsync()
{
    await Task.Yield();
    return new DtoModel(); // completely empty
}
```

Previously: `FSP005 Error: ForceSetProperties found no assignable properties on 'Task' to
validate` (before FSP005 existed: a silently-passing `FSP201` against `Task` itself) - checking the
wrong type either way. Now: `FSP001: Property 'Name' must be initialized on 'DtoModel'` - the
correct type, with the correct result. A fully-populated `DtoModel` now reports `FSP201` against
`DtoModel`, not `Task`.

**Implemented in:** `ContextBuilder.UnwrapDelegateReturnType` and the new `ContextBuilder.IsTaskType`
(`Services/ContextBuilder.cs`).

**Validated by:** `UnwrapDelegateReturnTypeTests` (`Task<T>`, `ValueTask<T>`, and a non-generic
`Task` left unchanged) and `IsTaskTypeTests`, plus the end-to-end
`IntegrationTests/CompileErrorsTests.AsyncMethodInferringTaskAsTheDestinationType_ValidatesTheWrappedTypeInstead`
(rewritten - it used to assert the `FSP005`-against-`Task` bug as correct).

---

## ✅ 2. Positional records / record structs are never recognized as fully set

**Fixed, together with issue #3 (both are record-specific).** When tracing into a constructor call,
`PropertyAssignmentScanner` now recognizes a record/record struct's primary constructor specifically
(`IsPositionalRecordConstructor` - true when the constructor's declaring syntax is the type
declaration itself, `RecordDeclarationSyntax`, rather than a dedicated constructor declaration) and
maps each call-site argument to the correspondingly-named property directly
(`MarkPositionalConstructorArguments`/`ResolveParameter`, matching by `NameColon` for named
arguments or by position otherwise) - rather than trying to trace into a constructor body that never
existed in source to begin with.

```csharp
public record Person(string Name, int Age);

[ForceSetProperties]
public Person Create(string name, int age)
{
    return new Person(name, age);
}
```

Previously: `FSP002: Name, Age must be initialized`, even though the primary constructor sets both.
Now: `FSP201`. Also fixed for `record struct`, and for reaching the primary constructor via chaining
(`public Person(string name) : this(name, 0)`). A property outside the positional parameter list is
still validated normally - adding one without setting it still correctly fails.

Confirmed **not** broken by this fix (regression-tested): a record with explicit settable properties
and no positional constructor still validates the same way it always did; an ordinary (non-record)
class whose constructor parameter happens to share a property's name, but never actually assigns it,
still correctly fails - the fix is scoped to genuine record primary constructors only.

**Implemented in:** `PropertyAssignmentScanner.cs` (`IsPositionalRecordConstructor`,
`MarkPositionalConstructorArguments`, `ResolveParameter`; `FindTraceableCallees` now also returns
each callee's call-site `ArgumentListSyntax`, needed to read the actual arguments passed).
Documented in the README under a new "Records" section.

**Validated by:** `IsPositionalRecordConstructorTests`, `ResolveParameterTests`, and
`IntegrationTests/RecordSupportTests` (positional record and record struct, additional non-positional
property still required, chained constructor, and the two regression checks above).

---

## ✅ 3. `with`-expressions produce false positives for untouched properties

**Fixed, together with issue #2 (both are record-specific).** `PropertyAssignmentScanner` now has a
dedicated `MarkWithExpressionCopiedProperties` step: for every `with` expression found (whose own
type exactly matches the destination type being validated), every required property *not* named in
the `with { ... }` initializer is marked set - annotated `(via with)` - since it's provably copied
from the source instance rather than missing.

```csharp
public record Person
{
    public string Name { get; init; }
    public int Age { get; init; }
}

[ForceSetProperties]
public Person Rename(Person existing, string name)
{
    return existing with { Name = name }; // Age intentionally preserved from existing
}
```

Previously: `FSP001: Property 'Age' must be initialized`, even though `Age` is legitimately carried
over from `existing`. Now: `FSP201`.

Confirmed **not** broken by this fix (regression-tested): a `with` expression on an *unrelated* type
elsewhere in the same method does not satisfy any property on the actual destination type - the
exact-type check prevents that.

**Implemented in:** `PropertyAssignmentScanner.MarkWithExpressionCopiedProperties`.

**Validated by:** `IntegrationTests/RecordSupportTests.WithExpression_PropertiesNotMentioned_...`
and `WithExpression_OnAnUnrelatedType_...` (the regression check above).

---

## ✅ 4. `[method: ForceSetProperties]` on a record primary constructor is rejected as unsupported

**Fixed - by a different mechanism than originally proposed below, discovered while implementing
it (see the "Update" note).**

```csharp
[method: ForceSetProperties]
public record Person(string Name, int Age)
{
    public Person() : this(default, default) { }
}
```

Previously: `FSP003: ForceSetProperties can only be applied to constructors, methods, or
properties; this target is not yet supported` — even though `method:` is the syntactically correct
C# way to target a record's primary constructor with an attribute. Now: `FSP201` (both `Name` and
`Age` are set via the `: this(default, default)` chain into the primary constructor - the same
positional-constructor-argument mechanism issue #2 fixed).

**Original "why"/"likely fix" (kept for context - superseded by the Update below):** the syntax
node the attribute is attached to is the `RecordDeclarationSyntax` itself (a primary constructor
has no dedicated `ConstructorDeclarationSyntax` of its own), which never matched
`IsSupportedDeclaration`'s node-kind check regardless of what the attribute target actually
resolves to. The originally-proposed fix was to also accept a `RecordDeclarationSyntax` target node
in `Classify` whenever `ctx.TargetSymbol` (from the incremental pipeline's
`GeneratorAttributeSyntaxContext`) resolves to a constructor.

**Update - the incremental pipeline needed the same kind of workaround as issue #9, not a tweak to
`Classify`:** verified directly (via a `CSharpGeneratorDriver`-based repro, the same way issue #9's
fix had to be verified), `ForAttributeWithMetadataName`'s predicate never even *fires* for a
`method:`-targeted attribute on a record's primary constructor - so `Classify` never runs for it at
all, regardless of what it checks. `Classify` was never the place to fix this. The manual
`ForceSetPropertiesTargetFinder.FindTargets` path (used by `ForceSetPropertiesDriver.GetDiagnostics(Compilation)`
directly, and by most of this repo's tests) had an equivalent, separate gap: it only visits nodes
matching `IsSupportedDeclaration`, which a `RecordDeclarationSyntax` never does either, and even if
it did, `GetDeclaredSymbol` on a record declaration returns the *type* symbol, not the primary
constructor the `method:` attribute actually attaches to - so this path would have silently found
nothing at all (worse than the original `FSP003`, not better) if only the incremental side had been
patched.

**Implemented in, two parts (mirroring how issue #9 solved an analogous discovery gap):**
1. **Incremental pipeline:** a new syntactic provider,
   `IsMethodTargetedRecordConstructorAttributeSyntax` / `ClassifyMethodTargetedRecordConstructor`
   (`Services/ForceSetPropertiesTargetFinder.cs`), discovered by simple name the same way the
   generic attribute form is - it resolves the primary constructor directly via
   `GetPrimaryConstructor` (new) instead of ever depending on `ForAttributeWithMetadataName`
   handing back a usable `ctx.TargetSymbol` for this case.
2. **Manual/non-incremental path:** `FindTargets` now also visits a `RecordDeclarationSyntax` node
   directly, resolving its primary constructor (`GetPrimaryConstructor`) and searching *that*
   symbol's own attributes, rather than only ever considering nodes `IsSupportedDeclaration` already
   accepts. `IsOnSupportedDeclaration`/`FindUnsupportedAttributeUsages` (used together with
   `FindTargets`) needed the matching syntactic recognition too
   (`IsMethodTargetedOnRecordWithPrimaryConstructor`), so the two don't disagree and double-report.

A plain `[ForceSetProperties]` on a record with no `method:` target specifier is unaffected -  that
still targets the record type itself, same as a class, and is still rejected (`FSP003`) exactly as
before. A `method:`-targeted attribute on a record with no primary constructor at all (nothing for
`method:` to target) is unaffected too, and still rejected.

**Implemented in:** `ForceSetPropertiesTargetFinder.GetPrimaryConstructor`,
`IsMethodTargetedOnRecordWithPrimaryConstructor`, `FindTargets`, `IsOnSupportedDeclaration`,
`CreateIncrementalProvider`, `IsMethodTargetedRecordConstructorAttributeSyntax`,
`ClassifyMethodTargetedRecordConstructor` (all in `Services/ForceSetPropertiesTargetFinder.cs`).

**Validated by:** `GetPrimaryConstructorTests`, `IsMethodTargetedOnRecordWithPrimaryConstructorTests`,
new cases in `FindUnsupportedAttributeUsagesTests`, the end-to-end
`IntegrationTests/AttributeTargetsTests` (fully-set via the manual driver, a genuinely-missing
extra property, and the no-`method:`-target regression check), and
`ForceSetAnalyserTests/InitializeTests.MethodTargetedRecordPrimaryConstructor_IsValidatedThroughTheRealIncrementalPipeline`
(through the real `CSharpGeneratorDriver` - the only path that could have caught the incremental-
pipeline-specific gap described in the Update above).

**Second update - the fix above still wasn't the whole story:** flagged directly against
`Example/ExampleService.cs`, which fails to compile with `FSP002` on a plain (non-`method:`) method
that returns a positional record built via target-typed `new(...)`:

```csharp
public record TestRecord(string Name, int Age)
{
    public string? Description { get; set; }
}

[ForceSetProperties]
public static TestRecord Record(string name, string description)
{
    return new(name, 10) // target-typed `new` - no type name before the parens
    {
        Description = description
    };
}
```

This isn't a `method:`-targeting problem at all - it reproduces on any constructor call, positional
record or not. `PropertyAssignmentScanner.FindTraceableCallees` only ever searched for
`ObjectCreationExpressionSyntax` (`new TypeName(...)`), never
`ImplicitObjectCreationExpressionSyntax` (target-typed `new(...)`, introduced in C# 9) - so a call
through the latter was never traced at all: `Name`/`Age` (positional constructor arguments) were
never marked set, even though `Description` (a plain object-initializer assignment, found by a
different, unrelated scan step) correctly was. Previously: `FSP002: The following properties on
'TestRecord' must be initialized: Name, Age`. Now: `FSP201` - `Name` and `Age` via `.ctor`,
`Description` via the object initializer.

**Fixed in:** `PropertyAssignmentScanner.FindTraceableCallees` now searches for
`BaseObjectCreationExpressionSyntax` (the common base of both `ObjectCreationExpressionSyntax` and
`ImplicitObjectCreationExpressionSyntax`) instead of `ObjectCreationExpressionSyntax` alone -
`GetSymbolInfo` already resolves either syntax shape to the exact same constructor symbol, so
nothing else about the tracing logic needed to change.

**Validated by:** `FindTraceableCalleesTests.TargetTypedNewConstructorCall_IsReturned` (ordinary,
non-record class), `WhatCountsAsSetTests.ObjectInitializerViaTargetTypedNew_CountsAsSet`, and
`RecordSupportTests.PositionalRecord_ConstructedViaTargetTypedNew_ValidatesEachPositionalPropertyAndTheAdditionalOne`
(the exact shape flagged in `ExampleService.cs`).

---

## ✅ 5. Interface destination types don't meaningfully validate anything

**Fixed, both parts:**

```csharp
public interface IDto
{
    string Name { get; set; }
    int Age { get; set; }
}

public class DtoModel : IDto
{
    public string Name { get; set; }
    public int Age { get; set; }
}

[ForceSetProperties<IDto>]
public DtoModel Create()
{
    return new DtoModel(); // Name and Age both left unset
}
```

Previously: a silent `FSP201` even though both properties were left unset (or `FSP005` for the
empty-interface flavor, as a side effect of issue #8). Now: `FSP002: The following properties on
'IDto' must be initialized: Name, Age` - plus a new `FSP101` warning (see below) on every usage
with an interface destination type, since this one is *still* pointed at `IDto`.

1. **`ContextBuilder.ResolveRequiredProperties` now resolves interface properties correctly.**
   `GetTypeHierarchy` (new) walks the `BaseType` chain for a class/struct exactly as issue #6 fixed
   it, but for an interface it instead yields the interface itself plus every interface it extends
   (`INamedTypeSymbol.AllInterfaces`) - interfaces have no `BaseType` to walk. `IDto : IHasName` now
   correctly requires `IHasName`'s properties too, several levels deep if needed.
2. **`PropertyAssignmentScanner.MarkIfRequiredProperty` now matches implementations, not just
   identical symbols.** The new `SatisfiesRequiredProperty` still checks raw symbol equality first,
   but when the required property's containing type is an interface, it additionally checks whether
   the assignment's left-hand property is that interface member's implementation on its own
   containing type (`INamedTypeSymbol.FindImplementationForInterfaceMember`) - covering both
   implicit (`public string Name { get; set; }`) and explicit (`string IDto.Name { get; set; }`)
   interface implementations.
3. **New `FSP101` warning, always raised alongside whatever else is reported, whenever the
   destination type is an interface** - unlike `FSP004`/`FSP006`, this doesn't gate context
   building; it's an additive diagnostic, since the interface is still validated (parts 1 and 2 make
   sure of that), just with a caveat worth surfacing: only what the interface itself (and what it
   extends) declares is checked, not the full concrete type actually being constructed. Message:
   `ForceSetProperties is validating against the interface 'IDto'; only properties declared on
   'IDto' (or an interface it extends) are checked, so properties on the concrete type actually
   being constructed that aren't part of 'IDto' are not verified. Consider specifying the concrete
   destination type instead.`

Confirmed **not** broken by this fix (regression-tested): a non-interface destination type never
produces the new `FSP101` warning; an interface property implemented on an unrelated type (not the
one actually being constructed) still doesn't satisfy the requirement.

**Implemented in:** `ContextBuilder.GetTypeHierarchy`/`ResolveRequiredProperties`
(`Services/ContextBuilder.cs`), `PropertyAssignmentScanner.SatisfiesRequiredProperty`
(`Services/PropertyAssignmentScanner.cs`), `DiagnosticDescriptors.InterfaceDestinationType`
(`FSP101`, warning severity - the `FSP1xx` range is reserved for warnings, alongside the `FSP0xx`
error and `FSP2xx` informational ranges), `MessageBuilder.BuildInterfaceDestinationTypeWarningDiagnostic`,
wired into both branches of `ForceSetPropertiesDriver.GetDiagnostics` (the generic-form path and the
normal `AttributeData` path).

**Validated by:** `ResolveRequiredPropertiesTests` (interface's own properties, an extended
interface's properties, multiple levels deep), `SatisfiesRequiredPropertyTests` (same symbol,
implicit implementation, explicit implementation, unrelated type, non-interface required property),
`MarkIfRequiredPropertyTests.AssignmentOnTheConcreteImplementingType_SatisfiesAnInterfaceDestinationTypesProperty`,
`BuildInterfaceDestinationTypeWarningDiagnosticTests`, and the end-to-end
`IntegrationTests/InterfaceDestinationTypeTests` (fully-set, missing property, property from an
extended interface, the warning always appearing, and the regression check above).

---

## ✅ 6. Inherited properties from a base class are skipped - only direct properties are checked

**Fixed.** `ContextBuilder.ResolveRequiredProperties` now walks the `BaseType` chain (`for (var t =
destinationType; t != null; t = t.BaseType) ...`) and collects properties from every level, not just
`destinationType` itself, instead of relying on `destinationType.GetMembers()` alone (which only
returns members **declared directly** on that type).

```csharp
public class BaseModel
{
    public string Id { get; set; }
}

public class DtoModel : BaseModel
{
    public string Name { get; set; }
}

[ForceSetProperties]
public DtoModel Create()
{
    return new DtoModel { Name = "x" }; // Id (inherited) is now required
}
```

Previously: `FSP201`, with `Id` never part of the required set at all. Now: `FSP001: Property 'Id'
must be initialized`.

**Overridden properties are de-duplicated**, not double-counted: before collecting required
properties, every property's `OverriddenProperty` chain (walked all the way to its root virtual
declaration) is recorded, and any base-class member found in that set is skipped when walking the
`BaseType` chain - an `override` in `DtoModel` is what gets checked, not the original virtual
property on `BaseModel` as well.

Accessibility is unaffected by the fix: `IsRequiredProperty`'s existing accessibility check still
runs per base-class member exactly as it does for direct members, so a `private`-setter property on
the base class is still correctly excluded when accessed from outside the base class (and still
included when the access context is the base class itself).

Confirmed **not** broken by this fix (regression-tested): a property with no base class at all
still resolves exactly as before; a property inherited two levels up the base class chain is found
just as reliably as one level up.

**Implemented in:** `ContextBuilder.ResolveRequiredProperties` (`Services/ContextBuilder.cs`).

**Validated by:** `ResolveRequiredPropertiesTests` (base-class property included alongside direct
ones, a property two levels up the chain, an overridden property not duplicated, and the two
private-setter accessibility cases) and `IntegrationTests/InheritedPropertiesTests` (inherited
property left unset reports `FSP001`, setting it too reports `FSP201`, a private setter on the base
class excluded from outside the base class, and an overridden property not double-reported).

---

## ✅ 7. No rule catalog shown in Solution Explorer (Dependencies > Analyzers), unlike a normal analyzer package

**Fixed, both parts, plus the `EnforceExtendedAnalyzerRules` property the RS1036 warning itself
recommended.**

Expanding `Microsoft.CodeAnalysis.NetAnalyzers` under a project's Dependencies > Analyzers node in
Solution Explorer shows every rule it can produce - ID, title, description, default severity - with
per-rule severity controls. Expanding `Analyser.ForceSetProperties` showed nothing like that, since
`ForceSetAnalyser` is purely an `IIncrementalGenerator` - that interface has no
`SupportedDiagnostics`-equivalent concept for Visual Studio to enumerate, unlike a
`DiagnosticAnalyzer`.

Previously, every build in this repo carried these warnings:
```
warning RS1036: 'Analyser.ForceSetProperties.ForceSetAnalyser': A project containing analyzers or
source generators should specify the property '<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>'

warning RS2008: Enable analyzer release tracking for the analyzer project containing rule 'FSP001'
(and FSP002, FSP003, FSP004, FSP201)
```
Now: neither warning appears, and Solution Explorer's Dependencies > Analyzers >
`Analyser.ForceSetProperties` node lists the full rule catalog - FSP001, FSP002, FSP003, FSP004,
FSP005, FSP006, FSP007, FSP101, and FSP201, with their titles and default severities - the same way
`Microsoft.CodeAnalysis.NetAnalyzers` does. (The rule list naturally grew since this entry was first
written - issues #10, #11, and #5 added FSP006, FSP007, and FSP101 in the meantime; all three are
included below, not just the FSP001/002/003/004/005/201 originally listed.)

**Implemented, three parts:**
1. **`ForceSetPropertiesRuleCatalog`** (`ForceSetPropertiesRuleCatalog.cs`) - a small companion
   `DiagnosticAnalyzer` in the same assembly whose only job is to declare `SupportedDiagnostics`
   (all nine current descriptors) with a no-op `Initialize` (just
   `EnableConcurrentExecution`/`ConfigureGeneratedCodeAnalysis`, standard analyzer boilerplate - no
   analysis actions registered, so it never itself reports anything). `ForceSetAnalyser` keeps
   reporting the real diagnostics exactly as before; this class exists purely to advertise the rule
   catalog to the IDE.
2. **`AnalyzerReleases.Shipped.md`/`AnalyzerReleases.Unshipped.md`** (standard
   `Microsoft.CodeAnalysis.Analyzers` convention, read by its own RS2008 meta-analyzer) - all nine
   rules are listed in `Unshipped.md` under "New Rules" (nothing has actually shipped as a released
   package version yet, so `Shipped.md` is the empty/header-only file the convention expects when
   there's no prior release to list).
3. **`<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>`** added to
   `Analyser.ForceSetProperties.csproj`, exactly as RS1036 itself recommended - clears that warning
   with no other changes needed (the project already satisfied every rule this flag enforces).

**Confirmed via the CLI** (rebuilt with zero warnings, zero errors, RS1036/RS2008 both gone) - the
Solution Explorer node itself is a Visual Studio UI detail that can't be checked from this
environment, but it reads directly from `SupportedDiagnostics`, the same mechanism verified by
`SupportedDiagnosticsTests` below.

**Implemented in:** `ForceSetPropertiesRuleCatalog.cs` (new),
`Analyser.ForceSetProperties.csproj` (`EnforceExtendedAnalyzerRules`), `AnalyzerReleases.Shipped.md`
(new), `AnalyzerReleases.Unshipped.md` (new).

**Validated by:** `ForceSetPropertiesRuleCatalogTests/SupportedDiagnosticsTests` (contains exactly
the nine current rule IDs, no duplicates) and `ForceSetPropertiesRuleCatalogTests/InitializeTests`
(runs the real analyzer through `Compilation.WithAnalyzers`, confirming it reports nothing on its
own - the generator is still what reports the real diagnostics).

---

## ✅ 8. No error when a destination type has zero assignable properties

Added `FSP005`: an error raised whenever a destination type resolves successfully (it's not
`void`/`object`/`dynamic`, so it doesn't hit FSP004) but has zero properties the annotated code can
actually see and set. Previously this case fell through to `IsFullySet` being vacuously true (every
property in an empty list is trivially "set"), producing a misleading `FSP201 Info: ... validated`
with an empty property list - easy to mistake for a real guarantee, and the exact mechanism behind
issue #1's silent pass.

```csharp
public class EmptyDto
{
}

[ForceSetProperties]
public EmptyDto Create()
{
    return new EmptyDto();
}
```

Now reports: `FSP005 Error: ForceSetProperties found no assignable properties on 'EmptyDto' to
validate; check that this is the intended destination type`.

**Implemented in:** `DiagnosticDescriptors.NoPropertiesFound` and
`MessageBuilder.BuildDiagnostic`/`BuildNoPropertiesFoundDiagnostic`. Documented in the README's
"What triggers a compile error" section and rules table.

**Validated by:** `BuildNoPropertiesFoundDiagnosticTests`, the new case in `BuildDiagnosticTests`,
and `CompileErrorsTests.DestinationTypeWithNoAssignableProperties_RaisesFSP005` /
`AsyncMethodInferringTaskAsTheDestinationType_RaisesFSP005InsteadOfSilentlyPassing`.

**Note:** this only catches the *symptom* for issues #1 and #5 (a silent vacuous pass now becomes a
visible error) - it does not fix either issue's actual root cause (Task/ValueTask unwrapping,
interface member resolution), which remain open above.

---

## ✅ 9. The generic attribute form (`ForceSetProperties<T>`) is never discovered by the real generator - not just "unenforced"

**Fixed.** The generic form is now discovered syntactically instead of via
`ForAttributeWithMetadataName`, which never worked for it (see below) - `ForceSetPropertiesAttribute`
usages are found the normal way; `ForceSetPropertiesAttribute<T>` usages are found by matching
`AttributeSyntax` nodes whose `Name` is a `GenericNameSyntax` named `"ForceSetProperties"` or
`"ForceSetPropertiesAttribute"`, with the type argument and decorated declaration resolved directly
via the semantic model - neither of which needs the attribute's *own* type to resolve.

Originally confirmed with a `CSharpGeneratorDriver` repro mirroring `Example/ExampleService.cs`:
before this fix, two methods each using `[ForceSetProperties<T>]` produced **zero** diagnostics for
either one through the real generator - not "unenforced with a passing 101," nothing at all. (The
manual `ForceSetPropertiesDriver.GetDiagnostics(Compilation)` path used by most of this test suite
never reproduced this, since it re-scans the whole compilation directly instead of going through the
incremental discovery - which is why fixing/validating this had to go through `CSharpGeneratorDriver`
specifically, the same way `ForceSetAnalyserTests/InitializeTests.cs` does.)

**Implemented in:** `ForceSetPropertiesTargetFinder.CreateIncrementalProvider` (new
`IsGenericAttributeSyntax` predicate + `ClassifyGeneric` transform, combined with the existing
non-generic `ForAttributeWithMetadataName` results), `Models/AttributeUsage.cs` (new
`SupportedGeneric` factory carrying an already-resolved destination type + attribute location instead
of `AttributeData`), `ContextBuilder.BuildSingle` (builds a context from that pre-resolved type,
bypassing `AttributeData`-based resolution), and `ForceSetPropertiesDriver.GetDiagnostics` (branches
on `AttributeUsage.GenericDestinationType` to use the new path). The non-generic path is completely
unaffected.

**Validated by:** `InitializeTests.GenericAttributeForm_IsValidatedOnASupportedLanguageVersion`
(fully-set and missing-property cases, through the real `CSharpGeneratorDriver`) and
`GenericAttributeForm_OnAnUnsupportedPlacement_RaisesFSP003`. The old
`GenericAttributeForm_CompilesButIsNotValidated_OnASupportedLanguageVersion` test - which asserted
the broken behavior as correct - was rewritten.

**Side effect worth knowing about:** this changed which source wins in issue #11's first repro
(generic argument vs. named `Types`) - see the update there.

---

## ✅ 10. Implicit inference through an unrecognized generic type validates the wrong thing - generalizes issue #1

**Fixed, together with issue #1 - two parts, priority order matters, exactly as designed:**

1. **Known-safe structural unwraps go first** - issue #1's fix to `UnwrapDelegateReturnType`
   extends its existing `Expression<TDelegate>`/`Func<...>` special-casing to also unwrap `Task<T>`
   and `ValueTask<T>`. These are safe to unwrap unconditionally: there is no legitimate scenario
   where "validate `Task<T>` itself" was the intent.
2. **Everything else generic reached through *implicit inference* is now an error**, not a silent
   validation of the wrapper. `List<T>`, `Dictionary<TKey, TValue>`, a custom `Result<T>`/`Maybe<T>`
   wrapper, etc. are genuinely ambiguous - "validate the container" and "validate what it wraps" are
   both plausible readings, and there's no safe default to pick silently:

```csharp
[ForceSetProperties]
public static List<DatabaseModel> CreateClone(DatabaseModel model)
{
    return new List<DatabaseModel> { model };
}
```

Previously: `FSP001: Property 'Capacity' must be initialized on 'List'` - technically correct
(`List<T>.Capacity` genuinely wasn't set), but not what anyone means by this attribute; the intent
was clearly to validate `DatabaseModel`, not `List<DatabaseModel>`'s own unrelated members. Now:
`FSP006: ForceSetProperties inferred 'List<DatabaseModel>' as the destination type, but it's
generic and its properties may not be what you intend to validate; specify the destination type
explicitly ... to confirm 'List<DatabaseModel>' is really what you mean`.

Explicitly stating the destination type is **not** restricted by this at all -
`[ForceSetProperties(typeof(List<DatabaseModel>))]` or `[ForceSetProperties<List<DatabaseModel>>]`
still validates `List<DatabaseModel>` exactly as before, no questions asked - confirmed
not broken by this fix (regression-tested). The new error only fires when `ResolveDestinationTypes`
falls all the way through to the `InferDestinationType` branch (no explicit `Types=`, no
`typeof(...)`, no generic `<T>` attribute argument) *and* what inference lands on - after the
known-safe unwraps from step 1 have already run - is still a generic type. An async method
inferring `Task<DtoModel>` reports `FSP001`/`FSP201` against `DtoModel` (per issue #1's fix), never
`FSP006`, since `Task<T>` is unwrapped before this check ever runs.

**New diagnostic:** `FSP006` (`DiagnosticDescriptors.AmbiguousGenericInference`, error severity).

**Implemented in:** `ContextBuilder.IsAmbiguousGenericInference` and
`ContextBuilder.GetAmbiguousInferredDestinationTypes` (`Services/ContextBuilder.cs`, mirroring the
existing `IsUnsupportedDestinationType`/`GetUnsupportedDestinationTypes` pattern used for `FSP004`),
wired into `ContextBuilder.Build` (skips building a context for an ambiguous type, same as an
unsupported one) and `ForceSetPropertiesDriver.GetDiagnostics` (reports `FSP006` for each ambiguous
type, alongside the existing `FSP004` loop) - plus `MessageBuilder.BuildAmbiguousGenericInferenceDiagnostic`.

**Validated by:** `IsAmbiguousGenericInferenceTests`, `GetAmbiguousInferredDestinationTypesTests`,
`BuildAmbiguousGenericInferenceDiagnosticTests`, a new case in `ContextBuilderTests.BuildTests`, and
the end-to-end `IntegrationTests/CompileErrorsTests.AmbiguousGenericTypeReachedThroughInference_RaisesFSP006`.

---

## ✅ 11. Combining any two of the three ways to specify a destination type doesn't merge or error - one silently wins

**Fixed - both repros, plus a third combination (generic argument + `Types`) that the manual/
non-incremental driver path could also reach independently of `ClassifyGeneric` (see "Implemented
in" below).**

There are three independent mechanisms for picking an explicit destination type:

1. The generic type argument - `ForceSetProperties<TypeA>`
2. Constructor positional argument(s) - `ForceSetProperties(typeof(TypeA))`
3. The named `Types` property - `ForceSetProperties(Types = [typeof(TypeA)])`

Nothing stops two (or all three) of these from being used together on the *same* attribute
application. When that happens, one silently wins and the other is discarded - with no error, and
no merging of the two into "validate both types."

**Repro 1 - generic argument + named `Types`:**

```csharp
public class TypeA { public string Name { get; set; } }
public class TypeB { public string Name { get; set; } }

[ForceSetProperties<TypeA>(Types = [typeof(TypeB)])]
public static object Create()
{
    return new TypeB { Name = "x" }; // previously: validated TypeA, Types = [TypeB] silently ignored
}
```

Previously: validated `TypeA` (reported `FSP001` for `TypeA.Name`), with `Types = [TypeB]` silently
discarded - the winning side had flipped once already, after issue #9's fix moved generic-form
discovery onto its own syntactic path (`ClassifyGeneric`), which never looked at `Types =` at all
and so always won unconditionally. Now: `FSP007: ForceSetProperties has more than one destination
type specification on the same member (generic argument, constructor argument, and/or Types
property); these don't merge - remove all but one` - neither type is validated, since validating
either one would be arbitrarily picking a winner.

(Note: the literal form `[ForceSetProperties<TypeA>(typeof(TypeB))]` - a positional argument on the
generic form - isn't valid C# at all: `ForceSetPropertiesAttribute<T>`'s generated source
(`Emission/AttributeEmitter.cs`) only declares a *parameterless* constructor, so that's a compile
error, CS1729, independent of this analyzer. The named `Types` property above is the reachable
equivalent, since the parameterless generic constructor doesn't prevent it.)

**Repro 2 - constructor positional argument + named `Types` (no generic form involved at all):**

```csharp
public class EmptyDto { }
public interface IDto { string Name { get; set; } }

[ForceSetProperties(typeof(EmptyDto), Types = new Type[] { typeof(IDto) })]
public static object Create()
{
    return new EmptyDto(); // previously: validated IDto, EmptyDto silently ignored
}
```

Previously: validated `IDto` (per issue #5, likely nothing useful), with `EmptyDto` silently
discarded. Now: `FSP007`, same as repro 1.

**Two separate fixes for two separate discovery paths, not one:**
- **Repro 1** needed a fix in `ForceSetPropertiesTargetFinder.ClassifyGeneric` - the generic form's
  own syntactic discovery path (added for issue #9), which had no knowledge of `Types =` at all.
  It now also checks the attribute syntax's own `ArgumentList` for a named `Types` argument
  (`HasTypesArgument` - purely syntactic, no semantic resolution needed for that part either) and
  returns a new `AttributeUsage.ConflictingTypeSpecification` instead of `SupportedGeneric` when
  both are present.
- **Repro 2**, plus a third combination this uncovered - **generic argument + `Types` through the
  *manual*, non-incremental driver path** (`FindTargets`, used directly by
  `ForceSetPropertiesDriver.GetDiagnostics(Compilation)` and by most of this repo's tests) - both
  needed a fix in `ContextBuilder`. Unlike the incremental generator, the manual path discovers a
  generic-form usage through the *same* `AttributeData`-based mechanism as the non-generic form (no
  `RegisterPostInitializationOutput` timing gap to work around there - see issue #9's notes on why
  that gap is incremental-generator-specific), so `GetGenericTypeArgument` and
  `GetTypesFromConstructorArguments`/`GetTypesFromNamedArgument` are all reachable from the same
  `AttributeData`, and a new `ContextBuilder.HasConflictingTypeSpecification` checks all three at
  once (`sourceCount > 1`) rather than the two originally scoped for repro 2 alone.

**Where the checks are wired in:** both new checks run as an early gate in
`ForceSetPropertiesDriver.GetDiagnostics` - before context-building for the non-generic form
(`HasConflictingTypeSpecification`), and inside `ClassifyGeneric` itself for the generic form
(`ConflictingTypeSpecification`, a new `AttributeUsage` kind checked ahead of the existing
`!IsSupported` check in the driver, so it isn't shadowed by that check's own diagnostic). Neither
gate calls `ResolveDestinationTypes`/builds a context at all when it fires - unlike a real
destination type, there's nothing to validate. `ResolveDestinationTypes`'s own internal priority
order is otherwise untouched, so using only one of the three mechanisms - alone, in any combination
of settable-property-only, constructor-only, or generic-only usage - is completely unaffected and
keeps validating exactly as before.

**New diagnostic:** `FSP007` (`DiagnosticDescriptors.ConflictingDestinationTypeSpecification`, error
severity).

**Implemented in:** `Models/AttributeUsage.cs` (new `ConflictingTypeSpecification` factory +
`ConflictingTypeSpecificationLocation`), `ForceSetPropertiesTargetFinder.ClassifyGeneric`/
`HasTypesArgument`, `ContextBuilder.HasConflictingTypeSpecification`,
`MessageBuilder.BuildConflictingDestinationTypeDiagnostic`, and
`ForceSetPropertiesDriver.GetDiagnostics` (both new gates).

**Validated by:** `HasConflictingTypeSpecificationTests` (both conflicting pairs, plus each source
alone confirmed not a conflict), `BuildConflictingDestinationTypeDiagnosticTests`, the end-to-end
`IntegrationTests/TargetTypeSpecificationTests` (both repros through the manual driver, reporting
`FSP007` and nothing else), and
`ForceSetAnalyserTests/InitializeTests.GenericAttributeFormConflictingWithNamedTypes_RaisesFSP007ThroughTheRealIncrementalPipeline`
(through the real `CSharpGeneratorDriver` - the only path that exercises `ClassifyGeneric`'s own
conflict check, since the manual driver never reaches it at all for the generic form).

---

## ✅ 12. Applying the attribute to a method with no body always reports every property as missing

**Fixed.** `IsSupportedDeclaration` (`Services/ForceSetPropertiesTargetFinder.cs`) now also checks
that a `MethodDeclarationSyntax` actually has a body (`method.Body != null || method.ExpressionBody
!= null`) before accepting it - a `ConstructorDeclarationSyntax` always has one in a supported
placement, so this check only needed to apply to methods, exactly as scoped. Applies to both an
interface method signature (no default implementation) and an `abstract` method declaration:

```csharp
public class Dto
{
    public string Name { get; set; }
}

public interface IFactory
{
    [ForceSetProperties]
    Dto Create();
}
```
```csharp
public abstract class FactoryBase
{
    [ForceSetProperties]
    public abstract Dto Create();
}
```

Previously: both reported `FSP001: Property 'Name' must be initialized on 'Dto'` - even though
there was no method body at all to have initialized it in. Now: both report `FSP003:
ForceSetProperties can only be applied to constructors, methods, or properties; this target is not
yet supported` - the same unsupported-placement error as attaching the attribute to a field or a
class, rather than implying a real, checkable method forgot to set `Name`. The real implementation
(an interface's implementing method, or an override of the abstract method) is what should be
annotated instead, once it exists - that richer "propagate the requirement" alternative is a
meaningfully bigger feature than this bug fix, and stays tracked separately in
[`Suggested-features.md`](Suggested-features.md).

Confirmed **not** broken by this fix (regression-tested): a default interface method implementation
(`Dto Create() => new Dto { Name = "x" };`, which does have a body) and an `override` of an
`abstract` method both validate normally, unaffected.

**Implemented in:** `ForceSetPropertiesTargetFinder.IsSupportedDeclaration`.

**Validated by:** `IsSupportedDeclarationTests` (block-bodied method, expression-bodied method, an
interface method signature, a default interface method implementation, an abstract method
declaration, an override of one, a constructor, a property, and a class declaration), a new case in
`IsOnSupportedDeclarationTests`, and the end-to-end
`IntegrationTests/AttributeTargetsTests` (interface method signature and abstract method both
raising `FSP003` instead of `FSP001`/`FSP002`, plus the two regression checks above).

---

## 🔴 13. Assigning a property's private backing field directly is never recognized as satisfying that property

```csharp
public class Dto
{
    private string _name;
    public string Name { get => _name; set => _name = value; }

    [ForceSetProperties]
    public Dto(string name)
    {
        _name = name; // bypasses the Name property setter directly
    }
}
```

Reports `FSP001: Property 'Name' must be initialized on 'Dto'` - even though `_name` is `Name`'s own
backing field, and `Name`'s getter reads directly from it, so `Name` is functionally fully
initialized the moment `_name` is assigned. Reproduced directly via `RoslynTestHelper.GetDiagnostics`.

**Why:** `PropertyAssignmentScanner.MarkIfRequiredProperty` matches the assignment's left-hand
symbol against `context.RequiredProperties` by symbol equality
(`SymbolEqualityComparer.Default.Equals(p.Symbol, leftSymbol)`). `_name` resolves to a *field*
symbol; `Name` in `RequiredProperties` is a *property* symbol. These are never equal, regardless of
how obviously related they are, so the assignment is invisible to the scanner - this is the same
"wrong symbol identity" shape as issue #5 (interfaces), just for fields vs. their own property
instead of an interface member vs. its implementation.

This only matters for manually-backed properties (a private field plus a hand-written getter/setter)
- auto-properties have no user-accessible backing field to assign directly, so they're unaffected.

**Likely fix:** this is more speculative than the other entries, since "which field backs which
property" isn't something Roslyn's symbol model exposes directly for a manually-implemented
property (unlike an auto-property, where `IPropertySymbol` doesn't expose a distinct field symbol
you can query either - the two cases would need the same underlying heuristic). One option: inspect
the property's `get` accessor body for a single `return someField;`-shaped expression and treat
`someField` as an alias for the property for matching purposes. This needs more design thought
before committing to an approach - flagging the gap here rather than a concrete fix.

**To validate the fix:** the example above should report `FSP201`, not `FSP001`, once the analyzer
can recognize `_name = name` as equivalent to setting `Name`.

---

## ✅ 14. Listing the same type twice in `Types = [...]` produces duplicate diagnostics

**Fixed, together with issue #11 (both touch how `ContextBuilder` resolves the explicit-types
list).**

```csharp
public class Dto
{
    public string Name { get; set; }
}

[ForceSetProperties(Types = new System.Type[] { typeof(Dto), typeof(Dto) })]
public Dto Create()
{
    return new Dto { Name = "x" };
}
```

Previously: **two** separate `FSP201: ForceSetProperties validated Dto: Name` diagnostics at the
same location, one per occurrence of `typeof(Dto)` in the list. Now: exactly one. A genuinely
missing property is likewise reported exactly once (`FSP001`), not once per repeated occurrence.

**Fixed in:** `ContextBuilder.ResolveDestinationTypes` now runs the explicit-types list (and only
that list - the generic-argument and inferred branches are always single-item, so duplicates can't
occur there) through a new `DeduplicateTypes` (by `SymbolEqualityComparer`, keeping first-occurrence
order) before returning it - rather than de-duplicating only in `Build` as originally proposed, so
every consumer of `ResolveDestinationTypes` (`Build`, `GetUnsupportedDestinationTypes`,
`GetAmbiguousInferredDestinationTypes`) benefits uniformly, not just the success/missing-property
path.

**Validated by:** `DeduplicateTypesTests` (a repeated type collapsed, distinct types all kept, a
repeated type mixed in with distinct ones, an empty list), and the end-to-end
`IntegrationTests/TargetTypeSpecificationTests` (the exact repro above reporting exactly one
`FSP201`, and a genuinely missing property reporting exactly one `FSP001`).

---

## Confirmed NOT broken (checked, no action needed)

- Plain `struct` with an explicit constructor that assigns properties via statements - behaves
  identically to a class.
- `record` with explicit settable properties and no positional constructor.
- `struct`/`record struct` default (parameterless, all-default) construction correctly reports
  missing properties - this is the intended behavior, not a bug.
