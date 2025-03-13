using Microsoft.Xna.Framework;

namespace Engine.Camera.Projections;

public abstract class ProjectionBase : IProjection
{
    public float AspectRatio { get; set; } = 1f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 100f;

    public abstract Matrix GetProjectionMatrix();
}