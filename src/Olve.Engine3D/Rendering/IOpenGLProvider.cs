using Jab;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Textures;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

[ServiceProviderModule]
[Singleton(typeof(OpenGLBufferManager))]
[Singleton(typeof(OpenGLInstancedBufferManager))]
[Singleton(typeof(OpenGLModelRenderingManager))]
[Singleton(typeof(OpenGLQuadRenderingManager))]
[Singleton(typeof(OpenGLShaderManager))]
[Singleton(typeof(OpenGLTextureManager))]
[Singleton(typeof(Provider<GL>))]
[Singleton(typeof(RenderingManager2D))]
[Singleton(typeof(RenderingManager3D))]
[Singleton(typeof(RenderingServiceHelper))]
[Singleton(typeof(ShaderEntityManager))]
[Singleton(typeof(TextureEntityManager))]
[Singleton(typeof(TextureManager))]
[Singleton(typeof(TextureSlotManager))]
[Singleton(typeof(AssetLoader))]
[Singleton(typeof(TextureLoadingManager))]
public interface IOpenGLProvider;