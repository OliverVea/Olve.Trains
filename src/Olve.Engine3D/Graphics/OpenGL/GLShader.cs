namespace Olve.Engine3D.Graphics.OpenGL;

public class GLShader(ShaderProgram shaderProgram)
{
    public ShaderProgram ShaderProgram { get; } = shaderProgram;

    public required string PositionAttributeName { get; set; }
    public required string? WorldMatrixUniformName { get; set; }
    public required string? ViewMatrixUniformName { get; set; }
    public required string? ProjectionMatrixUniformName { get; set; }
}