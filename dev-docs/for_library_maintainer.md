# ライブラリメンテナー向け情報

## パッケージ作成

```
dotnet pack D3Fragments.slnx -c Release -o artifacts
```

- ビルド時に `scripts/SeparateDocs.ps1` が XML ドキュメントを en(既定) / ja に分離する（要 PowerShell 7 `pwsh`）
- ローカル版を区別する場合は `--version-suffix local.1` などを付与

## 未公開の変更をデモアプリで確かめる

デモアプリ（`demo/`）は NuGet のパッケージを参照するため、未公開の変更はローカルで作ったパッケージで確かめる。

1. `dotnet pack D3Fragments.slnx -c Release -o artifacts --version-suffix local.1` でパッケージを作る（バージョンは `<VersionPrefix>-local.1`）
2. デモアプリのフォルダに、`artifacts/` を参照する `nuget.config` を置く

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <!-- 未公開の D3Fragments をリポジトリで作ったパッケージから参照する。公開後は削除する -->
     <packageSources>
       <add key="D3Fragments-local" value="../../artifacts" />
     </packageSources>
   </configuration>
   ```

3. デモアプリの `Directory.Packages.props` の D3Fragments のバージョンを、作ったパッケージのバージョンにする

- 同じバージョンのパッケージは NuGet のキャッシュ（`~/.nuget/packages`）から使われるため、作り直すたびに `local.2`、`local.3` と番号を上げる
- `nuget.config` とローカル版のバージョンは、リリースの前に必ず元に戻す（下の「リリース」を参照）

## リリース

`main` 上のコミットに `v*` タグを push すると、`release.yml` が NuGet への公開、ドキュメントサイトの公開、GitHub Release の下書きの作成を行う。
タグを打つ前に、リリース用のブランチで次の変更を行い、`main` にマージする。

1. `Directory.Build.props` の `VersionPrefix` を新しいバージョンにする
2. `src/Analyzers/D3Fragments.Analyzers/AnalyzerReleases.Unshipped.md` の規則を、`AnalyzerReleases.Shipped.md` の先頭に `## Release <バージョン>` として移す
3. デモアプリ（`demo/*/Directory.Packages.props`）の D3Fragments の参照を新しいバージョンにし、ローカルのパッケージを参照する `nuget.config` があれば削除する
4. ドキュメント（`docs/`）が新しい機能に追いついているか確認する

マージ後の手順:

```bash
git switch main && git pull
git tag v0.3.0
git push origin v0.3.0
```

- タグ名の `v` を除いた部分がパッケージのバージョンになる。`-` を含む場合（`v0.3.0-preview.1` など）はプレリリースになる
- `release.yml` は、タグのコミットが `main` に含まれていない場合は失敗する
- ワークフローが成功したら、GitHub Release の下書きのリリースノートを確認して公開する
- デモアプリは公開されたパッケージで復元・ビルドできることを確認する（NuGet への反映には少し時間がかかる）
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
  - アナライザーの `RegisterCompilationStartAction` の中（規則の登録を行うかどうかの早期リターンなど）
- `D3Fragments.AspNetCore` の Identity 用の拡張メソッド（`D3Fragments.AspNetCore.Identity`）のテストは、記録用の認証サービスで標準の `SignInManager` と比べるため `D3Fragments.Identity.EFCore.Tests` にある。
  `D3Fragments.AspNetCore` は両方のテストプロジェクトを指定して実行する

  ```bash
  cd tests/D3Fragments.AspNetCore.Tests
  dotnet dotnet-stryker -f ../../stryker-config.json --project D3Fragments.AspNetCore.csproj --test-project D3Fragments.AspNetCore.Tests.csproj --test-project ../D3Fragments.Identity.EFCore.Tests/D3Fragments.Identity.EFCore.Tests.csproj
  ```

- Testcontainers を使う `D3Fragments.Testing.EFCore.Tests` は Docker が必要
