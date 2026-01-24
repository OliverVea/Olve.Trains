using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Textures;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class TextureEntityManager(OpenGLTextureManager openGLTextureManager) : RenderingEntityManagerBase<Texture, TextureEntityManager.Registration>
{
    public readonly record struct Registration(Texture2D Texture);

    protected override Result<Registration> RegisterInOpenGL(Texture texture)
    {
        if (openGLTextureManager.Register(texture.Data).TryPickProblems(out var problems, out var openGlTexture))
        {
            return problems.Prepend("Failed registering texture in OpenGL");
        }

        return new Registration(openGlTexture);
    }

    protected override Result DeregisterFromOpenGL(Registration registration)
    {
        if (openGLTextureManager.Unregister(registration.Texture).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering texture in OpenGL");
        }

        return Result.Success();
    }

    /// <summary>
    /// Adopts an externally-created texture (e.g., from HeightmapEntityManager).
    /// The caller is responsible for managing the texture's lifecycle.
    /// </summary>
    public RenderingId<Texture> AdoptExternalTexture(Texture2D texture)
    {
        var renderingId = RenderingId<Texture>.New();
        ModelRegistrations.Add(renderingId, new Registration(texture));
        return renderingId;
    }
}