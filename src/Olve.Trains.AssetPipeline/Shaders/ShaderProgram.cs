namespace Olve.Trains.AssetPipeline.Shaders;

public class ShaderProgram
{
    public required string Name { get; set; }
    public required string Destination { get; set; }
    public required Shader FragmentShader { get; set; }
    public required Shader VertexShader { get; set; }
    public Shader? GeometryShader { get; set; }
    
    public required IReadOnlyList<Uniform> Uniforms { get; set; }
}