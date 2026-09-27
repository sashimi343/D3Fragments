# ライブラリメンテナー向け情報

## パッケージ作成

```
dotnet pack D3Fragments.slnx -c Release -o artifacts
```

- ビルド時に `scripts/SeparateDocs.ps1` が XML ドキュメントを en(既定) / ja に分離する（要 PowerShell 7 `pwsh`）
- ローカル版を区別する場合は `--version-suffix local.1` などを付与