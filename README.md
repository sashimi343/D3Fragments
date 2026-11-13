<p align="center">
  <img src="assets/icon.svg" alt="D3Fragments" width="96" height="96">
</p>

<h1 align="center">D3Fragments</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/Sashimi343.D3Fragments"><img src="https://img.shields.io/nuget/v/Sashimi343.D3Fragments?label=NuGet" alt="NuGet"></a>
  <a href="https://github.com/sashimi343/D3Fragments/actions/workflows/ci.yml"><img src="https://github.com/sashimi343/D3Fragments/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="License: MIT"></a>
</p>

<p align="center">
  English | <a href="README.ja.md">日本語</a>
</p>

D3Fragments is a library that supports .NET application development based on Domain-Driven Design (DDD).
It provides, in reusable form, the classes that many projects end up writing for themselves: building blocks for the domain model, a Command / Query execution pipeline, and integrations with EF Core and ASP.NET Core.

## Features

- **Domain model building blocks**: aggregate roots, entities, value objects, type-safe enums, `Result`, and domain events
- **Use case pipeline**: Commands and Queries are processed by handlers, and the pipeline saves the changes
- **EF Core integration**: mapping for value objects and `UuidV7`, optimistic concurrency, and domain event dispatching
- **ASP.NET Core integration**: converts domain errors into Problem Details responses
- **Analyzers**: report deviations from the design guidelines at build time

## Installation

D3Fragments requires .NET 10 or later.
Add the package that matches each layer of your application.

```bash
dotnet add package Sashimi343.D3Fragments                # Domain layer
dotnet add package Sashimi343.D3Fragments.Application    # Application layer
dotnet add package Sashimi343.D3Fragments.EFCore         # Infrastructure layer
dotnet add package Sashimi343.D3Fragments.AspNetCore     # Presentation layer
```

| Package | Description |
|---|---|
| `Sashimi343.D3Fragments` | Domain model building blocks |
| `Sashimi343.D3Fragments.Application` | Command / Query execution pipeline |
| `Sashimi343.D3Fragments.EFCore` | EF Core integration |
| `Sashimi343.D3Fragments.AspNetCore` | ASP.NET Core integration |
| `Sashimi343.D3Fragments.AspNetCore.OpenApi` | OpenAPI integration |
| `Sashimi343.D3Fragments.Testing` | Test helpers |
| `Sashimi343.D3Fragments.Testing.EFCore` | Helpers for tests against a real database |
| `Sashimi343.D3Fragments.Analyzers` | Roslyn analyzers for the design guidelines |

## Documentation

See the documentation for how to get started, the guides, and the API reference.

- **[Documentation (English)](https://sashimi343.github.io/D3Fragments/en/)**
- [Quick start](https://sashimi343.github.io/D3Fragments/en/getting-started/01-quick-start.html)
- [Demo application](demo/01_BasicWebApp): a library lending management application built with D3Fragments

## License

D3Fragments is licensed under the [MIT License](LICENSE).
