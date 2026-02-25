using Olve.Trains.Scenes.GameLogic.ShaderExtensions;

// ReSharper disable once CheckNamespace
namespace Olve.Generated.Shaders;

public partial class Shaders
{
    public partial class Default : IDaylightShader, ICameraPositionShader, ICameraDirectionShader;
    public partial class Terrain : IDaylightShader, ICameraPositionShader, ICameraDirectionShader, IWorldMousePositionShader;
    public partial class LineStrip : ICameraPositionShader;
    public partial class Track : IDaylightShader, ICameraPositionShader;
}

