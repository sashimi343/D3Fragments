# D3Fragments architecture

## Overview

D3Fragments adopts an architecture based on Clean Architecture.
The overall architecture is as follows.

**TODO: Add a diagram that describes the overall architecture**

## Assembly architecture layers

Assemblies (projects) are classified into the following four architecture layers.
The architecture layer to which each assembly belongs is identified by the `ArchitectureLayerAttribute` attribute set in `AssemblyInfo.cs`.
`ArchitectureLayerAttribute` is included in the D3Fragments package (`Sashimi343.D3Fragments`).

The architecture layer is one of the following.

| Architecture layer                     | Value                                  |
| :------------------------------------- | :------------------------------------- |
| Presentation layer (user interface layer) | `ArchitectureLayerType.Presentation` |
| Application layer                      | `ArchitectureLayerType.Application`    |
| Domain layer                           | `ArchitectureLayerType.Domain`         |
| Infrastructure layer                   | `ArchitectureLayerType.Infrastructure` |

A single assembly can implement multiple architecture layers.
If the assembly implements multiple architecture layers, combine multiple `ArchitectureLayerType` values with `|`.

The following is an example of `AssemblyInfo.cs`.

```csharp
using D3Fragments;

[assembly: ArchitectureLayer(ArchitectureLayerType.Application | ArchitectureLayerType.Domain)]
```

## Module layers

Module layers subdivide the classes and interfaces implemented in each architecture layer (presentation, application, domain, and infrastructure layers) by their roles.
The following module layers are defined for each architecture layer.

### Determining the module layer

The module layer of a class or interface is determined by both the architecture layer of the assembly to which the class or interface belongs and the conditions of the class or interface itself (the types it inherits or implements).

- Classes and interfaces in an assembly without `ArchitectureLayerAttribute` have no module layer (they are not verified by rules that depend on module layers).
- For example, a class that inherits `EntityBase<TId>` is determined as `Domain_Entity` only when it exists in a domain layer assembly.
- In an assembly with multiple architecture layers, a class or interface is determined as a module layer if it meets the conditions of that module layer in any of those architecture layers. For example, a class that inherits `EntityBase<TId>` in an application + domain layer assembly is determined as `Domain_Entity`.
- In an assembly with multiple architecture layers, a class or interface that does not meet the conditions of any module layer is determined as `Other` (other, with an unknown architecture layer), because it cannot be identified which architecture layer's `*_Other` it belongs to. In an assembly with a single architecture layer, it is determined as `*_Other` of that architecture layer.
- If a class or interface meets the conditions of multiple module layers, it is determined as the first matching module layer in the order described in this document.

### Presentation layer (user interface layer)

#### Presentation_Controller

A class that inherits ASP.NET Core [PageBase](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.razorpages.pagebase) or [ControllerBase](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase), or implements the [IComponent](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.icomponent) interface.

#### Presentation_Other

A class or interface in the presentation layer that does not fall under any of the above.

### Application layer

#### Application_Request

A class that directly or indirectly implements the `D3Fragments.Application.IRequest<TResponse>` interface or the `D3Fragments.Application.ICommand` interface.

#### Application_RequestHandler

A class that directly or indirectly implements the `D3Fragments.Application.IRequestHandler<in TRequest, TResponse>` interface.

#### Application_RequestExecutor

A class that directly or indirectly implements the `D3Fragments.Application.IRequestExecutor<in TRequest, TResponse>` interface.

#### Application_DomainEventHandler

A class that directly or indirectly implements the `D3Fragments.Application.IBeforeCommitDomainEventHandler<in TEvent>` or `D3Fragments.Application.IAfterCommitDomainEventHandler<in TEvent>` interface.

#### Application_PipelineBehavior

A class that directly or indirectly implements the `D3Fragments.Application.IPipelineBehavior<in TRequest, TResponse>` interface.

#### Application_Other

A class or interface in the application layer that does not fall under any of the above.

### Domain layer

#### Domain_Entity

A class that directly or indirectly inherits the `D3Fragments.EntityBase<TId>` class.

#### Domain_ValueObject

A class that directly or indirectly implements the `D3Fragments.IValueObject<TSelf, TPrimitive>` interface.

#### Domain_RepositoryInterface

An interface that directly or indirectly inherits the `D3Fragments.IRepository<TAggregate, TId>` interface.

#### Domain_DomainRule

A class that directly or indirectly implements the `D3Fragments.IDomainRule<in TContext>` interface.

#### Domain_Other

A class or interface in the domain layer that does not fall under any of the above.

### Infrastructure layer

#### Infrastructure_RepositoryImplementation

A class that directly or indirectly implements the `D3Fragments.IRepository<TAggregate, TId>` interface.

#### Infrastructure_DbContext

A class that directly or indirectly inherits the `Microsoft.EntityFrameworkCore.DbContext` class.

#### Infrastructure_UnitOfWork

A class that directly or indirectly implements the `D3Fragments.Application.IUnitOfWork` interface.

#### Infrastructure_Other

A class or interface in the infrastructure layer that does not fall under any of the above.
