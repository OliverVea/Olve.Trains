using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class TextureEntityManager(OpenGLTextureManager openGLTextureManager) : RenderingEntityManagerBase<TextureData, TextureEntityManager.Registration>
{
    public readonly record struct Registration(OpenGL.Handles.Texture2D Texture);

    protected override Result<Registration> RegisterInOpenGL(TextureData entity)
    {
        if (openGLTextureManager.Register(entity).TryPickProblems(out var problems, out var texture))
        {
            return problems.Prepend("Failed registering texture in OpenGL");
        }

        return new Registration(texture);
    }

    protected override Result DeregisterFromOpenGL(Registration registration)
    {
        if (openGLTextureManager.Unregister(registration.Texture).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering texture in OpenGL");
        }

        return Result.Success();
    }
}