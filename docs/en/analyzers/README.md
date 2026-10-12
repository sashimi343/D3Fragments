# D3Fragments analyzers

D3Fragments.Analyzers helps you comply with the design and implementation guidelines recommended when using the D3Fragments packages.

## Setup

To apply D3Fragments.Analyzers, follow the steps below.

### Add the analyzer reference

Add the following reference to each project.

**Using the dotnet command**

```bash
dotnet add package Sashimi343.D3Fragments.Analyzers
```

**Writing directly in the csproj**

```csproj
<ItemGroup>
  <PackageReference Include="Sashimi343.D3Fragments.Analyzers">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### Set the assembly architecture layer

In `AssemblyInfo.cs` of each project that uses D3Fragments.Analyzers, specify the architecture layer to which the assembly belongs.
`ArchitectureLayerAttribute` is included in the D3Fragments package (`Sashimi343.D3Fragments`), so each project must reference the D3Fragments package.
For details on architecture layers, see also [D3Fragments architecture](../architecture.md).

The architecture layer is one of the following.

| Architecture layer                     | Value                                  |
| :------------------------------------- | :------------------------------------- |
| Presentation layer (user interface layer) | `ArchitectureLayerType.Presentation` |
| Application layer                      | `ArchitectureLayerType.Application`    |
| Domain layer                           | `ArchitectureLayerType.Domain`         |
| Infrastructure layer                   | `ArchitectureLayerType.Infrastructure` |

If the assembly implements multiple architecture layers, combine multiple `ArchitectureLayerType` values with `|`.

The following is an example of `AssemblyInfo.cs`.

```csharp
using D3Fragments;

[assembly: ArchitectureLayer(ArchitectureLayerType.Application | ArchitectureLayerType.Domain)]
```

## Rules

| Rule ID and warning | Description |
|---|---|
| [DFR0001: Architecture reference violation (project reference)](DFR0001.md) | A dependency between projects is inappropriate. An assembly with a higher level of abstraction should not depend on an assembly with a lower level of abstraction. |
| [DFR0002: Architecture reference violation (module reference)](DFR0002.md) | A dependency between modules (classes and interfaces) is inappropriate. |
| [DFR0003: Entity classes should not have public setters](DFR0003.md) | An entity class defines a public setter. The state of an entity should be changed through operations, and overwriting values with public setters is not recommended. |
| [DFR0004: Do not define repository methods that retrieve part of an aggregate](DFR0004.md) | The return type of a repository method is a part of the aggregate instead of the aggregate root type. Repository methods must retrieve data in units of aggregate roots. |
| [DFR0005: Do not define repository methods that update part of an aggregate](DFR0005.md) | A part of an aggregate is used as a parameter type of a repository method. Such a method allows partial updates of an aggregate and can break its consistency, so do not implement it. |
| [DFR0006: Do not expose constructors of TypeSafeEnum types outside the class](DFR0006.md) | The accessibility of a constructor of a class that inherits `TypeSafeEnum<TSelf, TKey>` is not private. Exposing the constructor outside the class allows unintended values to be created, so avoid exposing it. |
| [DFR0007: Specify the declaring type itself for the self-referencing type parameter TSelf](DFR0007.md) | A type other than the declaring class itself is specified for a self-referencing type parameter `TSelf` such as that of `TypeSafeEnum<TSelf, TKey>`. An invalid cast exception occurs at run time. |
| [DFR0008: Define TypeSafeEnum values as static readonly fields](DFR0008.md) | A value of `TypeSafeEnum<TSelf, TKey>` is defined by something other than a static readonly field. An exception due to a duplicate value definition or a replacement of a value can occur. |
| [DFR0009: Entities should not expose mutable collections](DFR0009.md) | An entity class exposes a mutable collection. Even if the setter is not public, the state of the entity can be changed from outside through collection operations. |
| [DFR0010: Value objects should not have public setters](DFR0010.md) | A value object defines a public setter. Value objects should be immutable. |
| [DFR0011: Controllers should not inject domain classes through the constructor](DFR0011.md) | A controller injects a domain class (such as a domain service) through its constructor. Controllers should call the application layer through `IRequestExecutor<TRequest, TResponse>`. |
