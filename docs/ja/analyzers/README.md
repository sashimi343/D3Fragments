# D3Fragments アナライザ

D3Fragments.Analyzers では D3Fragments パッケージを利用する場合に推奨される設計・実装ガイドラインへの準拠をサポートします。

## 適用方法

D3Fragments.Analyzers を適用するためには以下の手順を実施してください。

### アナライザ参照の追加

各プロジェクトに以下の参照を追加します。

**dotnetコマンドで追加する場合**

```bash
dotnet add package Sashimi343.D3Fragments.Analyzers
```

**csprojに直接記述する場合**

```csproj
<ItemGroup>
  <PackageReference Include="Sashimi343.D3Fragments.Analyzers">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### アセンブリ アーキテクチャ層の設定

D3Fragments.Analyzers を追加した各プロジェクトの `AssemblyInfo.cs` にて、該当アセンブリが属するアーキテクチャ層を明示します。
`ArchitectureLayerAttribute` は D3Fragments パッケージ（`Sashimi343.D3Fragments`）に含まれるため、各プロジェクトから D3Fragments パッケージを参照している必要があります。
アーキテクチャ層の詳細については [D3Fragmentsのアーキテクチャ](../architecture.md) にも記載しています。

アーキテクチャ層は以下のいずれかです。

| アーキテクチャ層                                 | 設定値                                 |
| :----------------------------------------------- | :------------------------------------- |
| プレゼンテーション層（ユーザインターフェース層） | `ArchitectureLayerType.Presentation`   |
| アプリケーション層                               | `ArchitectureLayerType.Application`    |
| ドメイン層                                       | `ArchitectureLayerType.Domain`         |
| インフラストラクチャ層                           | `ArchitectureLayerType.Infrastructure` |

対象のアセンブリが複数のアーキテクチャ層を実装している場合、複数の `ArchitectureLayerType` を `|` で連結して指定できます。

以下は `AssemblyInfo.cs` の設定例です。

```csharp
using D3Fragments;

[assembly: ArchitectureLayer(ArchitectureLayerType.Application | ArchitectureLayerType.Domain)]
```

## 規則の一覧

| 規則 ID と警告 | 説明 |
|---|---|
| [DFR0001: アーキテクチャ参照違反（プロジェクト間参照）](DFR0001.md) | プロジェクト間の依存関係が不適切です。抽象度の高いアセンブリは抽象度の低いアセンブリに依存すべきではありません。 |
| [DFR0002: アーキテクチャ参照違反（モジュール間参照）](DFR0002.md) | モジュール（クラス・インターフェース）間の依存関係が不適切です。 |
| [DFR0003: エンティティクラスは公開セッターをもつべきではありません](DFR0003.md) | エンティティクラスに公開セッターが定義されています。エンティティの状態変化は操作によって行われるべきであり、公開セッターによる値の書き換えは推奨しません。 |
| [DFR0004: 集約の一部を取得するリポジトリメソッドを定義しないでください](DFR0004.md) | リポジトリメソッドの戻り値が集約ルート型ではなくその部分型になっています。リポジトリメソッドでは集約ルート単位でデータ取得を行う必要があります。 |
| [DFR0005: 集約の一部を更新するリポジトリメソッドを定義しないでください](DFR0005.md) | リポジトリメソッドの引数に集約の部分型が使用されています。このようなメソッドは集約の部分更新を可能とし、集約の整合性を壊す可能性があるため実装しないでください。 |
| [DFR0006: TypeSafeEnum型のコンストラクタをクラス外部に公開しないでください](DFR0006.md) | `TypeSafeEnum<TSelf, TKey>` を継承したクラスのコンストラクタの可視性が private ではありません。クラス外部にコンストラクタを公開すると意図しない列挙子を生成可能になるため、コンストラクタの公開を避けるべきです。 |
| [DFR0007: 自己参照型パラメーター TSelf には宣言している型自身を指定してください](DFR0007.md) | `TypeSafeEnum<TSelf, TKey>` などの自己参照型パラメーター `TSelf` に、宣言しているクラス自身以外の型が指定されています。実行時に型変換の例外が発生します。 |
| [DFR0008: TypeSafeEnum の値は static readonly フィールドで定義してください](DFR0008.md) | `TypeSafeEnum<TSelf, TKey>` の値が static readonly フィールド以外で定義されています。値の重複定義による例外や、値の差し替えが発生する可能性があります。 |
| [DFR0009: エンティティは変更可能なコレクションを公開すべきではありません](DFR0009.md) | エンティティクラスが変更可能なコレクションを公開しています。セッターを非公開にしても、コレクションの操作によりエンティティの状態を外部から変更できてしまいます。 |
| [DFR0010: 値オブジェクトは公開セッターをもつべきではありません](DFR0010.md) | 値オブジェクトに公開セッターが定義されています。値オブジェクトは不変であるべきです。 |
| [DFR0011: コントローラはドメイン層のクラスをコンストラクタで注入すべきではありません](DFR0011.md) | コントローラのコンストラクタでドメイン層のクラス（ドメインサービス等）が注入されています。コントローラはアプリケーション層の処理を `IRequestExecutor<TRequest, TResponse>` 経由で呼び出すべきです。 |
