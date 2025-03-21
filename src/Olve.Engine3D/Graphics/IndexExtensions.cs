namespace Olve.Engine3D.Graphics;

public static class IndexExtensions
{
    public static void CopyToSpan(this TriangleIndex[] indices, Span<uint> buffer)
    {
        for (var i = 0; i < indices.Length; i++)
        {
            buffer[i * 3] = indices[i].A;
            buffer[i * 3 + 1] = indices[i].B;
            buffer[i * 3 + 2] = indices[i].C;
        }
    }

    public static void CopyToSpan(this LineIndex[] indices, Span<uint> buffer)
    {
        for (var i = 0; i < indices.Length; i++)
        {
            buffer[i * 2] = indices[i].A;
            buffer[i * 2 + 1] = indices[i].B;
        }
    }
}