# ドメイン層

ドメイン層には業務のルールと状態を表すクラスを置きます。
D3Fragments はドメイン層で使う部品を `Sashimi343.D3Fragments` パッケージで提供します。

| ページ | 主な型 |
|---|---|
| [エンティティと集約](01-entities-and-aggregates.md) | `AggregateRoot`、`Entity`、`EntityBase`、`UuidV7`、`IRepository<TAggregate>`、`DeletionToken<T>` |
| [値オブジェクト](02-value-objects.md) | `ValueObjectBase<TSelf, TPrimitive>`、`IValueObject<TSelf, TPrimitive>` |
| [型安全な列挙型](03-type-safe-enum.md) | `TypeSafeEnum<TSelf, TKey>` |
| [Result とエラー](04-result-and-errors.md) | `Result`、`Result<T>`、`Error`、`ErrorType`、`DomainException` |
| [入力値の検証](05-validation.md) | `StringValidator`、`NumberValidator<T>`、`DateOnlyValidator`、`DateTimeOffsetValidator` |
| [ドメインイベント](06-domain-events.md) | `IDomainEvent` |
| [ドメインルール](07-domain-rules.md) | `IDomainRule<TContext>` |
