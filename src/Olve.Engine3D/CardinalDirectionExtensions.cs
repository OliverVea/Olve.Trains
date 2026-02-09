namespace Olve.Engine3D;

public static class CardinalDirectionExtensions
{
    public static CardinalDirection RotateClockwise(this CardinalDirection direction)
    {
        return direction switch
        {
            CardinalDirection.North => CardinalDirection.East,
            CardinalDirection.South => CardinalDirection.West,
            CardinalDirection.East => CardinalDirection.South,
            CardinalDirection.West => CardinalDirection.North,
            _ => direction
        };
    }
    
    public static CardinalDirection RotateCounterClockwise(this CardinalDirection direction)
    {
        return direction switch
        {
            CardinalDirection.North => CardinalDirection.West,
            CardinalDirection.South => CardinalDirection.East,
            CardinalDirection.East => CardinalDirection.North,
            CardinalDirection.West => CardinalDirection.South,
            _ => direction
        };
    }
}