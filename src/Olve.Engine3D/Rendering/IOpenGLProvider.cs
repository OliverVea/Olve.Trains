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
        // CPU-side asset caches are singletons so they survive scene-scope
        // disposal — assets stay loaded across scene transitions instead of
        // being re-read from disk each time. The GPU-side managers above
        // (Geometry/OpenGLTexture/TextureEntity/TextureSlot) stay scoped: they
        // hold per-scope GL handles and re-upload from these caches each scene.
        // These singletons guard their caches with locks, so the loading scene
        // can pre-warm them from a background thread (see AssetPrewarmService).
        services.TryAddSingleton<TextureManager>();
        services.TryAddScoped<TextureSlotManager>();
        services.TryAddSingleton<AssetLoader>();
        services.TryAddSingleton<TextureLoadingManager>();
        services.TryAddSingleton<MeshManager>();
        services.TryAddSingleton<MeshLoadingManager>();
        services.TryAddScoped<CollisionSystem>();
        services.TryAddScoped<FramebufferManager>();
        services.TryAddScoped<RenderPassManager>();
        services.TryAddScoped<ScreenPassManager>();
        return services;
    }
}
