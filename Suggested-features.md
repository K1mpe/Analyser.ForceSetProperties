# Suggested Features

Feature ideas that go beyond fixing existing bugs - not scheduled for the short term, kept here so
they aren't lost. Unlike [`Known-issues.md`](Known-issues.md), nothing here is a defect in current
behavior; these are proposals for new capability.

---

## Propagate `[ForceSetProperties]` on an interface/abstract method to every implementation

Related to [`Known-issues.md`](Known-issues.md) #12: placing `[ForceSetProperties]` directly on a
bodyless interface method or an `abstract` method currently (once #12 is fixed) raises an error,
since there's no method body to check. Instead of only rejecting that placement, this method
requirement could propagate to every implementation/override automatically: each class implementing
the interface method, or overriding the abstract method, gets validated as if that implementation
itself carried `[ForceSetProperties]` - without needing to annotate every one by hand. The
interface/abstract declaration becomes a single point of enforcement instead of one that has to be
repeated on every implementer.

```csharp
public interface IFactory
{
    [ForceSetProperties]
    Dto Create();
}

public class ConcreteFactory : IFactory
{
    public Dto Create() => new Dto(); // would be validated here, with no attribute of its own
}
```

**What implementing this would require:**

1. **Detect the bodyless case**: a `MethodDeclarationSyntax` with no `Body` and no `ExpressionBody`
   on an interface member or an `abstract` method (this detection is shared with the simpler fix in
   Known-issues.md #12, which just turns this case into an error instead of propagating from it).
2. **Find every implementation/override in the compilation.** This project only references
   `Microsoft.CodeAnalysis.CSharp`/`Microsoft.CodeAnalysis.Analyzers`, not
   `Microsoft.CodeAnalysis.Workspaces` - so the usual `SymbolFinder.FindImplementationsAsync`/
   `FindOverridesAsync` convenience APIs aren't available. This needs a manual walk instead: for an
   interface method, enumerate every named type in the compilation whose `AllInterfaces` includes
   the declaring interface, and resolve the implementing method via
   `INamedTypeSymbol.FindImplementationForInterfaceMember`; for an abstract method, walk types whose
   `BaseType` chain includes the declaring class and match via `IMethodSymbol.OverriddenMethod`.
   Only implementations with source in the current compilation can be found this way - the same
   boundary already documented under Limitations for "methods without available source."
3. **Validate each implementation using its own signature**, not the interface/abstract member's -
   destination-type inference should run normally against each implementation (return types can
   differ under covariance), so this is "treat each implementation as its own target," not "copy
   the interface member's resolved type onto every implementer."
4. **Decide what happens if an implementation also carries its own explicit
   `[ForceSetProperties]`.** Doubling up shouldn't produce duplicate diagnostics for the same thing -
   likely just let the implementation's own explicit attribute take over for that one, and skip
   propagating onto it separately.
5. **Decide where the diagnostic is reported** for an implementation that fails validation - most
   likely on the implementation itself (so the error appears where the fix belongs), with the
   message referencing that it's required via the interface/abstract member, rather than on the
   interface/abstract declaration for every failing implementer.

This is a meaningfully bigger feature than a normal bug fix, not a small patch - the design
questions above need decisions before implementation starts.

**To validate:** given the interface example above, a class implementing `IFactory` with `Create()`
missing a required property should report `FSP001` **on that implementation**, and a compliant
implementation should report `FSP201` there - all without any attribute on the implementation
itself. Same for an abstract method and its overrides. A default interface method implementation
(one that does have a body) is a normal target already and must keep validating exactly as it does
today, independent of this propagation behavior.
