using Olve.CodeGen;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Scenes;
using Olve.Logging;

namespace Olve.Trains.Scenes.UI.GUI;

public class InfoBarService(ILoggingManager loggingManager,
    RenderingManager2D renderingManager2D,
    ShaderEntityManager shaderEntityManager) : SceneService(loggingManager)
{
    private static readonly Shaders.Rectangle RectangleShader = new()
    {
        BlendState = RenderState.Opaque,
        UResolution = new Vector2D<float>(1920, 1080)
    };
    
    protected override Result OnLoad()
    {
        if (shaderEntityManager.Register(RectangleShader.ShaderData)
            .TryPickProblems(out var problems, out var shaderRenderingId))
        {
            return problems;
        }

        RectangleShader.RenderingId = shaderRenderingId;

        var registrationResult = renderingManager2D.RegisterRectangle(shaderRenderingId, new RectangleData()
        {
            ColorRgba = new Vector4D<float>(1.0f, 0, 0, 1.0f),
            SizePx = new Vector2D<float>(400, 50),
            PositionPx = new Vector2D<float>(0, 0)
        });
        
        if (registrationResult.TryPickProblems(out problems, out var boxInstanceId))
        {
            return problems;
        }

        return Result.Success();
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        return renderingManager2D.Render(RectangleShader);
    }
}