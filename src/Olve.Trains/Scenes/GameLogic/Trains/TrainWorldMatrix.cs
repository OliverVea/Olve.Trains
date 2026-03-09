using Olve.Engine3D.Math;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public static class TrainWorldMatrix
{
    public static Matrix4X4<float> Compute(float velocity, Position3D splinePosition)
    {
        var worldMatrix = Matrix4X4<float>.Identity;
        if (velocity > 0)
        {
            worldMatrix *= Matrix4X4.CreateRotationY(float.Pi);
        }

        worldMatrix *= splinePosition.ToMatrix4X4();
        return worldMatrix;
    }
}
