namespace Olve.Trains.AssetPipeline.Shaders;

public static class UniformTypeExtensions
{
    public static string GetDataType(this UniformType uniformType)
    {
        return uniformType switch
        {
            UniformType.Float => "float",
            UniformType.Vector2 => "Vector2D<float>",
            UniformType.Vector3 => "Vector3D<float>",
            UniformType.Matrix3 => "Matrix3X3<float>",
            UniformType.Matrix4 => "Matrix4X4<float>",
            UniformType.Sampler2D => "Texture2D",
            _ => "Unknown"
        };
    }

    public static string GetRenderParameterType(this UniformType uniformType)
    {
        return uniformType switch
        {
            UniformType.Float => "RenderingParameter.Float",
            UniformType.Vector2 => "RenderingParameter.Vector2D",
            UniformType.Vector3 => "RenderingParameter.Vector3D",
            UniformType.Matrix3 => "RenderingParameter.Matrix3X3",
            UniformType.Matrix4 => "RenderingParameter.Matrix4X4",
            UniformType.Sampler2D => "RenderingParameter.Texture",
            _ => "Unknown"
        };
    }
}