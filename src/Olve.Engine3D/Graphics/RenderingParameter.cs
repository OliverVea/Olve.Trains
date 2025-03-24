using Olve.Engine3D.Rendering.OpenGL.Handles;

namespace Olve.Engine3D.Graphics;

public static class RenderingParameter
{
    public class Matrix4X4(string name, Matrix4X4<float> value) : Base<Matrix4X4<float>>(name, value);
    public class Vector3D(string name, Vector3D<float> value) : Base<Vector3D<float>>(name, value);
    public class Float(string name, float value) : Base<float>(name, value);
    public class Texture(string name, Texture2D value) : Base<Texture2D>(name, value);

    public abstract class Base<T>(string name, T value)
    {
        public string Name { get; } = name;
        public T Value { get; } = value;
    }
}