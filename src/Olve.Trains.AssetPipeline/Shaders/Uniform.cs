namespace Olve.Trains.AssetPipeline.Shaders;

public class Uniform
{
    public required string Name { get; set; }
    public required UniformType Type { get; set; }
    public int? LayoutLocation { get; set; }

    /// <summary>
    /// For Sampler2D uniforms: the pixel data type (e.g. "RGBA", "float").
    /// Parsed from // @pixelType(...) annotations in shader source.
    /// </summary>
    public string? PixelType { get; set; }

    public List<ImplementsAnnotation> Implements { get; set; } = [];

    public override string ToString()
    {
        return $"{Type} {Name}";
    }
}