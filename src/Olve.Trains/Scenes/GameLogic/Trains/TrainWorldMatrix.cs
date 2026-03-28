using Olve.Engine3D.Math;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public static class TrainWorldMatrix
{
    public const float TrainScale = 0.6f;

    public static Matrix4X4<float> Compute(TrainDirection direction, Position3D splinePosition)
    {
        var worldMatrix = Matrix4X4.CreateScale(TrainScale);
        if (direction == TrainDirection.Forward)
        {
            worldMatrix *= Matrix4X4.CreateRotationY(float.Pi);
        }

        worldMatrix *= splinePosition.ToMatrix4X4();
        return worldMatrix;
    }
}
