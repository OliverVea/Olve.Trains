using Olve.CodeGen;
using Olve.Engine3D.Light;
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
    public static readonly Shaders.Default DefaultShader = new(
        default,
        0,
        default,
        default,
        0,
        default,
        default,
        0,
        textureSampler: new Texture2D(0, 0, 0),
        world: Matrix4X4<float>.Identity,
        view: Matrix4X4<float>.Identity,
        projection: Matrix4X4<float>.Identity,
        cameraDirection: Vector3D<float>.Zero
    );

    public static readonly Shaders.Terrain TerrainShader = new(
        default,
        default,
        default,
        default,
        default,
        default,
        default,
        default,
        heightMap: new Texture2D(0, 0, 0),
        texelSize: new Vector2D<float>(0, 0),
        world: Matrix4X4<float>.Identity,
        view: Matrix4X4<float>.Identity,
        projection: Matrix4X4<float>.Identity,
        cameraDirection: Vector3D<float>.Zero);

    public static readonly Shaders.TerrainWireframe TerrainWireframe = new(
        5f,
        default,
        default,
        Matrix4X4<float>.Identity,
        Matrix4X4<float>.Identity,
        Matrix4X4<float>.Identity,
        default,
        false,
        false);


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

    public static class Colors
    {
        public static readonly Vector3D<float> Black = new(0, 0, 0);
        public static readonly Vector3D<float> White = new Vector3D<float>(255,255,255) / 255f;

        public static readonly Vector3D<float> Shadow = new Vector3D<float>(91,91,111) / 255f;
        public static readonly Vector3D<float> SunRed = new Vector3D<float>(231,76,60) / 255f;
        public static readonly Vector3D<float> SunOrange = new Vector3D<float>(243,156,18) / 255f;
        public static readonly Vector3D<float> SunPaleOrange = new Vector3D<float>(245,176,65) / 255f;
        public static readonly Vector3D<float> SunYellow = new Vector3D<float>(247,220,111) / 255f;
        public static readonly Vector3D<float> SunWhite = (White + SunYellow) * 0.5f;

        public static readonly Vector3D<float> MoonBlue = new Vector3D<float>(110, 170, 255) / 255f;
        public static readonly Vector3D<float> MoonWhite = new Vector3D<float>(200, 200, 255) / 255f;
    }

    private static readonly DayTimeKeyFrame<Vector3D<float>>[] SunColorKeyFrames =
    [
        (new DayTime(0), Colors.Black),
        (new DayTime(5, 30), Colors.Black),
        (new DayTime(6), Colors.SunRed),
        (new DayTime(6, 30), Colors.SunOrange),
        (new DayTime(7), Colors.SunPaleOrange),
        (new DayTime(7, 30), Colors.SunYellow),
        (new DayTime(9), Colors.SunWhite),
        (new DayTime(17), Colors.SunWhite),
        (new DayTime(18, 30), Colors.SunYellow),
        (new DayTime(19), Colors.SunPaleOrange),
        (new DayTime(19, 30), Colors.SunOrange),
        (new DayTime(20), Colors.SunRed),
        (new DayTime(20, 30), Colors.Black),
        (new DayTime(24), Colors.Black),
    ];

    private static readonly DayTimeKeyFrame<float>[] SunAngleKeyFrames =
    [
        (new DayTime(0), 0),
        (new DayTime(6, 30), 0),
        (new DayTime(20, 30), 180),
        (new DayTime(24), 180),
    ];

    private static readonly DayTimeKeyFrame<float>[] SunIntensityKeyFrames =
    [
        (new DayTime(0), 0),
        (new DayTime(5, 30), 0),
        (new DayTime(9), 1f),
        (new DayTime(17), 1f),
        (new DayTime(20, 30), 0),
        (new DayTime(24), 0),
    ];

    private static readonly DayTimeKeyFrame<float>[] SunAmbientIntensityKeyFrames =
    [
        (new DayTime(0), 0),
        (new DayTime(6), 0),
        (new DayTime(9), 0.45f),
        (new DayTime((9 + 18) / 2f), 0.6f),
        (new DayTime(18), 0.45f),
        (new DayTime(21), 0),
        (new DayTime(24), 0),
    ];


    public static readonly DaylightData SunData = new()
    {
        Angle = new Curve1(InterpolationType.Linear, SunAngleKeyFrames) { Min = 0, Max = 180},
        Color = new Curve3(InterpolationType.CatmullRom, SunColorKeyFrames) { Min = Colors.Black, Max = Colors.White},
        Intensity = new Curve1(InterpolationType.CatmullRom, SunIntensityKeyFrames) { Min = 0, Max = 1},
        AmbientColor = new Curve3(InterpolationType.Linear, (new DayTime(0), Colors.Shadow), (new DayTime(24), Colors.Shadow)),
        AmbientIntensity = new Curve1(InterpolationType.CatmullRom, SunAmbientIntensityKeyFrames) { Min = 0, Max = 1},
    };

    private static readonly DayTimeKeyFrame<Vector3D<float>>[] MoonColorKeyFrames =
    [
        (new DayTime(0), Colors.MoonWhite),
        (new DayTime(24), Colors.MoonWhite),
    ];

    private static readonly DayTimeKeyFrame<float>[] MoonAngleKeyFrames =
    [
        (new DayTime(0), 90),
        (new DayTime(6), 180),
        (new DayTime(18), 0),
        (new DayTime(24), 90),
    ];

    private static readonly DayTimeKeyFrame<float>[] MoonIntensityKeyFrames =
    [
        (new DayTime(0), 0.45f),
        (new DayTime(5), 0.45f),
        (new DayTime(7), 0),
        (new DayTime(17), 0),
        (new DayTime(21), 0.45f),
        (new DayTime(24), 0.45f),
    ];

    private static readonly DayTimeKeyFrame<float>[] MoonAmbientIntensityKeyFrames =
    [
        (new DayTime(0), 0.25f),
        (new DayTime(24), 0.25f),
    ];

    public static readonly DaylightData MoonData = new()
    {
        Angle = new Curve1(InterpolationType.Linear, MoonAngleKeyFrames) { Min = 0, Max = 180 },
        Color = new Curve3(InterpolationType.CatmullRom, MoonColorKeyFrames) { Min = Colors.Black, Max = Colors.MoonWhite },
        Intensity = new Curve1(InterpolationType.CatmullRom, MoonIntensityKeyFrames) { Min = 0, Max = 1f },
        AmbientColor = new Curve3(InterpolationType.Linear, (new DayTime(0), Colors.MoonBlue), (new DayTime(24), Colors.MoonBlue)),
        AmbientIntensity = new Curve1(InterpolationType.CatmullRom, MoonAmbientIntensityKeyFrames) { Min = 0, Max = 1f },
    };
}