using Olve.Engine3D;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public static class DirectionExtensions
{
    public static Vector3D<float> ToVector3D(this CardinalDirection cardinalDirection)
    {
        return cardinalDirection switch
        {
            CardinalDirection.North => new Vector3D<float>(0, 0, 1),
            CardinalDirection.South => new Vector3D<float>(0, 0, -1),
            CardinalDirection.East => new Vector3D<float>(1, 0, 0),
            CardinalDirection.West => new Vector3D<float>(-1, 0, 0),
            CardinalDirection.Up => new Vector3D<float>(0, 1, 0),
            CardinalDirection.Down => new Vector3D<float>(0, -1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(cardinalDirection), cardinalDirection, null)
        };
    }
    
    public static float ToYRotation(this CardinalDirection cardinalDirection)
    {
        return cardinalDirection switch
        {
            CardinalDirection.North => 0,
            CardinalDirection.South => MathF.PI,
            CardinalDirection.East => MathF.PI / 2,
            CardinalDirection.West => -MathF.PI / 2,
            _ => throw new ArgumentOutOfRangeException(nameof(cardinalDirection), cardinalDirection, null)
        };
    }
}