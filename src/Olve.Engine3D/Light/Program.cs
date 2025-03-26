namespace Olve.Engine3D.Light;

class Program
{
    private static readonly Vector3D<float> Black = new(0, 0, 0);
    private static readonly Vector3D<float> Red = new(1, 0, 0);
    private static readonly Vector3D<float> Orange = new(1, 0.5f, 0);
    private static readonly Vector3D<float> Yellow = new(1, 1, 0);
    private static readonly Vector3D<float> White = new(1, 1, 1);


    static void Main(string[] args)
    {
        List<KeyFrame<Vector3D<float>>> keyframes =
        [
            new(0, Black),
            new(4.75f, Black),
            new(5, Red),
            new(5.5f, Orange),
            new(7, Yellow),
            new(8, White),
            new(16, White),
            new(17f, Yellow),
            new(18.5f, Orange),
            new(19, Red),
            new(19.25f, Black),
            new(24, Black)
        ];

        var interpolator = new CatmullRom3(keyframes);

        // Create a list to accumulate color tuples.
        var colorTuples = new List<string>();

        // Sample colors for each hour from 0 to 24.
        for (var t = 0f; t <= 23.9f; t += 0.1f)
        {
            var color = interpolator.Sample(t) * 255;
            colorTuples.Add($"({color.X}, {color.Y}, {color.Z})");
        }

        // Output the colors as a single list.
        var output = "[" + string.Join(", ", colorTuples) + "]";
        Console.WriteLine(output);
    }
}