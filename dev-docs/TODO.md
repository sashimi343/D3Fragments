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

### 対応中

### 完了 ✓
