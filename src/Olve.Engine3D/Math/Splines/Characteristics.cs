namespace Olve.Engine3D.Math.Splines;

public static class Characteristics
{
    public static readonly Matrix4X4<float> CatmullRom = new(
        0,      1,      0,     0,
        -0.5f,  0,    0.5f,     0,
        1,    -2.5f,    2,   -0.5f,
        -0.5f,  1.5f, -1.5f,  0.5f);

    public static readonly Matrix4X4<float> Hermite = new(
        2, -2, 1, 1,
        -3, 3, -2, -1,
        0, 0, 1, 0,
        1, 0, 0, 0);
}