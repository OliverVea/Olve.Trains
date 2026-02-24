namespace Olve.Engine3D.Rendering;

public interface IWithWorldMatrix<TSelf> where TSelf : IWithWorldMatrix<TSelf>
{
    Matrix4X4<float> WorldMatrix { get; }
    static abstract TSelf WithWorldMatrix(TSelf self, Matrix4X4<float> worldMatrix);
}
