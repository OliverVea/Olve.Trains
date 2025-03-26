using Olve.CodeGen;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Primitives;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes;

public enum Direction
{
    None,
    North,
    South,
    East,
    West,
    Up,
    Down
}

public static class GameSceneEntities
{
    private static readonly Vector3D<float> AmbientLightColor = new(180 / 255f, 167 / 255f, 214 / 255f);
    private static readonly float AmbientLightIntensity = 0.2f;
    private static readonly Vector3D<float> DirectionalLightColor = new(1.0f, 1.0f, 0.95f);
    private static readonly Vector3D<float> DirectionalLightDir = Vector3D.Normalize(new Vector3D<float>(0.25f, -1, 0.25f));
    private static readonly float DirectionalIntensity = 1.0f;

    public static readonly Shaders.Default DefaultShader = new(
        AmbientLightColor,
        AmbientLightIntensity,
        DirectionalLightColor,
        DirectionalLightDir,
        DirectionalIntensity,
        textureSampler: new Texture2D(0, 0, 0),
        world: Matrix4X4<float>.Identity,
        view: Matrix4X4<float>.Identity,
        projection: Matrix4X4<float>.Identity
    );

    public static readonly Shaders.Terrain TerrainShader = new(
        AmbientLightColor,
        AmbientLightIntensity,
        DirectionalLightColor,
        DirectionalLightDir,
        DirectionalIntensity,
        heightMap: new Texture2D(0, 0, 0),
        texelSize: new Vector2D<float>(0, 0),
        world: Matrix4X4<float>.Identity,
        view: Matrix4X4<float>.Identity,
        projection: Matrix4X4<float>.Identity,
        cameraDirection: Vector3D<float>.Zero);


    public static readonly TextureData DefaultTexture = new()
    {
        Width = 8,
        Height = 1,
        Pixels = [
            new Vector4D<byte>(0, 0, 0, 255),
            new Vector4D<byte>(255, 0, 0, 255),
            new Vector4D<byte>(0, 255, 0, 255),
            new Vector4D<byte>(0, 0, 255, 255),
            new Vector4D<byte>(255, 255, 0, 255),
            new Vector4D<byte>(255, 0, 255, 255),
            new Vector4D<byte>(0, 255, 255, 255),
            new Vector4D<byte>(200, 200, 185, 255),
        ]
    };

    public static readonly Vector3D<float>[] CubeVertices =
    [
        // Top
        new (-0.5f, 0.5f, -0.5f),
        new (0.5f, 0.5f, -0.5f),
        new (0.5f, 0.5f, 0.5f),
        new (-0.5f, 0.5f, 0.5f),

        // Left
        new (-0.5f, -0.5f, -0.5f),
        new (-0.5f, -0.5f, 0.5f),
        new (-0.5f, 0.5f, 0.5f),
        new (-0.5f, 0.5f, -0.5f),

        // Right
        new (0.5f, -0.5f, -0.5f),
        new (0.5f, -0.5f, 0.5f),
        new (0.5f, 0.5f, 0.5f),
        new (0.5f, 0.5f, -0.5f),

        // Front
        new (-0.5f, -0.5f, 0.5f),
        new (0.5f, -0.5f, 0.5f),
        new (0.5f, 0.5f, 0.5f),
        new (-0.5f, 0.5f, 0.5f),

        // Back
        new (-0.5f, -0.5f, -0.5f),
        new (0.5f, -0.5f, -0.5f),
        new (0.5f, 0.5f, -0.5f),
        new (-0.5f, 0.5f, -0.5f),

        // Bottom
        new (-0.5f, -0.5f, -0.5f),
        new (0.5f, -0.5f, -0.5f),
        new (0.5f, -0.5f, 0.5f),
        new (-0.5f, -0.5f, 0.5f),
    ];

    public static readonly Direction[] CubeDirections =
    [
        // Top
        Direction.Up,
        Direction.Up,
        Direction.Up,
        Direction.Up,

        // Left
        Direction.West,
        Direction.West,
        Direction.West,
        Direction.West,

        // Right
        Direction.East,
        Direction.East,
        Direction.East,
        Direction.East,

        // Front
        Direction.North,
        Direction.North,
        Direction.North,
        Direction.North,

        // Back
        Direction.South,
        Direction.South,
        Direction.South,
        Direction.South,

        // Bottom
        Direction.Down,
        Direction.Down,
        Direction.Down,
        Direction.Down,
    ];

    public static readonly TriangleIndex[] CubeIndices =
    [
        // Top
        new(0, 1, 2),
        new (0, 2, 3),

        // Left
        new TriangleIndex(0, 1, 2) + 4u,
        new TriangleIndex(0, 2, 3) + 4u,

        // Right
        new TriangleIndex(0, 1, 2) + 8u,
        new TriangleIndex(0, 2, 3) + 8u,

        // Front
        new TriangleIndex(0, 1, 2) + 12u,
        new TriangleIndex(0, 2, 3) + 12u,

        // Back
        new TriangleIndex(0, 1, 2) + 16u,
        new TriangleIndex(0, 2, 3) + 16u,

        // Bottom
        new TriangleIndex(0, 1, 2) + 20u,
        new TriangleIndex(0, 2, 3) + 20u,
    ];

    public static readonly MeshData Cube = new()
    {
        Positions = CubeVertices,
        Normals = CubeDirections.Select(MapToNormal).ToArray(),
        Indices = CubeIndices,
        TextureCoordinates = CubeDirections.Select(MapToTexture).ToArray()
    };

    public static Vector3D<float> MapToNormal(Direction direction) => direction switch
    {
        Direction.None => new Vector3D<float>(0, 0, 0),
        Direction.North => new Vector3D<float>(0, 0, 1),
        Direction.South => new Vector3D<float>(0, 0, -1),
        Direction.East => new Vector3D<float>(1, 0, 0),
        Direction.West => new Vector3D<float>(-1, 0, 0),
        Direction.Up => new Vector3D<float>(0, 1, 0),
        Direction.Down => new Vector3D<float>(0, -1, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
    };

    public static Vector2D<float> MapToTexture(Direction direction)
    {
        var x = direction switch
        {
            Direction.None => 0,
            Direction.North => 1f / 7f,
            Direction.South => 2f / 7f,
            Direction.East => 3f / 7f,
            Direction.West => 4f / 7f,
            Direction.Up => 0.99f,
            Direction.Down => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        return new Vector2D<float>(x, 0);
    }
}