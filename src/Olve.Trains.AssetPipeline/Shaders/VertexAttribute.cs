namespace Olve.Trains.AssetPipeline.Shaders;

public class VertexAttribute
{
    public required int Location { get; set; }
    public required string Name { get; set; }
    public required UniformType Type { get; set; }
    public bool IsInstanced { get; set; }
}
