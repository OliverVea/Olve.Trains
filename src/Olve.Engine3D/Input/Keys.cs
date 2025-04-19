using Silk.NET.Input;

namespace Olve.Engine3D.Input;

public static class Keys
{
    public static readonly IReadOnlyList<Key> NumericKeys =
    [
        Key.Number0,
        Key.Number1,
        Key.Number2,
        Key.Number3,
        Key.Number4,
        Key.Number5,
        Key.Number6,
        Key.Number7,
        Key.Number8,
        Key.Number9
    ];

    public static readonly IReadOnlyList<Key> LetterKeys =
    [
        Key.A,
        Key.B,
        Key.C,
        Key.D,
        Key.E,
        Key.F,
        Key.G,
        Key.H,
        Key.I,
        Key.J,
        Key.K,
        Key.L,
        Key.M,
        Key.N,
        Key.O,
        Key.P,
        Key.Q,
        Key.R,
        Key.S,
        Key.T,
        Key.U,
        Key.V,
        Key.W,
        Key.X,
        Key.Y,
        Key.Z
    ];

    public static readonly IReadOnlyList<Key> AlphaNumericKeys = new List<Key>(NumericKeys).Concat(LetterKeys).ToList();

    public static bool TryGetChar(this Key key, out char character, bool shift = false)
    {
        character = key switch
        {
            Key.A => shift ? 'A' : 'a',
            Key.B => shift ? 'B' : 'b',
            Key.C => shift ? 'C' : 'c',
            Key.D => shift ? 'D' : 'd',
            Key.E => shift ? 'E' : 'e',
            Key.F => shift ? 'F' : 'f',
            Key.G => shift ? 'G' : 'g',
            Key.H => shift ? 'H' : 'h',
            Key.I => shift ? 'I' : 'i',
            Key.J => shift ? 'J' : 'j',
            Key.K => shift ? 'K' : 'k',
            Key.L => shift ? 'L' : 'l',
            Key.M => shift ? 'M' : 'm',
            Key.N => shift ? 'N' : 'n',
            Key.O => shift ? 'O' : 'o',
            Key.P => shift ? 'P' : 'p',
            Key.Q => shift ? 'Q' : 'q',
            Key.R => shift ? 'R' : 'r',
            Key.S => shift ? 'S' : 's',
            Key.T => shift ? 'T' : 't',
            Key.U => shift ? 'U' : 'u',
            Key.V => shift ? 'V' : 'v',
            Key.W => shift ? 'W' : 'w',
            Key.X => shift ? 'X' : 'x',
            Key.Y => shift ? 'Y' : 'y',
            Key.Z => shift ? 'Z' : 'z',
            Key.Number0 => '0',
            Key.Number1 => shift ? '!' : '1',
            Key.Number2 => '2',
            Key.Number3 => '3',
            Key.Number4 => '4',
            Key.Number5 => '5',
            Key.Number6 => '6',
            Key.Number7 => '7',
            Key.Number8 => '8',
            Key.Number9 => '9',
            Key.Minus => shift ? '_' : '-',
            Key.Space => ' ',
            Key.Period => '.',
            Key.Comma => ',',
            _ => '\0'
        };

        return character != '\0';
    }
}