using Olve.Engine3D.Rendering.Textures;

namespace Olve.Trains.Scenes.GameLogic.ShaderExtensions;

public interface IShadowShader
{
    Matrix4X4<float>? LightSpaceMatrix { get; set; }
    TextureId<Rg32f>? ShadowMap { get; set; }
}
