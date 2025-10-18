using Olve.Trains.Scenes.Game.ShaderExtensions;

// ReSharper disable once CheckNamespace
namespace Olve.Generated.Shaders;

public partial class Shaders
{
    public partial class Default : IDaylightShader, ICameraPositionShader, ICameraDirectionShader;
    public partial class Terrain : IDaylightShader, ICameraPositionShader, ICameraDirectionShader;

    public partial class TerrainWireframe : ICameraPositionShader, IWorldMousePositionShader;
    public partial class LineStrip : ICameraPositionShader;
}

