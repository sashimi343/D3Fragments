# D3Fragmentsのアーキテクチャ

## 全体像

D3Fragments は Clean Architecture をベースとしたアーキテクチャを採用しています。
アーキテクチャの全体像は以下のとおりです。

**TODO: ここにアーキテクチャの全体像を説明する図を追加する**

## アセンブリ アーキテクチャ層

アセンブリ（プロジェクト）単位では以下の4つのアーキテクチャ層に分類する。
各アセンブリが所属するアーキテクチャ層は、`AssemblyInfo.cs` に設定する `ArchitectureLayerAttribute` 属性によって識別する。
`ArchitectureLayerAttribute` は D3Fragments パッケージ（`Sashimi343.D3Fragments`）に含まれます。

アーキテクチャ層は以下のいずれかです。

| アーキテクチャ層                                 | 設定値                                 |
| :----------------------------------------------- | :------------------------------------- |
| プレゼンテーション層（ユーザインターフェース層） | `ArchitectureLayerType.Presentation`   |
| アプリケーション層                               | `ArchitectureLayerType.Application`    |
| ドメイン層                                       | `ArchitectureLayerType.Domain`         |
| インフラストラクチャ層                           | `ArchitectureLayerType.Infrastructure` |

1つのアセンブリに複数のアーキテクチャ層を実装することも可能である。
対象のアセンブリが複数のアーキテクチャ層を実装している場合、複数の `ArchitectureLayerType` を `|` で連結して指定できます。

以下は `AssemblyInfo.cs` の設定例です。

```csharp
using D3Fragments;

[assembly: ArchitectureLayer(ArchitectureLayerType.Application | ArchitectureLayerType.Domain)]
```

## モジュールレイヤー

モジュールレイヤーは各アーキテクチャ層（プレゼンテーション層、アプリケーション層、ドメイン層、インフラストラクチャ層）の中に実装されるクラス・インターフェースを機能ごとに細分化して整理した概念です。
アーキテクチャ層ごとに以下のモジュールレイヤーを定めます。

### モジュールレイヤーの判定

クラス・インターフェースのモジュールレイヤーは、そのクラス・インターフェースが属するアセンブリのアーキテクチャ層と、クラス・インターフェース自体の条件（継承・実装している型）の両方によって判定します。

- `ArchitectureLayerAttribute` が設定されていないアセンブリのクラス・インターフェースは、モジュールレイヤーを持ちません（モジュールレイヤーに依存する規則の検証対象外です）。
- 例えば `EntityBase<TId>` を継承したクラスは、ドメイン層のアセンブリに存在する場合のみ `Domain_Entity` と判定されます。
- 複数のアーキテクチャ層を持つアセンブリでは、それらのアーキテクチャ層のいずれかのモジュールレイヤーの条件に該当すれば、そのモジュールレイヤーと判定します。例えばアプリケーション層＋ドメイン層のアセンブリに `EntityBase<TId>` を継承したクラスが存在する場合、`Domain_Entity` と判定します。
- 複数のアーキテクチャ層を持つアセンブリで、どのモジュールレイヤーの条件にも該当しないクラス・インターフェースは、どのアーキテクチャ層の `*_Other` かを特定できないため、`Other`（アーキテクチャ層不明のその他）と判定します。単一のアーキテクチャ層を持つアセンブリでは、そのアーキテクチャ層の `*_Other` と判定します。
- 1つのクラス・インターフェースが複数のモジュールレイヤーの条件に該当する場合、本ドキュメントでの記載順で最初に該当したモジュールレイヤーと判定します。

### プレゼンテーション層（ユーザインターフェース層）

#### Presentation_Controller

ASP.NET Coreの[PageBase](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.razorpages.pagebase), [ControllerBase](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase)を継承、または [IComponent](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.icomponent)インターフェースを実装したクラスです。

#### Presentation_Other

プレゼンテーション層のクラス・インターフェースのうち、上記のいずれにも該当しないクラス・インターフェースです。

### アプリケーション層

#### Application_Request

`D3Fragments.Application.IRequest<TResponse>` インターフェース、または `D3Fragments.Application.ICommand` インターフェースを直接、または間接的に実装したクラスです。

#### Application_RequestHandler

`D3Fragments.Application.IRequestHandler<in TRequest, TResponse>` インターフェースを直接、または間接的に実装したクラスです。

#### Application_RequestExecutor

`D3Fragments.Application.IRequestExecutor<in TRequest, TResponse>` インターフェースを直接、または間接的に実装したクラスです。

#### Application_DomainEventHandler

`D3Fragments.Application.IDomainEventHandler<in TEvent>` インターフェースを直接、または間接的に実装したクラスです。

#### Application_Other

アプリケーション層のクラス・インターフェースのうち、上記のいずれにも該当しないクラス・インターフェースです。

### ドメイン層

#### Domain_Entity

`D3Fragments.EntityBase<TId>` クラスを直接、または間接的に継承したクラスです。

#### Domain_ValueObject

`D3Fragments.IValueObject<TSelf, TPrimitive>` インターフェースを直接、または間接的に実装したクラスです。

#### Domain_RepositoryInterface

`D3Fragments.IRepository<TAggregate, TId>` インターフェースを直接、または間接的に継承したインターフェースです。

#### Domain_DomainRule

`D3Fragments.IDomainRule<in TContext>` インターフェースを直接、または間接的に実装したクラスです。

#### Domain_Other

ドメイン層のクラス・インターフェースのうち、上記のいずれにも該当しないクラス・インターフェースです。

### インフラストラクチャ層

#### Infrastructure_RepositoryImplementation

`D3Fragments.IRepository<TAggregate, TId>` インターフェースを直接、または間接的に実装したクラスです。

#### Infrastructure_Other

インフラストラクチャ層のクラス・インターフェースのうち、上記のいずれにも該当しないクラス・インターフェースです。
