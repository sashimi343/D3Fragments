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
  <a href="README.md">English</a> | 日本語
</p>

D3Fragments はドメイン駆動設計（DDD）に基づいた .NET のアプリケーション開発を支援するライブラリです。
ドメインモデルの部品、Command / Query の実行基盤、EF Core と ASP.NET Core との連携機能など、多くのプロジェクトで共通して作るクラスを再利用できる形で提供します。

## 特徴

- ドメインモデルの部品：集約ルート、エンティティ、値オブジェクト、型安全な列挙型、`Result`、ドメインイベント
- ユースケースの実行基盤：Command / Query を Handler で処理し、変更の保存は Pipeline が行う
- EF Core 連携：値オブジェクトと `UuidV7` のマッピング、楽観的同時実行制御、ドメインイベントの配信
- ASP.NET Core 連携：ドメインのエラーを Problem Details 形式の応答に変換する
- アナライザ：設計ガイドラインからの逸脱をビルド時に警告する

## インストール

.NET 10 以降が必要です。
アプリケーションの層に応じたパッケージを追加します。

```bash
dotnet add package Sashimi343.D3Fragments                # ドメイン層
dotnet add package Sashimi343.D3Fragments.Application    # アプリケーション層
dotnet add package Sashimi343.D3Fragments.EFCore         # インフラストラクチャ層
dotnet add package Sashimi343.D3Fragments.AspNetCore     # プレゼンテーション層
```

| パッケージ | 内容 |
|---|---|
| `Sashimi343.D3Fragments` | ドメインモデルの部品 |
| `Sashimi343.D3Fragments.Application` | Command / Query の実行基盤 |
| `Sashimi343.D3Fragments.EFCore` | EF Core 連携 |
| `Sashimi343.D3Fragments.AspNetCore` | ASP.NET Core 連携 |
| `Sashimi343.D3Fragments.AspNetCore.OpenApi` | OpenAPI 連携 |
| `Sashimi343.D3Fragments.Testing` | テスト用のヘルパー |
| `Sashimi343.D3Fragments.Testing.EFCore` | 実 DB を使うテストのヘルパー |
| `Sashimi343.D3Fragments.Analyzers` | 設計ガイドラインを検査するアナライザ |

## ドキュメント

使い方、各機能のガイド、API リファレンスはドキュメントを参照してください。

- **[ドキュメント（日本語）](https://sashimi343.github.io/D3Fragments/ja/)**
- [クイックスタート](https://sashimi343.github.io/D3Fragments/ja/getting-started/01-quick-start.html)
- [デモアプリケーション](demo/01_BasicWebApp)：D3Fragments で作った図書館の貸出管理アプリケーション

## ライセンス

D3Fragments は [MIT ライセンス](LICENSE)で公開しています。
