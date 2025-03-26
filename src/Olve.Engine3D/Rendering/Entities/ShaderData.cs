using System.Diagnostics.CodeAnalysis;

namespace Olve.Engine3D.Rendering.Entities;

public class ShaderData
{
    public ShaderData()
    {
        
    }
    
    [SetsRequiredMembers]
    public ShaderData(string name, string vertexSource, string fragmentSource)
    {
        Name = name;
        VertexSource = vertexSource;
        FragmentSource = fragmentSource;
    }
    
    public required string Name { get; set; }
    
    public string? VertexPath { get; set; }
    public required string VertexSource { get; set; }
    
    public string? FragmentPath { get; set; }
    public required string FragmentSource { get; set; }

    public string? GeometryPath { get; set; }
    public string? GeometrySource { get; set; }
}