namespace Olve.Engine3D.Graphics;

public class RenderingParameters(IReadOnlyList<AnyRenderingParameter> renderingParameters)
{
    public static string? DefaultWorldMatrixName { get; } = "world";

    public string? WorldMatrixName { get; init; } = DefaultWorldMatrixName;
    public IReadOnlyList<AnyRenderingParameter> Parameters { get; } = renderingParameters;
}