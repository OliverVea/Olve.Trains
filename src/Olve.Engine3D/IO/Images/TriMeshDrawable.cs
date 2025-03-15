using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = Olve.Engine3D.Graphics.IDrawable;

namespace Olve.Engine3D.IO.Images;

public class TriMeshDrawable(GraphicsDevice graphicsDevice, TriMesh triMesh, IReadOnlyCollection<Effect> effects) : IDrawable
{
    private bool _initialized;

    private readonly VertexBuffer _vertexBuffer = new(graphicsDevice, typeof(VertexPosition),
        triMesh.Vertices.Length, BufferUsage.WriteOnly);

    private readonly IndexBuffer _indexBuffer = new(graphicsDevice, IndexElementSize.ThirtyTwoBits,
        triMesh.Indices.Length, BufferUsage.WriteOnly);
    
    public void Initialize()
    {
        if (_initialized) return;

        _vertexBuffer.SetData(triMesh.Vertices);
        _indexBuffer.SetData(triMesh.Indices);
        
        _initialized = true;
    }

    public void Draw(Matrix view, Matrix projection)
    {
        if (!_initialized)
        {
            Initialize();
        }
        
        graphicsDevice.SetVertexBuffer(_vertexBuffer);
        graphicsDevice.Indices = _indexBuffer;
        
        foreach (var effect in effects)
        {
            if (effect is BasicEffect basicEffect)
            {
                basicEffect.Projection = projection;
                basicEffect.View = view;
                basicEffect.World = Matrix.Identity;
            
                foreach (var pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, triMesh.Indices.Length / 3);
                }
            }
            
        }
    }
}