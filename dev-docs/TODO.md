# D3Fragments バックログ

今後の改修予定。書式は [TODO.md](https://github.com/todomd/todo.md) に従う（`###` が列、末尾の `#タグ` で種類を表す）。

### バックログ

- [ ] 値オブジェクトのコレクションをマッピングする手段を用意する #enhancement #efcore  
  - `List<Email>` のような値オブジェクトのコレクションは、`ComplexValueObject` で登録できない
  - 何も登録しないと、`UnmappedValueObjectConvention` が「ComplexValueObjectで登録してください」と案内するが、案内どおり `builder.ComplexValueObject(x => x.Aliases)` と書くとコンパイルは通り、実行時に EF Core の例外（`The property 'Value' cannot be added to the type 'Mailbox.Aliases#List<Email>' ...`）になる
  - 原因: `ComplexValueObject` の型制約が `where TValueObject : class` だけで、コレクション型も受け付けてしまい、その型に `Value` プロパティを登録しようとする
  - 方針案: コレクション版（EF Core の `ComplexCollection` を使うもの）を追加する。または、コレクションの場合は未対応であることをエラーメッセージで案内する。`ComplexCollection` とライブラリの規約（列名の設定など）を組み合わせて動くかは未検証
- [ ] テスト品質のチェックを CI のゲートに組み込む #ci #test  
  - 基準（パッケージごと）: 分岐カバレッジ 80% 以上、ミューテーションスコア 70% 以上、アサーションのないテスト・フレーキーなテストが0件、テストのない public 型と診断 ID が0件
  - 計測: `dotnet-coverage` + ReportGenerator、Stryker.NET（`--test-runner mtp`）、`dotnet run scripts/TestQualityCheck.cs`
  - 例外: `D3Fragments.Analyzers.CodeFixes` は到達しないコードのため基準未満（ミューテーション 68.4%、分岐 76.9%）を許容する
  - Stryker の設定は `stryker-config.json`、集計は `scripts/StrykerSummary.cs`（`ConfigureAwait(false→true)` の等価ミュータントを除外する。Stryker の `ignore-methods` は本物の変異まで除外するため使えない）
  - static 初期化子・EF Core のモデル構築時に効く変異は Stryker が有効化できず、誤って「生存」になる。ゲートの判定でどう扱うか（手動確認の結果を除外リストとして持つ等）を決める
  - アナライザーの `RegisterCompilationStartAction` の中の変異（DFR0017 の早期リターン等）も、誤って「生存」になる場合がある
  - `D3Fragments.AspNetCore` の Identity 用の拡張メソッドのテストは `D3Fragments.Identity.EFCore.Tests` にあるため、Stryker は両方のテストプロジェクトを `--test-project` で指定して実行する
- [ ] ASP.NET Core Identity 連携のドキュメントを書く #docs #identity  
  - 既存の記述の更新: `D3DbContext` の継承を前提とした記述（`guide/02-service-registration.md` の `TContext` の制約、`getting-started/01-quick-start.md`）、`ApplyD3FragmentsConventions` の説明、パッケージ一覧（`docs/*/index.md`、`getting-started/index.md`）への `D3Fragments.Identity.EFCore` の追加
  - Identity 連携のガイド（新規）: `AddD3FragmentsStores` と `ApplyD3FragmentsConventions` による構成（`IdentityDbContext` の継承）、`IdentitySchemaVersions.Version3`（パスキー）、ログイン・二要素認証・回復コード・パスキー・外部ログイン・ログアウトの Command と Controller の例（DB 側の `Verify*` と Cookie 側の拡張メソッドの分け方、失敗は例外ではなく結果で返すこと）、自分の security stamp を変える操作の後の `RefreshSignInAsync`
  - 注意点: `AutoSaveChanges = false` による違い（同じ保存単位でのユーザー名の重複は `DbUpdateException`、競合は `ConflictOccurredException`）、Commit 後ハンドラーでは UserManager を直接使えないこと、ロックアウトの解除とトークンの期限は `TimeProvider` に従わないこと、パスキーを削除してもセッションは切れないこと、二要素認証待ちの Cookie は有効期限が延長されること、回復コードではロックアウトを確認しないこと（Identity と同じ）、メールのコード送信の回数制限はアプリで行うこと
  - デモアプリでの実装例
- [ ] プレゼンテーション層からの UserManager の書き込みメソッドの呼び出しを検出する #enhancement #analyzer #identity  
  - DFR0002 は Identity のユーザー型（`Infrastructure_IdentityUser`）をプレゼンテーション層から参照できるため、画面で `UserManager<TUser>` を受け取って `CreateAsync`・`SetTwoFactorEnabledAsync` などを直接呼べる
  - `AddD3FragmentsStores` のストアは保存を UnitOfWork に任せる（`AutoSaveChanges = false`）ため、Command の外での変更はエラーにならずに保存されない
  - 方針案: DFR0017 と同じく、プレゼンテーション層からの呼び出しを警告し、Command での実行を案内する。書き込みとみなすメソッドの一覧（`Create*`・`Update*`・`Set*`・`Add*`・`Remove*`・`Reset*`・`Generate*RecoveryCodes`・`AccessFailed` など）をどう管理するかを決める
- [ ] アプリケーション層の認証インターフェースを提供する（案A） #enhancement #identity  
  - 現状（案B）は、Identity の `SignInManager` を DB 側と Cookie 側に分けた部品だけを提供し、アプリケーション層のインターフェースはアプリが定義する
  - 案A: `D3Fragments.Application` に Identity に依存しない認証のインターフェースと結果の型（汎用のアカウント ID）を、`D3Fragments.Identity.EFCore` にその実装（案Bの拡張メソッドを使う）を追加する。案Bの公開 API は変えない
  - 認証方式をライブラリが固定しないよう、インターフェースの粒度と拡張方法を設計する

### 対応中

### 完了 ✓
