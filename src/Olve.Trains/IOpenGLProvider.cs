using Jab;
using Olve.Engine3D;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL;
using Silk.NET.OpenGL;

namespace Olve.Trains;

[ServiceProviderModule]
[Singleton(typeof(MeshEntityManager))]
[Singleton(typeof(ShaderEntityManager))]
[Singleton(typeof(TextureEntityManager))]
[Singleton(typeof(HeightmapEntityManager))]
[Singleton(typeof(LineStripEntityManager))]
[Singleton(typeof(RenderingManager3D))]
[Singleton(typeof(OpenGLMeshManager))]
[Singleton(typeof(OpenGLShaderManager))]
[Singleton(typeof(OpenGLTextureManager))]
[Singleton(typeof(OpenGLHeightmapManager))]
[Singleton(typeof(OpenGLModelRenderingManager))]
[Singleton(typeof(OpenGLLineStripManager))]
[Singleton(typeof(Provider<GL>))]
public interface IOpenGLProvider;