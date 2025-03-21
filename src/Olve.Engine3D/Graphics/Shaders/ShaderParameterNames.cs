namespace Olve.Engine3D.Graphics.Shaders;

public class ShaderParameterNames
{
    public required string PositionAttributeName { get; init; }

    public string? WorldMatrixUniformName { get; init; }
    public string? ViewMatrixUniformName { get; init; }
    public string? ProjectionMatrixUniformName { get; init; }
}