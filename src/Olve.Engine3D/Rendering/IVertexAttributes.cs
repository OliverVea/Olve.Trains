namespace Olve.Engine3D.Rendering;

public interface IWithPosition3D<TSelf> where TSelf : IWithPosition3D<TSelf>
{
    Vector3D<float> Position { get; }
    static abstract TSelf WithPosition(TSelf self, Vector3D<float> position);
}

public interface IWithNormal3D<TSelf> where TSelf : IWithNormal3D<TSelf>
{
    Vector3D<float> Normal { get; }
    static abstract TSelf WithNormal(TSelf self, Vector3D<float> normal);
}

public interface IWithTexCoords2D<TSelf> where TSelf : IWithTexCoords2D<TSelf>
{
    Vector2D<float> TexCoords { get; }
    static abstract TSelf WithTexCoords(TSelf self, Vector2D<float> texCoords);
}

public interface IWithColor3D<TSelf> where TSelf : IWithColor3D<TSelf>
{
    Vector3D<float> Color { get; }
    static abstract TSelf WithColor(TSelf self, Vector3D<float> color);
}
