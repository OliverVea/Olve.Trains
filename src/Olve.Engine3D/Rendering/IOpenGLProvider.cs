using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
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
        services.TryAddScoped<GeometryManager>();
        services.TryAddScoped<OpenGLShaderManager>();
        services.TryAddScoped<OpenGLTextureManager>();
        services.TryAddScoped<RenderingGroupManager>();
        services.TryAddScoped<RenderingInstanceManager>();
        services.TryAddScoped<RenderingManager>();
        services.TryAddScoped<RenderingServiceHelper>();
        services.TryAddScoped<ShaderEntityManager>();
        services.TryAddScoped<TextureEntityManager>();
        services.TryAddScoped<TextureManager>();
        services.TryAddScoped<TextureSlotManager>();
        services.TryAddScoped<AssetLoader>();
        services.TryAddScoped<TextureLoadingManager>();
        services.TryAddScoped<MeshManager>();
        services.TryAddScoped<MeshLoadingManager>();
        services.TryAddScoped<CollisionSystem>();
        services.TryAddScoped<FramebufferManager>();
        services.TryAddScoped<RenderPassManager>();
        services.TryAddScoped<ScreenPassManager>();
        return services;
    }
}
