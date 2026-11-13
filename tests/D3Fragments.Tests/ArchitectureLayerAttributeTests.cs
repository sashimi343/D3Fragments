namespace D3Fragments.Tests;

public class ArchitectureLayerAttributeTests
{
    [Fact]
    public void Constructor_指定したレイヤーを保持する()
    {
        var layer = ArchitectureLayerType.Domain | ArchitectureLayerType.Application;

        Assert.Equal(layer, new ArchitectureLayerAttribute(layer).ArchitectureLayer);
    }
}
