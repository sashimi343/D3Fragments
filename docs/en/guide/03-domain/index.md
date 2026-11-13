# Domain layer

The domain layer contains the classes that represent business rules and state.
D3Fragments provides building blocks for the domain layer in the `Sashimi343.D3Fragments` package.

| Page | Main types |
|---|---|
| [Entities and aggregates](01-entities-and-aggregates.md) | `AggregateRoot`, `Entity`, `EntityBase`, `UuidV7`, `IRepository<TAggregate>`, `DeletionToken<T>` |
| [Value objects](02-value-objects.md) | `ValueObjectBase<TSelf, TPrimitive>`, `IValueObject<TSelf, TPrimitive>` |
| [Type-safe enums](03-type-safe-enum.md) | `TypeSafeEnum<TSelf, TKey>` |
| [Result and errors](04-result-and-errors.md) | `Result`, `Result<T>`, `Error`, `ErrorType`, `DomainException` |
| [Input validation](05-validation.md) | `StringValidator`, `NumberValidator<T>`, `DateOnlyValidator`, `DateTimeOffsetValidator` |
| [Domain events](06-domain-events.md) | `IDomainEvent` |
| [Domain rules](07-domain-rules.md) | `IDomainRule<TContext>` |
