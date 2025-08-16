// ReSharper disable once CheckNamespace
namespace Olve.CodeGen;

public partial class Shaders
{
    public partial class Default : IDaylightShader, ICameraPositionShader, ICameraDirectionShader;
    public partial class Terrain : IDaylightShader, ICameraPositionShader, ICameraDirectionShader;
    public partial class TerrainWireframe : ICameraPositionShader;
    public partial class LineStrip : ICameraPositionShader;
}

