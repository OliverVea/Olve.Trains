using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        services.TryAddSingleton<Provider<GL>>();
        services.TryAddScoped<OpenGLBufferManager>();
        services.TryAddScoped<OpenGLInstancedBufferManager>();
        services.TryAddScoped<OpenGLModelRenderingManager>();
        services.TryAddScoped<OpenGLQuadRenderingManager>();
        services.TryAddScoped<OpenGLShaderManager>();
        services.TryAddScoped<OpenGLTextureManager>();
        services.TryAddScoped<RenderingManager2D>();
        services.TryAddScoped<RenderingManager3D>();
        services.TryAddScoped<RenderingServiceHelper>();
        services.TryAddScoped<ShaderEntityManager>();
        services.TryAddScoped<TextureEntityManager>();
        services.TryAddScoped<TextureManager>();
        services.TryAddScoped<TextureSlotManager>();
        services.TryAddScoped<AssetLoader>();
        services.TryAddScoped<TextureLoadingManager>();
        return services;
    }
}
