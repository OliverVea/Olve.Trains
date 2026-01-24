using Jab;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Textures;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

[ServiceProviderModule]
[Singleton(typeof(HeightmapEntityManager))]
[Singleton(typeof(LineStripEntityManager))]
[Singleton(typeof(MeshEntityManager))]
[Singleton(typeof(OpenGLHeightmapManager))]
[Singleton(typeof(OpenGLLineStripManager))]
[Singleton(typeof(OpenGLMeshManager))]
[Singleton(typeof(OpenGLModelRenderingManager))]
[Singleton(typeof(OpenGLQuadRenderingManager))]
[Singleton(typeof(OpenGLRectangleManager))]
[Singleton(typeof(OpenGLGlyphManager))]
[Singleton(typeof(OpenGLShaderManager))]
[Singleton(typeof(OpenGLTextureManager))]
[Singleton(typeof(Provider<GL>))]
[Singleton(typeof(RenderingManager2D))]
[Singleton(typeof(RenderingManager3D))]
[Singleton(typeof(RenderingServiceHelper))]
[Singleton(typeof(ShaderEntityManager))]
[Singleton(typeof(TextureEntityManager))]
[Singleton(typeof(TextureManager))]
[Singleton(typeof(TextureRenderingManager))]
[Singleton(typeof(TextureSlotManager))]
[Singleton(typeof(AssetLoader))]
[Singleton(typeof(TextureLoadingService))]
public interface IOpenGLProvider;