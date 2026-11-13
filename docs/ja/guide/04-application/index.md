# アプリケーション層

アプリケーション層にはユースケース（Command / Query）とその Handler を置きます。
D3Fragments はユースケースを実行する基盤を `Sashimi343.D3Fragments.Application` パッケージで提供します。

| ページ | 主な型 |
|---|---|
| [Command と Query](01-commands-and-queries.md) | `ICommand<TResponse>`、`IQuery<TResponse>`、`IRequestHandler<TRequest, TResponse>`、`IRequestExecutor<TRequest, TResponse>` |
| [Pipeline とトランザクション](02-pipeline-and-transactions.md) | `IPipelineBehavior<TRequest, TResponse>`、`UnitOfWorkBehavior`、`TransactionBehavior`、`ITransactionalCommand` |
| [ドメインイベントハンドラ](03-domain-event-handlers.md) | `IBeforeCommitDomainEventHandler<TEvent>`、`IAfterCommitDomainEventHandler<TEvent>` |

## サービスの登録

`AddD3FragmentsApplication` に、Handler を定義したアセンブリを渡します。

```csharp
builder.Services.AddD3FragmentsApplication([typeof(RegisterBookCommand).Assembly]);
```

渡したアセンブリにある Handler とドメインイベントハンドラが、自動で登録されます。
`IUnitOfWork` の実装は登録しないため、EF Core 連携の `AddD3FragmentsEFCore` も呼んでください。
登録されるサービスの一覧は [サービスの登録](../02-service-registration.md) を参照してください。
