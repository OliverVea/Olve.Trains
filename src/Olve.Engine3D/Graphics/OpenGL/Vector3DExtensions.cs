namespace Olve.Engine3D.Graphics.OpenGL;

public static class Vector3DExtensions
{
    public static void CopyToSpan(this Vector3D<float>[] vectors, Span<float> buffer)
    {
        for (var i = 0; i < vectors.Length; i++)
        {
            buffer[i * 3] = vectors[i].X;
            buffer[i * 3 + 1] = vectors[i].Y;
            buffer[i * 3 + 2] = vectors[i].Z;
        }
    }
}