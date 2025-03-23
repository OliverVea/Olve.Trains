namespace Olve.Engine3D.AssetPipeline.Shaders;

public class ShaderProgram
{
    public required string Name { get; set; }
    public Shader? FragmentShader { get; set; }
    public Shader? VertexShader { get; set; }
    
    public IReadOnlyList<Uniform> Uniforms { get; set; }
}