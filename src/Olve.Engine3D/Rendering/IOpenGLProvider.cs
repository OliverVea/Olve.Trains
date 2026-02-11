using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Utilities;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public static class OpenGLServiceRegistration
{
    public static IServiceCollection AddOpenGLServices(this IServiceCollection services)
    {
        services.AddSingleton<OpenGLBufferManager>();
        services.AddSingleton<OpenGLInstancedBufferManager>();
        services.AddSingleton<OpenGLModelRenderingManager>();
        services.AddSingleton<OpenGLQuadRenderingManager>();
        services.AddSingleton<OpenGLShaderManager>();
        services.AddSingleton<OpenGLTextureManager>();
        services.AddSingleton<Provider<GL>>();
        services.AddSingleton<RenderingManager2D>();
        services.AddSingleton<RenderingManager3D>();
        services.AddSingleton<RenderingServiceHelper>();
        services.AddSingleton<ShaderEntityManager>();
        services.AddSingleton<TextureEntityManager>();
        services.AddSingleton<TextureManager>();
        services.AddSingleton<TextureSlotManager>();
        services.AddSingleton<AssetLoader>();
        services.AddSingleton<TextureLoadingManager>();
        return services;
    }
}
