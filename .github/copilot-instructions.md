## D3Fragmentsプロジェクトについて

* DDDに基づいたアプリケーションを開発する際に有用なクラス群をまとめたNuGetパッケージの開発プロジェクト
* 以下のような機能を提供する
  * D3Fragments: Entity, Repository等の共通基底クラス・インターフェース、Result型などの共通的なデータ型等
  * D3Fragments.Application: ユースケース(Handler/Command)処理を実装するための共通クラス・インターフェースを提供
  * D3Fragments.AspNetCore: 再利用可能な認証・認可ロジック、ASP.NET Core共通ミドルウェア（ロギング、例外ハンドラ）、SwaggerUI向けのフィルタ等
  * D3Fragments.EFCore: Repositoryを実装するために機能拡張したDbContextの基底クラス、共通ValueConverter等
  * D3Fragments.Testing: カスタムAssert、テストヘルパ等
  * D3Fragments.Testing.EFCore: 実DB（Testcontainers）を使うテストのためのフィクスチャ・トランザクションロールバック基底クラス
  * D3Fragments.DependencyInjection: D3Fragmentsの各種クラスの依存性注入を簡単に行うためのヘルパを提供
  * D3Fragments.Analyzers: 依存関係チェックなど、D3Fragmentsの思想に準拠した保守性の高いコードを維持するためのアナライザを提供
* 主にASP.NET Core Webアプリケーションでの利用を想定しているが、コンソールアプリケーション等にも部分的に流用可能な作りとする

## 技術スタック

* 言語: C# (.NET)
* テストフレームワーク: xUnit
* 配布方法: NuGetパッケージ