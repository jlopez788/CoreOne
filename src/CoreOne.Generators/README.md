# CoreOne.Generators

A Roslyn source generator that produces strongly-typed IDs from a single attribute. Eliminates boilerplate for value types that wrap a primitive (e.g. `OrderId` wrapping a `Guid`).

## Usage

Decorate any `partial` type with `[StronglyTypedId]`:

```csharp
// Generic form (C# 11 / .NET 7+)
[StronglyTypedId<Guid>]
public partial struct OrderId { }

// Non-generic form (works everywhere)
[StronglyTypedId(typeof(int))]
public partial struct UserId { }
```

The generator emits a `.g.cs` file alongside your type with all members filled in automatically.

## What Gets Generated

### Core Members

| Member | Notes |
|---|---|
| `T Value` | The underlying value |
| `static T Empty` | Default/empty instance (`Guid.Empty`, `""`, etc.) |
| `string ValueAsString` | Culture-invariant string representation |
| `TypeName()` | Default constructor |
| `static TypeName From{T}(T value)` | Factory method |
| `static TypeName New()` | Guid types — wraps `Guid.NewGuid()` |
| `static TypeName Create()` | Guid types — wraps `ID.Create()` (sequential GUID) |
| `ToString()`, `GetHashCode()`, `Equals()` | Value-based equality |
| `==`, `!=` operators | Value-based comparison |
| `TryParse`, `Parse` | String parsing |
| `IParsable<T>`, `ISpanParsable<T>` | .NET 7+ only |
| `IComparable`, `<`, `<=`, `>`, `>=` | When declared on the partial type |

All members are **additive** — any member you define yourself is skipped by the generator.

### Serialization Converters

By default, the generator also emits nested converter classes and applies the corresponding attributes to your type:

| Converter | Generated Type |
|---|---|
| `System.Text.Json` | `{TypeName}JsonConverter` |
| `Newtonsoft.Json` | `{TypeName}NewtonsoftJsonConverter` |
| `System.ComponentModel.TypeConverter` | `{TypeName}TypeConverter` |
| `MongoDB.Bson` | `{TypeName}BsonConverter` |

Each converter is only emitted when the relevant library is detected in the consuming project.

## Supported Underlying Types

22 primitive types are supported:

`bool` · `byte` · `sbyte` · `short` · `ushort` · `int` · `uint` · `long` · `ulong` ·
`Int128` · `UInt128` · `BigInteger` · `float` · `double` · `decimal` · `Half` ·
`Guid` · `string` · `DateTime` · `DateTimeOffset` · `MongoDB.Bson.ObjectId`

## Attribute Options

```csharp
[StronglyTypedId<Guid>(
    generateSystemTextJsonConverter: true,           // default
    generateNewtonsoftJsonConverter: true,           // default
    generateSystemComponentModelTypeConverter: true, // default
    generateMongoDBBsonSerialization: true,          // default
    addCodeGeneratedAttribute: true,                 // default
    StringComparison = StringComparison.Ordinal,     // string IDs only
    GenerateToStringAsRecord = false
)]
public partial struct OrderId { }
```

Set any converter flag to `false` to suppress that converter entirely.

## Sequential GUIDs

For `Guid`-backed IDs, `Create()` uses `ID.Create()` from the CoreOne library, which generates sequential (v7-style) GUIDs. This is friendlier to database indexes than random GUIDs.

```csharp
var id = OrderId.Create(); // sequential GUID — good for DB inserts
var id = OrderId.New();    // random Guid.NewGuid()
```

## Setup

Add the generator project as an analyzer reference — no assembly reference is needed at runtime:

```xml
<ProjectReference Include="..\CoreOne.Generators\CoreOne.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```
