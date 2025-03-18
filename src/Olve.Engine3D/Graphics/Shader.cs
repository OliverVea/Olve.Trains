namespace Olve.Engine3D.Graphics;

public class Shader(ShaderProgram shaderProgram)
{
    public ShaderProgram ShaderProgram { get; } = shaderProgram;

    public string? ViewMatrixUniformName { get; set; }
    public string? ProjectionMatrixUniformName { get; set; }
}