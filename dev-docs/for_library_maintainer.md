# ライブラリメンテナー向け情報

## パッケージ作成

```
dotnet pack D3Fragments.slnx -c Release -o artifacts
```

- ビルド時に `scripts/SeparateDocs.ps1` が XML ドキュメントを en(既定) / ja に分離する（要 PowerShell 7 `pwsh`）
- ローカル版を区別する場合は `--version-suffix local.1` などを付与
## ドキュメントサイト

`docs/<lang>` の Markdown と、XML ドキュメントコメントから生成した API リファレンスを DocFX で言語別にビルドする。
このフォルダ（`dev-docs/`）はリポジトリ内でのみ参照する文書の置き場で、サイトには含めない。

```pwsh
dotnet build D3Fragments.slnx -c Release
dotnet tool restore
./scripts/BuildDocs.ps1
dotnet docfx serve _site   # http://localhost:8080
```

- `scripts/BuildDocs.ps1` は言語ごとに dll と分割済み XML をステージングして DocFX を実行し、`_site/<lang>` を生成する
- `docs/<lang>/toc.yml`（ナビバー）は手書き。各セクション（`guide/` など）の `toc.yml` は `scripts/GenerateToc.ps1` がファイル名順に自動生成する（`index.md` が先頭）
- 生成物（`_site/`、`docs/*/api/`、`docs/*/*/toc.yml`）はコミットしない
- 公開は `release.yml`（タグ `v*` の push）で NuGet パッケージと同時に行う。PR の CI でもビルドだけ行い、リンク切れを検出する
- GitHub Pages の公開元は Settings → Pages → Source を **GitHub Actions** にしておく

## テスト品質の確認

手動で実行する（CI には未組み込み）。合格ラインはパッケージごとに分岐カバレッジ 80% 以上、ミューテーションスコア 70% 以上。
`D3Fragments.Analyzers.CodeFixes` は到達しないコードのため基準未満を許容している。ツールは `dotnet tool restore` で入る。

### カバレッジ

```bash
dotnet build D3Fragments.slnx -c Debug
dotnet dotnet-coverage collect "dotnet test D3Fragments.slnx -c Debug --no-build" -f cobertura -o TestResults/coverage.cobertura.xml
dotnet reportgenerator -reports:TestResults/coverage.cobertura.xml -targetdir:TestResults/coverage -reporttypes:"TextSummary;Html" -assemblyfilters:"+D3Fragments*;-*.Tests" -classfilters:"-System.*;-Microsoft.*"
```

- テストランナーが MTP のため、`coverlet.collector`（`--collect`）は使えない
- `-classfilters` でソースジェネレータの生成コード（Regex、OpenAPI）を除外する

### 静的チェック

```bash
dotnet run scripts/TestQualityCheck.cs
```

アサーションのないテスト、Skip、Sleep、テストのない public 型・診断 ID などを検出する。指摘があれば終了コード 1。

### ミューテーションテスト

設定はルートの `stryker-config.json`。対象の src プロジェクトをテストするテストプロジェクトのディレクトリで、パッケージごとに実行する。

```bash
cd tests/D3Fragments.EFCore.Tests
dotnet dotnet-stryker -f ../../stryker-config.json --project D3Fragments.EFCore.csproj
cd ../..
dotnet run scripts/StrykerSummary.cs -- tests/D3Fragments.EFCore.Tests/StrykerOutput --threshold 70
```

- レポートは各テストプロジェクトの `StrykerOutput/`（コミットしない）。`StrykerSummary.cs` はその中の最新のレポートを集計する
- `StrykerSummary.cs` は `ConfigureAwait(false → true)` の等価ミュータントを除いたスコアを出す。
  Stryker の `ignore-methods` は待機している呼び出しの中の変異（`SaveChangesAsync` の削除など）まで除外するため使わない
- 次の変異は Stryker が有効化できず、誤って「生存」になる。手で変異を入れてテストが失敗するか確かめる
  - static 初期化子の中（集計結果に `(static)` と表示される）や、static 初期化から呼ばれる処理（`TypeSafeEnum` の登録など）
  - EF Core のモデル構築（規約、`OnModelCreating`、`ConfigureConventions`）。モデルがプロセス内でキャッシュされるため
- Testcontainers を使う `D3Fragments.Testing.EFCore.Tests` は Docker が必要
