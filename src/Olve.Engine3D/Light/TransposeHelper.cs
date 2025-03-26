namespace Olve.Engine3D.Light;

public static class TransposeHelper
{
    public static Matrix4X4<float> Transpose(this Matrix4X4<float> matrix)
    {
        return Matrix4X4.Transpose(matrix);
    }
}