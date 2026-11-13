# はじめに

D3Fragments を使うアプリケーションの前提条件と、プロジェクトの構成、パッケージのインストール方法を説明します。

## 前提条件

- .NET 10 以降
- EF Core 10 以降（`Sashimi343.D3Fragments.EFCore` を使う場合）
- ASP.NET Core 10 以降（`Sashimi343.D3Fragments.AspNetCore` を使う場合）

## プロジェクトの構成

D3Fragments ではアプリケーションをプレゼンテーション層・アプリケーション層・ドメイン層・インフラストラクチャ層の4つのプロジェクトに分けて作ることを想定しています。
層の考え方と依存関係のルールは [アーキテクチャ](../guide/01-architecture.md) で説明します。

各プロジェクトが参照するパッケージは次のとおりです。

| プロジェクト（例） | 層 | 参照するパッケージ |
|---|---|---|
| `MyApp.Domain` | ドメイン層 | `Sashimi343.D3Fragments` |
| `MyApp.Application` | アプリケーション層 | `Sashimi343.D3Fragments.Application` |
| `MyApp.Infrastructure` | インフラストラクチャ層 | `Sashimi343.D3Fragments.EFCore`、DB プロバイダー（`Npgsql.EntityFrameworkCore.PostgreSQL` など） |
| `MyApp.Web` | プレゼンテーション層 | `Sashimi343.D3Fragments.AspNetCore`（必要に応じて `Sashimi343.D3Fragments.AspNetCore.OpenApi`） |

`Sashimi343.D3Fragments.Application` は `Sashimi343.D3Fragments` に依存しているため、アプリケーション層では `Sashimi343.D3Fragments` を別に参照する必要はありません。
ほかのパッケージも同様です。

### 複数の層を1つのプロジェクトにまとめる

層は4つですが、プロジェクトを必ず4つに分ける必要はありません。
小さなアプリケーションでは複数の層を1つのプロジェクトにまとめることもできます。
たとえばドメイン層とアプリケーション層を `MyApp.Core` にまとめ、プロジェクトを3つにする構成です。

まとめたプロジェクトでは `AssemblyInfo.cs` で属する層を `|` でつないで指定します。
参照するパッケージは、まとめた層で使うものをすべて追加します（この例では `Sashimi343.D3Fragments.Application`）。

```csharp
using D3Fragments;

[assembly: ArchitectureLayer(ArchitectureLayerType.Application | ArchitectureLayerType.Domain)]
```

ただし、1つのプロジェクトの中にある層の間の参照は、プロジェクト参照の検査（[DFR0001](../analyzers/DFR0001.md)）では検出できません。
クラス間の参照の検査（[DFR0002](../analyzers/DFR0002.md) など）は行われますが、検査できる範囲は層ごとにプロジェクトを分けた場合より狭くなります。
層の間の依存関係のルールをアナライザで確実に守りたい場合は、層ごとにプロジェクトを分けてください。

## デモアプリケーション

D3Fragments を使ったアプリケーションの実装例として、図書館の貸出管理アプリケーションを用意しています。
4つのプロジェクトに分けた構成や、各機能の具体的な使い方は、このデモのコードで確認できます。

- [demo/01_BasicWebApp](https://github.com/sashimi343/D3Fragments/tree/main/demo/01_BasicWebApp)（GitHub）

## インストール

各プロジェクトで、上の表のパッケージを追加します。

```bash
dotnet add MyApp.Domain package Sashimi343.D3Fragments
dotnet add MyApp.Application package Sashimi343.D3Fragments.Application
dotnet add MyApp.Infrastructure package Sashimi343.D3Fragments.EFCore
dotnet add MyApp.Web package Sashimi343.D3Fragments.AspNetCore
```

アナライザも使う場合は、すべてのプロジェクトに `Sashimi343.D3Fragments.Analyzers` を追加します。
設定方法は [アナライザ](../analyzers/index.md) を参照してください。

## 次のステップ

[クイックスタート](01-quick-start.md) で、集約1つを登録・取得する Web API を作ります。
