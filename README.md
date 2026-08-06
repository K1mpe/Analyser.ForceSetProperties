# ForceSetProperties

Compile-time enforcement that **all settable properties must be initialized** when constructing a type.

`ForceSetProperties` is a Roslyn analyzer–backed attribute that prevents missing property assignments when creating DTOs, mapping models, or writing factory methods. If a new property is added later, **all annotated constructors, methods, and expressions will fail compilation until updated**.

---

## ✨ Features

* ✅ Compile-time enforcement (no runtime cost)
* ✅ Works on **constructors**, **methods**, and **expressions** (class-level support is planned — see [Limitations](#limitations))
* ✅ Supports **generic type override**: `ForceSetProperties<T>`
* ✅ Detects **object initializer** assignments
* ✅ Detects **return expressions**
* ✅ Detects **out parameter assignments**
* ✅ Detects **lambda / expression-bodied factories**
* ✅ Fails when new properties are added but not mapped

---

## 📦 Installation

Install via NuGet:

```bash
dotnet add package ForceSetProperties.Analyzers
```

or via Package Manager:

```powershell
Install-Package ForceSetProperties.Analyzers
```

---

## 🚀 Basic Usage

### Apply to a class — not yet supported

Placing `[ForceSetProperties]` directly on a class is **not implemented yet**. Rather than silently doing nothing, this raises a compile error so it can't be mistaken for working:

```csharp
[ForceSetProperties] // ❌ FSP006: not yet supported on a class
public class DtoModel
{
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Until class-level support lands, annotate the individual constructors, methods, or expressions that build the type instead — see the sections below.

---

## Apply to a constructor

```csharp
public class DtoModel
{
    public DtoModel()
    {
    }

    [ForceSetProperties]
    public DtoModel(DbModel db)
    {
        Name = db.Name;
        CreatedAt = db.CreatedAt;
        UpdatedAt = db.UpdatedAt;
    }

    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

If a property is added later:

```csharp
public string Description { get; set; }
```

The constructor will now **fail compilation** until updated.

---

## Apply to a method

```csharp
[ForceSetProperties]
public DtoModel FromFunction(string name, DateTime createdAt, DateTime updatedAt)
{
    return new DtoModel
    {
        Name = name,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

This ensures the returned `DtoModel` always has **every property initialized**.

---

## Specify target type explicitly

Useful when:

* return type is different
* using `out` parameters
* multiple DTOs created

```csharp
[ForceSetProperties<DtoModel>]
public DbModel SomeFunction(DbModel source, out DtoModel dto)
{
    dto = new DtoModel
    {
        Name = source.Name,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt
    };

    return source;
}
```

---

## Apply to expressions / lambdas

```csharp
[ForceSetProperties]
public static Expression<Func<DbModel, DtoModel>> FromExpression => db => new DtoModel
{
    Name = db.Name,
    CreatedAt = db.CreatedAt,
    UpdatedAt = db.UpdatedAt
};
```

This is especially useful for:

* EF projections
* LINQ Select mappings
* AutoMapper replacements
* Expression factories

---

## Which properties are required

A property is required when the code being validated can actually see **and** set it — not only when the property is `public`. The analyzer checks this the same way the C# compiler does, using normal accessibility rules (`public`, `internal`, `protected`, `private`) from the point of view of the annotated constructor, method, or expression.

```csharp
public class DtoModel
{
    public string Name { get; private set; }

    [ForceSetProperties]
    public static DtoModel CreateInstance(string name)
    {
        return new DtoModel { Name = name };
    }
}
```

`Name` has a `private set`, but `CreateInstance` lives inside `DtoModel`, so it has access — `Name` is still required. The same `private set` property would **not** be required from a `[ForceSetProperties]` method in a different class, since that code has no access to the setter.

This means a property is skipped only when the code being validated genuinely cannot set it — not just because the setter happens to be non-public.

---

## What counts as "set"

The analyzer considers a property initialized when:

### Object initializer

```csharp
new DtoModel
{
    Name = x,
    CreatedAt = y,
    UpdatedAt = z
}
```

### Assignment after creation

```csharp
var dto = new DtoModel();

dto.Name = x;
dto.CreatedAt = y;
dto.UpdatedAt = z;
```

### Constructor assignments

```csharp
Name = db.Name;
CreatedAt = db.CreatedAt;
UpdatedAt = db.UpdatedAt;
```

---

## Tracing into called methods and constructors

If a `[ForceSetProperties]` method doesn't build the target type directly but delegates to another **normal, non-virtual method or constructor**, the analyzer follows that call and checks the callee for the same "what counts as set" patterns.

```csharp
[ForceSetProperties]
public DtoModel Create(DbModel db)
{
    return Map(db);
}

private static DtoModel Map(DbModel db)
{
    return new DtoModel
    {
        Name = db.Name,
        CreatedAt = db.CreatedAt,
        UpdatedAt = db.UpdatedAt
    };
}
```

This is valid — `Map` sets every property, so `Create` is considered compliant even though it never constructs `DtoModel` itself.

This works because the callee is part of the same compilation, so the analyzer can resolve exactly which method or constructor runs and inspect its body at compile time.

This also follows constructor chaining (`: this(...)` / `: base(...)`):

```csharp
public class DtoModel
{
    public string Name { get; set; }
    public string Id { get; set; }

    public DtoModel()
    {
        Id = Guid.NewGuid().ToString();
    }

    [ForceSetProperties]
    public DtoModel(string name) : this()
    {
        Name = name;
    }
}
```

`Id` is never assigned directly inside the `(string name)` constructor, but the analyzer follows the `: this()` call into the parameterless constructor and finds it there.

---

## What triggers a compile error

### Missing property

```
error FSP001: Property 'UpdatedAt' must be initialized when using ForceSetProperties
```

### Multiple missing properties

```
error FSP002: The following properties must be initialized:
 - CreatedAt
 - UpdatedAt
```

### Unsupported attribute placement

```
error FSP006: ForceSetProperties can only be applied to constructors, methods, or properties; this target is not yet supported
```

Raised whenever `[ForceSetProperties]` is placed anywhere else — most notably on a class (see above), but also fields, events, or any other declaration. This exists so an unsupported usage fails loudly instead of being silently skipped.

### Unsupported destination type

```
error FSP007: ForceSetProperties cannot validate 'void'; void, object, and dynamic are not supported destination types
```

Raised when the resolved destination type is `void`, `object`, or `dynamic` — there are no strongly-typed properties to check, so validating it would either be meaningless or trivially "pass" with nothing checked. This applies no matter how the type was determined: return type inference, `ForceSetProperties<T>`, or an explicit `Types = [...]`.

---

## Validation feedback

Besides compile errors, the analyzer also reports an informational diagnostic when a `[ForceSetProperties]` target passes validation.

When every property was set directly, it's a short one-line summary naming the type and its properties:

```
info FSP101: ForceSetProperties validated DtoModel: Name, CreatedAt, UpdatedAt
```

When at least one property was only found by [tracing into a called method or constructor](#tracing-into-called-methods-and-constructors), it switches to a full breakdown showing where each property was actually set — file path relative to the project, plus the method name whenever that came from a trace:

```
info FSP101: Type checked: DtoModel
Name: TestModels\DtoModel.cs line 46
CreatedAt: TestModels\DtoModel.cs line 47
UpdatedAt: TestModels\DtoModel.cs line 23 (via .ctor)
```

If a property ends up being set in more than one place, only the first location found is shown.

This is reported at `Info` severity on the `[ForceSetProperties]` attribute, so it stays out of the way in normal build output while still being visible as a subtle IDE squiggle and in the tooltip / "Messages" tab of the Error List — a quick way to confirm a mapping is complete without opening and re-reading the whole method.

---

## Attribute Targets

### Class — not yet supported

Raises `FSP006`. See [Apply to a class](#apply-to-a-class--not-yet-supported) above.

---

### Constructor

```csharp
[ForceSetProperties]
public DtoModel(DbModel db)
```

---

### Method

```csharp
[ForceSetProperties]
public DtoModel Create()
```

---

### Expression / Property

```csharp
[ForceSetProperties]
public static Expression<Func<X,Y>> Map => ...
```

---

### Generic Type Override

```csharp
[ForceSetProperties<DtoModel>]
```

---

## Supported Scenarios

- ✔ DTO mapping
- ✔ EF projections
- ✔ Factory methods
- ✔ Constructors
- ✔ Static creators
- ✔ Lambda expressions
- ✔ Expression trees
- ✔ Out parameter assignment
- ✔ Multi-return methods

---
## Specifying Target Types

`ForceSetProperties` can determine the target type in multiple ways depending on how the attribute is used.

The analyzer supports:

1. Return type inference  
2. Generic attribute `ForceSetProperties<T>`  
3. Explicit `Types` property `ForceSetProperties(Types = [...])`  
4. Multiple target types  

---

### Return type inference (default)

When no type is specified, the analyzer uses the **return type** of the method or expression.

```csharp
[ForceSetProperties]
public DtoModel Create()
{
    return new DtoModel
    {
        Name = "Test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
```

### Generic attribute

Use the generic version when the return type is different or cannot be inferred.

```csharp
[ForceSetProperties<DtoModel>]
public DbModel Create(DbModel source, out DtoModel dto)
{
    dto = new DtoModel
    {
        Name = source.Name,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt
    };

    return source;
}
```

### Using Types property

The attribute also supports explicitly specifying the type using the Types property.

```csharp
[ForceSetProperties(Types = [typeof(DtoModel)])]
public DbModel Create(DbModel source, out DtoModel dto)
{
    dto = new DtoModel
    {
        Name = source.Name,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt
    };

    return source;
}
```
### Multiple types

You can enforce multiple DTOs within the same method.

```csharp
[ForceSetProperties(Types = [typeof(UserDto), typeof(RoleDto)])]
public void Map(User user, Role role, out UserDto userDto, out RoleDto roleDto)
{
    userDto = new UserDto
    {
        Id = user.Id,
        Name = user.Name
    };

    roleDto = new RoleDto
    {
        Id = role.Id,
        Name = role.Name
    };
}
```
All specified types must have every publicly assignable property initialized.

### All supported forms

```csharp
[ForceSetProperties]

[ForceSetProperties<DtoModel>]

[ForceSetProperties(typeof(DtoModel))]

[ForceSetProperties(Types = [typeof(DtoModel)])]

[ForceSetProperties(Types = [typeof(Dto1), typeof(Dto2)])]
```

All forms are treated equivalently by the analyzer.
---


## Non-Goals

The analyzer intentionally does NOT:

* Require setters to be public
* Require constructor parameters
* Enforce nullability
* Enforce order
* Enforce nested object initialization

This analyzer **only ensures all properties settable from the validated code — public, internal, protected, or private — are assigned**.

---

## Limitations

Tracing into called methods and constructors has a few intentional boundaries:

* **Interfaces, virtual, and overridden methods are not followed.** If the call could resolve to more than one implementation at runtime, the analyzer ignores it entirely — assignments inside are neither counted nor required.
* **Delegates and lambdas stored in variables are not followed.** Only direct calls to ordinary methods and constructors are traced.
* **Conditional branches (`if`/`else`, `switch`) are not analyzed for exhaustiveness.** A property set inside an `if` — but not its `else`, or in only one `switch` case — still counts as "set". The analyzer does not try to prove every code path sets a property; the goal is to catch a property that was **completely forgotten**, not to enforce branch-complete initialization.
* **Methods without available source (e.g. from a referenced assembly) are not followed.** Only methods and constructors that are part of the current compilation can be inspected.

These limitations are deliberate — the feature exists to catch forgotten property assignments when a new property is added, not to perform full control-flow or dataflow analysis.

---

## Why use this instead of `required`

`required` enforces initialization at **call site**:

```csharp
new DtoModel { ... }
```

`ForceSetProperties` enforces initialization at **factory definition**:

```csharp
DtoModel Create()
```

This makes it ideal for:

- DTO mapping layers
- Conversion constructors
- Projection expressions
- Factory patterns
- Preventing silent DTO drift

---

## Example: Safe DTO evolution

Initial DTO:

```csharp
public class DtoModel
{
    [ForceSetProperties]
    public DtoModel(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}
```

Later:

```csharp
public DateTime CreatedAt { get; set; }
```

All mappings now fail compilation until updated.

This prevents:

* silent nulls
* incomplete mappings
* runtime bugs
* forgotten properties

---

## Analyzer Rules

Error diagnostics use the `FSP0xx` range; informational diagnostics use `FSP1xx`, so severity is visible from the ID alone.

### Errors (`FSP0xx`)

| ID     | Description                      |
| ------ | --------------------------------- |
| FSP001 | Missing property assignment       |
| FSP002 | Multiple properties missing       |
| FSP003 | No object creation found          |
| FSP004 | Multiple assignments detected     |
| FSP005 | Unsupported construction pattern  |
| FSP006 | Unsupported attribute target       |
| FSP007 | Unsupported destination type       |

### Informational (`FSP1xx`)

| ID     | Description               |
| ------ | -------------------------- |
| FSP101 | All properties validated   |

---

## Best Practices

### Use on DTOs

Class-level support isn't available yet, so annotate every constructor and factory method that builds the DTO instead (see below).

### Use on mapping constructors

```csharp
[ForceSetProperties]
public UserDto(User user)
```

### Use on expression projections

```csharp
[ForceSetProperties]
Expression<Func<User, UserDto>>
```

---

## Performance

- Compile-time only
- Zero runtime overhead
- No reflection
- No allocations
- No IL changes

---

## License

MIT
