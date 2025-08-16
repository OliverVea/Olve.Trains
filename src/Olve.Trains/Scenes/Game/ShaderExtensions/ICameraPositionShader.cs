using Silk.NET.Maths;

namespace Olve.CodeGen;

public interface ICameraPositionShader
{
    Matrix4X4<float> View { get; set; }
    Matrix4X4<float> Projection { get; set; }
}

public interface ICameraDirectionShader
{
    Vector3D<float> CameraDirection { get; set; }
}