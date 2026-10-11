namespace D3Fragments.Analyzers.Models;

// D3Fragments.ArchitectureLayerType と同じ値を保つこと（アナライザはコアパッケージを参照できないため複製している）
[Flags]
internal enum ArchitectureLayerType
{
    None = 0,
    Presentation = 1 << 0,
    Application = 1 << 1,
    Domain = 1 << 2,
    Infrastructure = 1 << 3,
}
