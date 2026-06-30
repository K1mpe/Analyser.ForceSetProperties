# ForceSetProperties

Compile-time enforcement that **all publicly settable properties must be initialized** when constructing a type.

`ForceSetProperties` is a Roslyn analyzer–backed attribute that prevents missing property assignments when creating DTOs, mapping models, or writing factory methods. If a new property is added later, **all annotated constructors, methods, and expressions will fail compilation until updated**.

---

## ✨ Features

* ✅ Compile-time enforcement (no runtime cost)
* ✅ Works on **classes**, **constructors**, **methods**, and **expressions**
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

### Apply to a class

Placing the attribute on a class forces **every constructor, method, and expression** that creates the type to set **all public settable properties**.

```csharp
[ForceSetProperties]
public class DtoModel
{
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Now any creation missing a property will produce a **compile error**:

```csharp
return new DtoModel
{
    Name = name
    // ❌ Compile error: Missing CreatedAt, UpdatedAt
};
```

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

---

## Attribute Targets

### Class

Forces all constructors, methods, and expressions in the class

```csharp
[ForceSetProperties]
public class DtoModel { }
```

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

* Require private setters
* Require constructor parameters
* Enforce nullability
* Enforce order
* Enforce nested object initialization

This analyzer **only ensures all public settable properties or init properties are assigned**.

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
[ForceSetProperties]
public class DtoModel
{
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

| ID     | Description                      |
| ------ | -------------------------------- |
| FSP001 | Missing property assignment      |
| FSP002 | Multiple properties missing      |
| FSP003 | No object creation found         |
| FSP004 | Multiple assignments detected    |
| FSP005 | Unsupported construction pattern |

---

## Best Practices

### Use on DTOs

```csharp
[ForceSetProperties]
public class UserDto
```

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
