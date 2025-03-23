namespace Olve.Engine3D.AssetPipeline.Shaders;

public class Shader
{
    public required string Name { get; set; }
    public required ShaderType Type { get; set; }
    public required string SourcePath { get; set; }
    public required string SourceCode { get; set; }
    public required IReadOnlyList<Uniform> Uniforms { get; set; }
}