using Microsoft.Xna.Framework;

namespace Engine.Camera.Projections;

public interface IProjection
{
    float AspectRatio { get; set; }
    float NearPlane { get; set; }
    float FarPlane { get; set; }

    Matrix GetProjectionMatrix();
}