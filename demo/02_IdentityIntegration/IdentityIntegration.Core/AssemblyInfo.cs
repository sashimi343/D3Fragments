using D3Fragments;

// ドメインロジックはないが、D3Fragments のアーキテクチャに沿ってアプリケーション層とドメイン層を兼ねる
[assembly: ArchitectureLayer(ArchitectureLayerType.Application | ArchitectureLayerType.Domain)]
