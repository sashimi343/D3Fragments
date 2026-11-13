# ガイド

D3Fragments の各機能の使い方を、層ごとに説明します。
初めて使う場合は、先に [クイックスタート](../getting-started/01-quick-start.md) を読んでください。

| ページ | 内容 |
|---|---|
| [アーキテクチャ](01-architecture.md) | 層の分け方、依存関係のルール、リクエストの処理の流れ |
| [サービスの登録](02-service-registration.md) | DI コンテナへの登録メソッドと、登録されるサービスの一覧 |
| [ドメイン層](03-domain/index.md) | エンティティ、値オブジェクト、型安全な列挙型、`Result` とエラー、検証、ドメインイベント、ドメインルール |
| [アプリケーション層](04-application/index.md) | Command / Query、Pipeline とトランザクション、ドメインイベントハンドラ |
| [EF Core 連携](05-efcore/index.md) | `D3DbContext`、マッピング、リポジトリ、ページング |
| [ASP.NET Core 連携](06-aspnetcore/index.md) | エラー応答、Razor Pages、CSRF 対策、OpenAPI |
| [テスト](07-testing.md) | テストの方針と、テスト用パッケージの使い方 |

コード例の多くは、[デモアプリケーション](https://github.com/sashimi343/D3Fragments/tree/main/demo/01_BasicWebApp)（図書館の貸出管理）から取っています。
ガイドのコード例は説明に必要な部分だけを抜き出しています。全体の実装はデモのコードを参照してください。
