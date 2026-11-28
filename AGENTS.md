# D3Fragments

DDD（ドメイン駆動設計）に基づく .NET アプリケーションの共通部品を提供する NuGet ライブラリ（パッケージ ID は `Sashimi343.*`）。
ドメインモデルの部品、Command / Query の実行基盤、EF Core・ASP.NET Core・ASP.NET Core Identity との連携、設計ガイドラインを検査するアナライザを含む。
.NET 10 以降が対象。

## 設計思想

- **ドメインモデル貧血症を防ぐ**: Handler の責務は「集約の取得」「集約・値オブジェクト・ドメインルールの呼び出し」「外部とのオーケストレーション」に限る。状態の判定と変更は集約へ、値の検証は値オブジェクトへ置く方が自然に書ける部品を提供する。
- **書き方で迷わせない**: AI エージェントや初見の開発者でも同じ設計を再現できるように、正しい書き方を1つにし、外れたコードはアナライザで警告する。
- **横断的関心事は Pipeline に置く**: 変更の保存とトランザクションは Pipeline Behavior が行い、Handler・Repository は保存しない。ドメインイベントは Commit の前後で配信する。
- **集約の境界を型で守る**: Repository は集約ルートだけに作れる。削除は `DeletionToken` を経由する。汎用的な CRUD は公開しない。
- **アプリの選択を奪わない**: DBMS・認証方式・ログイン手順などはアプリが選ぶ。ライブラリが提供するのは、それらを D3Fragments のアーキテクチャに合わせるための部品だけ。
- **必要になってから作る**: 実際に必要性が確認できた機能だけを追加する。

## 言語ポリシー

D3Fragments では、開発者 (Kohei Kakimoto) の母国語である日本語を主にサポートしつつも、より多くの人が利用できるよう公開ドキュメントに関しては英語も併用する。
それぞれの実装箇所・内容ごとに使用する言語は以下のとおりとする。

| 実装箇所・内容 | 言語 |
|---|---|
| ドキュメント (`docs/`、リポジトリ直下の `README` ) | 英語、日本語 |
| public/protectedメンバに付与するXMLコメント | 英語、日本語 ( `xml-commenting` スキル参照) |
| 例外メッセージ | 英語、日本語（`.resx`で言語別のリソースファイルを作成） |
| GitHub Releasesのリリースノート | 英語（※リリースノートは単一の文書であるため、より多くの人が読める英語を採用する） |
| Gitコミットメッセージ | 英語（※リリースノートの自動生成に使用するため） |
| Pull Requestのタイトル | 英語（※リリースノートの自動生成に使用するため） |
| Pull Requestの本文 | 日本語（※開発者の母国語） |
| デモアプリ (`demo/`) | 英語 |
| 上記以外 | 日本語 |

## ディレクトリ構成

| パス | 内容 |
|---|---|
| `src/` | パッケージ本体（1 プロジェクト = 1 パッケージ）。`src/Analyzers/` はアナライザとコード修正 |
| `tests/` | `src/` のプロジェクトごとのテスト（`<パッケージ名>.Tests`） |
| `demo/` | ライブラリを使ったデモアプリ（それぞれ独立したソリューション） |
| `docs/{en,ja}/` | ドキュメントサイトの Markdown（英語・日本語）。API リファレンスと各セクションの `toc.yml` はビルド時に生成 |
| `docfx/` | DocFX の設定とテンプレート |
| `dev-docs/` | メンテナー向けの文書（サイトには含めない） |
| `scripts/` | ビルド・ドキュメント・テスト品質の補助スクリプト |

## 主要なコマンド

ビルドには PowerShell 7（`pwsh`）が必要（`src/` のビルド後に XML ドキュメントを en / ja に分けるため）。テストランナーは Microsoft.Testing.Platform。

```pwsh
dotnet tool restore                                  # docfx, Stryker などのローカルツール
dotnet pack D3Fragments.slnx -c Release -o artifacts # パッケージ作成
./scripts/BuildDocs.ps1                              # ドキュメントサイトを _site/<lang> に生成（先に Release ビルド）
dotnet docfx serve _site                             # http://localhost:8080
dotnet run scripts/TestQualityCheck.cs               # テスト品質の静的チェック
```

- テスト品質（カバレッジ・ミューテーション）の計測は `dotnet-test-quality` スキルと `dev-docs/for_library_maintainer.md` を参照
- 未公開の変更をデモアプリで確かめる手順と、リリースの手順は `dev-docs/for_library_maintainer.md` を参照
- `D3Fragments.Testing.EFCore.Tests` は Testcontainers を使うため Docker が必要

## 守ること

- 警告はエラーとして扱われる（`TreatWarningsAsErrors`）
- パッケージ本体 (`src/`) におけるコンパイラ警告の握りつぶし (`#pragma warning disable ....`) は原則として禁止。技術的に不可避の場合は理由を説明し、ユーザの承認を得ること
- public / protected な API の XML ドキュメントコメントは英語・日本語の両方で書く（`xml-commenting` スキル）。例外メッセージ等のリソースも `*.resx`（英語）と `*.ja.resx` の両方を更新する
- resx にエントリを追加・削除したら、対応する `*.Designer.cs` も手で更新する（`ResXFileCodeGenerator` は Visual Studio の中でしか動かず、CLI のビルドでは再生成されない）
- アナライザの規則の追加・変更は `analyzer-rule` スキルの手順に従う（ドキュメントの更新も同じ変更で行う）
- アナライザ以外の機能のドキュメント（`docs/`）は機能実装時に適宜更新する