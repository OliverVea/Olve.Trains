namespace Olve.Trains.AssetPipeline.Shaders;

public static class UniformTypeExtensions
{
    public static string GetDataType(this Uniform uniform)
    {
        return uniform.Type switch
        {
            UniformType.Bool => "bool",
            UniformType.Float => "float",
            UniformType.Vector2 => "Vector2D<float>",
            UniformType.Vector3 => "Vector3D<float>",
            UniformType.Vector4  => "Vector4D<float>",
            UniformType.Matrix3 => "Matrix3X3<float>",
            UniformType.Matrix4 => "Matrix4X4<float>",
            UniformType.Sampler2D => $"TextureId<{uniform.PixelType}>",
            _ => "Unknown"
        };
    }

    public static string GetRenderParameterType(this UniformType uniformType)
    {
        return uniformType switch
        {
            UniformType.Bool => "RenderingParameter.Bool",
            UniformType.Float => "RenderingParameter.Float",
            UniformType.Vector2 => "RenderingParameter.Vector2D",
            UniformType.Vector3 => "RenderingParameter.Vector3D",
            UniformType.Vector4  => "RenderingParameter.Vector4D", 
            UniformType.Matrix3 => "RenderingParameter.Matrix3X3",
            UniformType.Matrix4 => "RenderingParameter.Matrix4X4",
            UniformType.Sampler2D => "RenderingParameter.Texture",
            _ => "Unknown"
        };
    }

    public static string GetInitializer(this UniformType uniformType)
    {
        return uniformType switch
        {
            UniformType.Matrix3 => " = Matrix3X3<float>.Identity;",
            UniformType.Matrix4 => " = Matrix4X4<float>.Identity;",
            _ => ""
        };
    }

    public static string GetVertexDataType(this UniformType uniformType)
    {
        return uniformType switch
        {
            UniformType.Float => "float",
            UniformType.Vector2 => "Vector2D<float>",
            UniformType.Vector3 => "Vector3D<float>",
            UniformType.Vector4 => "Vector4D<float>",
            UniformType.Matrix4 => "Matrix4X4<float>",
            _ => throw new ArgumentException($"Unsupported vertex attribute type: {uniformType}")
        };
    }

    public static int GetComponentCount(this UniformType uniformType)
    {
        return uniformType switch
        {
            UniformType.Float => 1,
            UniformType.Vector2 => 2,
            UniformType.Vector3 => 3,
            UniformType.Vector4 => 4,
            UniformType.Matrix4 => 16,
            _ => throw new ArgumentException($"Unsupported vertex attribute type: {uniformType}")
        };
    }

    /// <summary>
    /// Number of attribute slots this type occupies. mat4 uses 4 slots (one per column).
    /// </summary>
    public static int GetLocationSlotCount(this UniformType uniformType)
    {
        return uniformType switch
        {
            UniformType.Matrix4 => 4,
            _ => 1
        };
    }
}