namespace Olve.Engine3D.Graphics;

public class Entity
{
    public EntityId EntityId { get; init; }= new();
    public Transform Transform { get; init; } = new();
    public bool Enabled { get; set; } = true;
    public bool Visible { get; set; } = true;
    public RenderingEntityId<Model>? ModelRenderingId { get; init; }
}