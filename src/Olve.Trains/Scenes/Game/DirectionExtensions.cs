using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public static class DirectionExtensions
{
    public static Vector3D<float> ToVector3D(this Direction direction)
    {
        return direction switch
        {
            Direction.North => new Vector3D<float>(0, 0, 1),
            Direction.South => new Vector3D<float>(0, 0, -1),
            Direction.East => new Vector3D<float>(1, 0, 0),
            Direction.West => new Vector3D<float>(-1, 0, 0),
            Direction.Up => new Vector3D<float>(0, 1, 0),
            Direction.Down => new Vector3D<float>(0, -1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
    
    public static float ToYRotation(this Direction direction)
    {
        return direction switch
        {
            Direction.North => 0,
            Direction.South => MathF.PI,
            Direction.East => MathF.PI / 2,
            Direction.West => -MathF.PI / 2,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}