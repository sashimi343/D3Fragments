# Guide

This guide explains how to use each feature of D3Fragments, layer by layer.
If you are new to D3Fragments, read [Quick start](../getting-started/01-quick-start.md) first.

| Page | Contents |
|---|---|
| [Architecture](01-architecture.md) | How to split layers, dependency rules, and how a request is processed |
| [Service registration](02-service-registration.md) | The methods that register services in the DI container, and the list of registered services |
| [Domain layer](03-domain/index.md) | Entities, value objects, type-safe enums, `Result` and errors, validation, domain events, domain rules |
| [Application layer](04-application/index.md) | Commands and Queries, the pipeline and transactions, domain event handlers |
| [EF Core integration](05-efcore/index.md) | `D3DbContext`, mapping, repositories, paging |
| [ASP.NET Core integration](06-aspnetcore/index.md) | Error responses, Razor Pages, CSRF protection, OpenAPI |
| [Testing](07-testing.md) | Testing strategy and how to use the testing packages |

Most code examples are taken from the [demo application](https://github.com/sashimi343/D3Fragments/tree/main/demo/01_BasicWebApp) (library lending management).
The examples in this guide show only the parts needed for the explanation. See the demo code for the complete implementation.
