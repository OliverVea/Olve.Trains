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
        // NOTE: these singletons are main-thread-only — the dictionaries are
        // plain (not thread-safe). Background pre-warming must add its own
        // synchronization before populating them off-thread.
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
