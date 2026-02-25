namespace Olve.Engine3D;

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
    
    public static CardinalDirection ToCardinalDirection(this Vector3D<float> direction)
    {
        var flat = new Vector3D<float>(direction.X, 0, direction.Z);
        if (flat.Length < 1e-6f) return CardinalDirection.None;
        flat = Vector3D.Normalize(flat);

        ReadOnlySpan<(CardinalDirection Direction, Vector3D<float> Vector)> candidates =
        [
            (CardinalDirection.North, new Vector3D<float>(0, 0, 1)),
            (CardinalDirection.South, new Vector3D<float>(0, 0, -1)),
            (CardinalDirection.East, new Vector3D<float>(1, 0, 0)),
            (CardinalDirection.West, new Vector3D<float>(-1, 0, 0)),
        ];

        var bestDirection = CardinalDirection.None;
        var bestDot = float.MinValue;
        foreach (var (dir, vec) in candidates)
        {
            var dot = Vector3D.Dot(flat, vec);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestDirection = dir;
            }
        }

        return bestDirection;
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