---
name: analyzer-rule
description: >
  D3Fragments.Analyzers にアナライザの規則（DFRxxxx）を追加・変更する手順と、更新が必要なファイルの一覧。
  「アナライザの規則を追加して」「DFR を作って」「警告を出すようにして」「既存の規則の検出範囲を変えて」と依頼されたら使う。
---

# アナライザの規則の追加・変更

規則の仕様の正本は `docs/ja/analyzers/DFRxxxx.md`。実装とドキュメントは同じ変更で更新する。

## 先に決めること（ユーザーと合意する）

- **新しい規則か、既存の規則の拡張か**: 検出の理由と直し方が既存の規則と同じなら、新しい ID を作らず拡張する（例: DFR0002 のモジュールレイヤーの禁止リストへの追加）
- **ID**: `DFR` + 4 桁の連番（最後の番号の次）
- **カテゴリ**: 層や参照の向きの問題は `Architecture`、型やメンバーの作り方の問題は `Design`
- **検出する範囲と、しない範囲**: 誤検知を避ける除外条件（派生クラスの中、生成コードなど）
- **コード修正を付けるか**

既定の重大度はすべて `Warning`（`DiagnosticDescriptors.Create` で固定）。利用者のビルドで `TreatWarningsAsErrors` だとエラーになるため、既存の規則の検出範囲を広げる変更はリリースノートで破壊的変更として案内する。

## 更新するファイル

| ファイル | 内容 |
|---|---|
| `src/Analyzers/D3Fragments.Analyzers/Rules/<Name>Analyzer.cs` | 規則の実装。先頭のコメントに規則 ID と検出内容を書く |
| `.../DiagnosticDescriptors.cs` | 記述子の追加（`Create("DFRxxxx", カテゴリ)`） |
| `.../Resources/DiagnosticResources.resx` と `.ja.resx` | `DFRxxxx_Title` と `DFRxxxx_MessageFormat`（英語・日本語）。このプロジェクトは `ResourceManager` を直接使うため `Designer.cs` はない |
| `.../Common/KnownTypes.cs` | 新しく参照する型（`GetTypeByMetadataName`。見つからなければ null） |
| `.../AnalyzerReleases.Unshipped.md` | 新しい規則の行（リリース時に `Shipped.md` へ移す） |
| `tests/D3Fragments.Analyzers.Tests/Rules/<Name>AnalyzerTests.cs` | テスト（下記） |
| `docs/{en,ja}/analyzers/DFRxxxx.md` | 規則のページ（既存の規則と同じ見出しの構成。プロパティ表のタイトルはリソースと一字一句同じにする） |
| `docs/{en,ja}/analyzers/index.md` | 規則の一覧に `[DFRxxxx: タイトル](DFRxxxx.md)` の行を追加 |

コード修正を付ける場合は `src/Analyzers/D3Fragments.Analyzers.CodeFixes/` に `CodeFixProvider` を追加し、タイトルは resx の `CodeFix_*` に英日で追加し、`DiagnosticDescriptors.Resources` から取得する。
関係する既存の規則のページ（「関連する規則」）や、ガイドの説明も必要に応じて更新する。

## 実装の決まり

- `ConfigureGeneratedCodeAnalysis(None)` と `EnableConcurrentExecution()` を呼ぶ
- `RegisterCompilationStartAction` の中で `KnownTypes.Create` を呼び、必要な型がない場合や、アセンブリに `ArchitectureLayerAttribute` がない場合（`ArchitectureLayerResolver.Resolve(...) == None`）は何も登録せずに return する
- 型は名前の文字列ではなくシンボル（`KnownTypes`）で比較する

## テスト

`Helpers/AnalyzerVerifier<TAnalyzer>`（コード修正は `CodeFixVerifier`）を使い、`{|#0:...|}` で位置を指定する。テスト名は `<条件>_ReportsDiagnostic` / `<条件>_ReportsNothing` のように、条件と結果を書く。

- 警告が出るケース: 検出対象のすべてのパターン（オーバーロード、派生型など）
- 警告が出ないケース: 除外条件、`ArchitectureLayerAttribute` がないアセンブリ、対象の型を参照していないコンパイル
- テストのコードは、テストのプロセスが読み込んでいるアセンブリを参照してコンパイルされる（`Helpers/TestReferences.cs`）。新しい外部パッケージの型を使う場合は、テストプロジェクトにそのパッケージの参照を追加する

`DocumentationConsistencyTests` が、記述子とドキュメントの対応、タイトルの一致、`index.md` の一覧を検証する。`scripts/TestQualityCheck.cs` も、診断 ID ごとに警告が出るテストと出ないテストがあるかを検査する。
