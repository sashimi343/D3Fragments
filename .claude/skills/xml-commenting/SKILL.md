---
name: xml-commenting
description: NuGetパッケージとして公開する C# クラスの XML ドキュメントコメント（`///`）を記述する際のルールを定義する。 src/ 以下の public/protected な型・メンバーのXMLコメントを作成または修正する際に参照する。
---

# C# XMLコメント記述ガイド

## 目的
このガイドは、C# の XML ドキュメントコメント（`///`）を記述する際のルールを定義する。
AIエージェントは、`src/` 以下の **public / protected の型・メンバー**に対して本ガイドを適用すること。

---

## 基本方針（共通）

1. **API利用者視点で記述する**
   実装手順ではなく、「この API は何をするか」「何を返すか」「いつ例外か」を明確に書く。

2. **簡潔・正確・非あいまい**
   1文を短く保ち、主語述語を明確化する。比喩や口語を使わない。

3. **事実のみを書く**
   推測・感想・曖昧語（「たぶん」「いい感じに」）は禁止。

4. **Microsoft Learn風の情報構造を守る**
   - `<summary>`: 機能の要約（1～2文）
   - `<param>`: 引数の意味・制約
   - `<returns>`: 戻り値の意味
   - `<exception>`: 例外発生条件
   - `<remarks>`: 補足仕様（必要時のみ）
   - `<value>`: プロパティ値の意味
   - `<typeparam>`: 型パラメーターの意味

5. **タグ優先順位**
   - `<summary>`: 必ず必要
   - `<param>`: 引数のあるメソッドやコンストラクターの場合、必要
   - `<returns>`: 戻り値がある場合、必要
   - `<exception>`: 送出しうる公開例外がある場合、必要
   - `<typeparam>`: ジェネリック型パラメーターがある場合、必要
   - `<remarks>`: 任意。summaryに書ききれない補足仕様がある場合はタグを追加しても良い

6. **不必要な重複を避ける**
   メソッド名から自明な内容を繰り返さない。
   ただし制約・境界値・null可否は明記する。

7. **日本語/英語のコメントを併記する**
   - このプロジェクトでは `日本語/英語併記の例` のように、言語ごとに`xml:lang`属性を持つ`<para>`タグを作成し、各言語のコメント文章を併記する
   - すべての説明タグ（`<summary>`, `<param>`, `<returns>`, `<exception>`, `<remarks>`など）に対して同じ方針を適用する
   - enのみ、jaのみのコメントは不可。必ず両方を記載する
   - 言語の記載順はen→jaに統一する

### 日本語/英語併記の例

```
/// <summary>
///     <para xml:lang="en">Represents a UUID version 7.</para>
///     <para xml:lang="ja">UUID version 7を表す構造体。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">This struct is a thin wrapper around the <see cref="Guid"/> type. This struct can only hold version 7 <see cref="Guid"/> values.</para>
///     <para xml:lang="ja">この構造体の実態は <see cref="Guid"/> 型の薄いラッパーである。version 7の <see cref="Guid"/> のみを保持することができる。</para>
/// </remarks>
public readonly struct UuidV7 : IComparable, IComparable<UuidV7>, IEquatable<UuidV7>, IParsable<UuidV7>, ISpanFormattable, ISpanParsable<UuidV7>, IUtf8SpanFormattable, IUtf8SpanParsable<UuidV7>
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="UuidV7"/> struct.</para>
    ///     <para xml:lang="ja"><see cref="UuidV7"/>構造体の新しいインスタンスを初期化する。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A new instance of the <see cref="UuidV7"/> struct.</para>
    ///     <para xml:lang="ja">新しい UUID version 7 オブジェクト。</para>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UuidV7 NewUuidV7() => new(Guid.CreateVersion7());
}
```

---

## 日本語コメントを記載する場合のルール（日本語）

### 文体ルール
1. **常体（です・ます禁止）**
   - 良い: 「文字列を比較する。」
   - 悪い: 「文字列を比較します。」

2. **現在形・断定的に書く**
   - 良い: 「指定したインデックス位置から検索する。」
   - 悪い: 「指定したインデックス位置から検索します。」

3. **主語省略を許容しつつ対象を明確化**
   曖昧になる場合は「このメソッドは」「このプロパティは」を補う。

4. **日本語として自然な技術文**
   不自然な直訳を避ける（例:「値を取得または設定します」濫用を避け、意味に応じて書き分ける）。

### 表現ルール
1. `<summary>` は「何をするか」を先に書く
2. `<param>` は「何を表す引数か + 制約」を書く
3. `<returns>` は「何を返すか」を名詞止めまたは断定文で書く
4. `<exception>` は「どの条件で何例外か」を明示する
5. `null`、空文字、範囲外、未初期化などの境界条件を具体化する

### 用語ルール
- `null` / `true` / `false` / 例外型名（`ArgumentNullException` など）はコード表記を維持
- 型名・メンバー名は `<see cref="..."/>` を優先
- 「引数」「戻り値」「例外」「インスタンス」など一般的技術語に統一

---

## 英語コメントを記載する場合のルール

### 文体ルール
1. **三人称単数・現在形を基本とする**
   - 良い: `Gets the length of this string.`
   - 悪い: `Get the length...`（命令形）

2. **簡潔な技術英語を使用する**
   口語表現（`awesome`, `simply`, `just`）は禁止。

3. **断定的・仕様的に書く**
   「何をするか」「何を返すか」「どの条件で失敗するか」を明示する。

4. **一文を短く保つ**
   長文を避け、必要なら `<remarks>` に分離する。

### 表現ルール
1. `<summary>` は `Gets ...`, `Sets ...`, `Returns ...`, `Converts ...`, `Determines whether ...` など Microsoft Learn で一般的な動詞句で開始する
2. `<param>` は名詞句で対象を定義し、必要に応じて制約を書く
3. `<returns>` は返却値の意味を簡潔に記述する
4. `<exception>` は `Thrown when ...` 構文で条件を示す
5. `null`、範囲外、形式不正などの条件は曖昧語なしで具体的に書く

### 用語ルール
- .NET の標準用語（`value`, `index`, `count`, `comparisonType` など）を優先
- 型名・メンバー名は `<see cref="..."/>` を使用
- 句読点・冠詞（`a`, `an`, `the`）を適切に用い、文法誤りを避ける

---

## XMLタグ記述ルール（共通）

1. **`<summary>` は必須**
2. **引数ごとに `<param name="...">` を必須**
3. **戻り値がある場合 `<returns>` 必須**
4. **送出しうる公開例外は `<exception cref="...">` を記載**
5. **型パラメーターがある場合 `<typeparam>` 必須**
6. **相互参照は `<see cref="..."/>` / `<seealso cref="..."/>` を使用**
7. **コード例は必要時のみ `<example>` で記載**
8. **`<inheritdoc/>` を使う場合、差分仕様があれば `<remarks>` で補う**

---

## 記述テンプレート

### クラス / 構造体

```csharp
/// <summary>
///     <para xml:lang="en">Represents ～～.</para>
///     <para xml:lang="ja">～～を表す（クラス／構造体）。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Provides ～～.</para>
///     <para xml:lang="ja">～～を提供する。</para>
/// </remarks>
public sealed class SampleType
{
}
```

```csharp
/// <summary>
///     <para xml:lang="en">Represents a ～～ struct.</para>
///     <para xml:lang="ja">～～を表す構造体。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">This struct is ～～.</para>
///     <para xml:lang="ja">この構造体は～～である。</para>
/// </remarks>
public readonly struct SampleStruct
{
}
```

### コンストラクター

```csharp
/// <summary>
///     <para xml:lang="en">Initializes a new instance of the <see cref="SampleType"/> class.</para>
///     <para xml:lang="ja"><see cref="SampleType"/> クラスの新しいインスタンスを初期化する。</para>
/// </summary>
/// <param name="value">
///     <para xml:lang="en">The ～～ value.</para>
///     <para xml:lang="ja">～～を表す値。</para>
/// </param>
/// <exception cref="ArgumentNullException">
///     <para xml:lang="en">Thrown when <paramref name="value"/> is null.</para>
///     <para xml:lang="ja"><paramref name="value"/> が null の場合にスローされる。</para>
/// </exception>
public SampleType(string value)
{
}
```

### メソッド（戻り値あり）

```csharp
/// <summary>
///     <para xml:lang="en">Performs ～～.</para>
///     <para xml:lang="ja">～～を実行する。</para>
/// </summary>
/// <param name="input">
///     <para xml:lang="en">The input value to ～～.</para>
///     <para xml:lang="ja">～～する対象の入力値。</para>
/// </param>
/// <returns>
///     <para xml:lang="en">A ～～ that indicates ～～.</para>
///     <para xml:lang="ja">～～を示す～～を返す。</para>
/// </returns>
/// <exception cref="ArgumentException">
///     <para xml:lang="en">Thrown when <paramref name="input"/> is invalid.</para>
///     <para xml:lang="ja"><paramref name="input"/> が無効な場合にスローされる。</para>
/// </exception>
public static int DoWork(string input) => 0;
```

### メソッド（戻り値なし）

```csharp
/// <summary>
///     <para xml:lang="en">Updates ～～.</para>
///     <para xml:lang="ja">～～を更新する。</para>
/// </summary>
/// <param name="value">
///     <para xml:lang="en">The value to apply.</para>
///     <para xml:lang="ja">適用する値。</para>
/// </param>
/// <exception cref="InvalidOperationException">
///     <para xml:lang="en">Thrown when the current state does not allow this operation.</para>
///     <para xml:lang="ja">現在の状態でこの操作が許可されない場合にスローされる。</para>
/// </exception>
public void Update(string value)
{
}
```

### プロパティ

```csharp
/// <summary>
///     <para xml:lang="en">Gets the ～～.</para>
///     <para xml:lang="ja">～～を取得する。</para>
/// </summary>
/// <value>
///     <para xml:lang="en">The ～～ value.</para>
///     <para xml:lang="ja">～～を表す値。</para>
/// </value>
public string Name { get; }

---

## 良い例 / 悪い例

### 日本語 `<summary>` の例
- 良い: `指定した部分文字列がこのインスタンス内で最初に出現する位置を検索する。`
- 悪い: `部分文字列を探します。`（口語・情報不足）

### 英語 `<summary>` の例
- 良い: `Returns a value indicating whether a specified substring occurs within this string.`
- 悪い: `Check substring.`（主語欠落・情報不足）

---

## 禁止事項

1. 実装詳細（内部アルゴリズム、私有フィールド名）を API 契約として書く
2. テスト観点のメモを XML コメントに混在させる
3. 未確認の例外を断定的に列挙する
4. ローカルルールで .NET 標準用語を崩す
5. 日本語と英語を1つの `<summary>` に混在させる

---

## エージェントへの適用指示

- 生成対象が `src/` 以下の C# の public/protected API である場合、本ガイドを**必ず適用**すること。
- 例外条件・null可否・範囲制約がコード上で判別できる場合、`<param>` / `<exception>` に反映すること。
- 曖昧な場合は過剰に推測せず、`<remarks>` を最小限に留めること。