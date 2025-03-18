using Olve.Engine3D.Assets;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Shaders;

namespace Olve.Engine3D.Utilities;

public class EntityHelper
{
    public static Result<Entity> CreateQuad(
        ShaderSource<VertexShaderAsset> vertexShaderSource,
        ShaderSource<FragmentShaderAsset> fragmentShaderSource)
    {
        var geometry = GeometryHelper.CreateQuadGeometry();

        if (ShaderLoader.Create(vertexShaderSource, fragmentShaderSource)
            .TryPickProblems(out var problems, out var shader))
        {
            return problems.Prepend("Failed to load shaders for quad");
        }

        return new Entity
        {
            Transform = new Transform(),
            Enabled = true,
            Visible = true,
            Meshes = [
                new Mesh
                {
                    Geometry = geometry,
                    Material = new Material
                    {
                        Shader = shader
                    }
                }
            ]
        };
    }
}