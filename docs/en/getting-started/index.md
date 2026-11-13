# Getting started

This page describes the prerequisites for an application that uses D3Fragments, how to structure its projects, and how to install the packages.

## Prerequisites

- .NET 10 or later
- EF Core 10 or later (when using `Sashimi343.D3Fragments.EFCore`)
- ASP.NET Core 10 or later (when using `Sashimi343.D3Fragments.AspNetCore`)

## Project structure

D3Fragments assumes that an application is split into four projects: the presentation layer, the application layer, the domain layer, and the infrastructure layer.
The ideas behind the layers and the rules for dependencies between them are described in [Architecture](../guide/01-architecture.md).

Each project references the following packages.

| Project (example) | Layer | Packages to reference |
|---|---|---|
| `MyApp.Domain` | Domain layer | `Sashimi343.D3Fragments` |
| `MyApp.Application` | Application layer | `Sashimi343.D3Fragments.Application` |
| `MyApp.Infrastructure` | Infrastructure layer | `Sashimi343.D3Fragments.EFCore`, a database provider (such as `Npgsql.EntityFrameworkCore.PostgreSQL`) |
| `MyApp.Web` | Presentation layer | `Sashimi343.D3Fragments.AspNetCore` (and `Sashimi343.D3Fragments.AspNetCore.OpenApi` if needed) |

`Sashimi343.D3Fragments.Application` depends on `Sashimi343.D3Fragments`, so the application layer does not need a separate reference to `Sashimi343.D3Fragments`.
The same applies to the other packages.

### Combining multiple layers into one project

There are four layers, but you do not have to split the application into exactly four projects.
In a small application, you can combine several layers into one project.
For example, you can combine the domain layer and the application layer into `MyApp.Core`, which leaves three projects.

In a combined project, specify the layers it belongs to in `AssemblyInfo.cs` by joining them with `|`.
Add all the packages that the combined layers use (in this example, `Sashimi343.D3Fragments.Application`).

```csharp
using D3Fragments;

[assembly: ArchitectureLayer(ArchitectureLayerType.Application | ArchitectureLayerType.Domain)]
```

However, references between layers inside one project cannot be detected by the project reference check ([DFR0001](../analyzers/DFR0001.md)).
The checks on references between classes ([DFR0002](../analyzers/DFR0002.md) and others) still run, but they cover less than when each layer has its own project.
If you want the analyzers to reliably enforce the dependency rules between layers, use a separate project for each layer.

## Demo application

A library lending management application is available as an example of an application built with D3Fragments.
Its code shows the four-project structure and concrete usage of each feature.

- [demo/01_BasicWebApp](https://github.com/sashimi343/D3Fragments/tree/main/demo/01_BasicWebApp) (GitHub)

## Installation

Add the packages from the table above to each project.

```bash
dotnet add MyApp.Domain package Sashimi343.D3Fragments
dotnet add MyApp.Application package Sashimi343.D3Fragments.Application
dotnet add MyApp.Infrastructure package Sashimi343.D3Fragments.EFCore
dotnet add MyApp.Web package Sashimi343.D3Fragments.AspNetCore
```

If you also use the analyzers, add `Sashimi343.D3Fragments.Analyzers` to every project.
See [Analyzers](../analyzers/index.md) for how to set them up.

## Next steps

In [Quick start](01-quick-start.md), you build a Web API that registers and retrieves a single aggregate.
