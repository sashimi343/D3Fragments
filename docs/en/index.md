# D3Fragments

D3Fragments is a library that supports .NET application development based on Domain-Driven Design (DDD).
It provides, in reusable form, the classes that many projects end up writing for themselves: building blocks such as entities and value objects, a Command / Query execution pipeline, and more.
The library is mainly intended for ASP.NET Core applications, but parts of it can also be used in other kinds of applications, such as console applications.

## Features

- Domain model building blocks: base classes for aggregate roots, entities, value objects, type-safe enums, the `Result` type, and domain events.
- Use case execution pipeline: Commands and Queries are processed by handlers. Handlers do not save anything themselves; when a handler completes successfully, the pipeline saves the changes.
- EF Core integration: `D3DbContext` maps value objects and `UuidV7` to database columns and detects concurrent update conflicts. The Unit of Work dispatches domain events to their handlers when it saves changes.
- ASP.NET Core integration: converts domain exceptions and failed `Result`s into error responses in the Problem Details format.
- Analyzers: report deviations from the design guidelines, such as invalid references between layers, as warnings at build time.

## Packages

| Package | Contents |
|---|---|
| `Sashimi343.D3Fragments` | Domain model building blocks (entities, value objects, `Result`, and so on) |
| `Sashimi343.D3Fragments.Application` | Command / Query execution pipeline, pipeline behaviors, domain event handlers |
| `Sashimi343.D3Fragments.EFCore` | EF Core integration (`D3DbContext`, repository base classes, Unit of Work) |
| `Sashimi343.D3Fragments.AspNetCore` | ASP.NET Core integration (error responses, CSRF protection) |
| `Sashimi343.D3Fragments.AspNetCore.OpenApi` | Aligns OpenAPI documents with the D3Fragments types |
| `Sashimi343.D3Fragments.Testing` | Assertions and helpers for tests |
| `Sashimi343.D3Fragments.Testing.EFCore` | Infrastructure for tests against a real database |
| `Sashimi343.D3Fragments.Analyzers` | Analyzers that check compliance with the design guidelines |

## Where to go next

- [Getting started](getting-started/index.md): installation and a minimal application
- [Guide](guide/index.md): how to use each feature
- [Analyzers](analyzers/index.md): the list of rules and how to apply them
- [API reference](api/D3Fragments.yml): the API specification generated from the XML documentation comments
