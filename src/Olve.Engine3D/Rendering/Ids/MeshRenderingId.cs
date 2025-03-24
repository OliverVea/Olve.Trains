using Olve.Engine3D.Rendering.OpenGL.Handles;

namespace Olve.Engine3D.Rendering;

public readonly record struct MeshRenderingId(uint Id, VAO VAO, VBO VBO, EBO EBO);