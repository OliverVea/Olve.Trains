namespace Olve.Engine3D.AssetPipeline.Shaders;

public class Uniform
{
    public required string Name { get; set; }
    public required UniformType Type { get; set; }
    public int? LayoutLocation { get; set; }

    public override string ToString()
    {
        return $"{Type} {Name}";
    }
}