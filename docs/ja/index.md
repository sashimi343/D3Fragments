# D3Fragments

D3Fragments はドメイン駆動設計（DDD）に基づいた .NET のアプリケーション開発を支援するライブラリです。
エンティティや値オブジェクトなどの部品、Command / Query の実行基盤など、多くのプロジェクトで共通的に作成するクラス群を再利用可能な形で提供します。
本ライブラリは主に ASP.NET Core アプリケーションでの利用を想定していますが、コンソールアプリケーションなどにも部分的な適用が可能です。

## 特徴

- ドメインモデルの部品：集約ルート、エンティティ、値オブジェクト、型安全な列挙型、`Result` 型、ドメインイベントの基底クラスを提供します。
- ユースケースの実行基盤：Command / Query を Handler で処理します。Handler は保存処理を書かず、Handler が正常に終わったら Pipeline が変更を保存します。
- EF Core 連携：`D3DbContext` が値オブジェクトや `UuidV7` を DB の列にマッピングし、同時更新の競合を検出します。Unit of Work は変更を保存するときに、ドメインイベントをハンドラに配信します。
- ASP.NET Core 連携：ドメインの例外や失敗した `Result` を、Problem Details 形式のエラー応答に変換します。
- アナライザ：レイヤー間の参照違反など、設計ガイドラインからの逸脱をビルド時に警告します。

## パッケージ

| パッケージ | 内容 |
|---|---|
| `Sashimi343.D3Fragments` | ドメインモデルの部品（エンティティ、値オブジェクト、`Result` など） |
| `Sashimi343.D3Fragments.Application` | Command / Query の実行基盤、Pipeline Behavior、ドメインイベントハンドラ |
| `Sashimi343.D3Fragments.EFCore` | EF Core 連携（`D3DbContext`、リポジトリ基底クラス、Unit of Work） |
| `Sashimi343.D3Fragments.AspNetCore` | ASP.NET Core 連携（エラー応答、CSRF 対策） |
| `Sashimi343.D3Fragments.AspNetCore.OpenApi` | OpenAPI ドキュメントを D3Fragments の型に合わせる |
| `Sashimi343.D3Fragments.Testing` | テスト用のアサーションとヘルパー |
| `Sashimi343.D3Fragments.Testing.EFCore` | 実 DB を使ったテストの基盤 |
| `Sashimi343.D3Fragments.Analyzers` | 設計ガイドラインの準拠を検査するアナライザ |

## 次に読むページ

- [はじめに](getting-started/index.md)：インストールと最小構成のアプリケーション
- [ガイド](guide/index.md)：各機能の使い方
- [アナライザ](analyzers/index.md)：規則の一覧と適用方法
- [API リファレンス](api/D3Fragments.yml)：XML ドキュメントコメントから生成した API 仕様
