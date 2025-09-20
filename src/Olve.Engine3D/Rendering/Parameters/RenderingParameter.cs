using System.Diagnostics;
using Olve.Engine3D.Rendering.OpenGL.Handles;

namespace Olve.Engine3D.Rendering.Parameters;

public static class RenderingParameter
{
    [DebuggerDisplay("Matrix3X3 {Name}")]
    public class Matrix3X3(string name, Matrix3X3<float> value) : Base<Matrix3X3<float>>(name, value);

    [DebuggerDisplay("Matrix4X4 {Name}")]
    public class Matrix4X4(string name, Matrix4X4<float> value) : Base<Matrix4X4<float>>(name, value);

    [DebuggerDisplay("Vector2D {Name} (X: {Value.X}, Y: {Value.Y})")]
    public class Vector2D(string name, Vector2D<float> value) : Base<Vector2D<float>>(name, value);

    [DebuggerDisplay("Vector3D {Name} (X: {Value.X}, Y: {Value.Y}, Z: {Value.Z})")]
    public class Vector3D(string name, Vector3D<float> value) : Base<Vector3D<float>>(name, value);

    [DebuggerDisplay("Vector4D {Name} (X: {Value.X}, Y: {Value.Y}, Z: {Value.Z}, W: {Value.W})")]
    public class Vector4D(string name, Vector4D<float> value) : Base<Vector4D<float>>(name, value);

    [DebuggerDisplay("Float {Name} ({Value})")]
    public class Float(string name, float value) : Base<float>(name, value);

    [DebuggerDisplay("Bool {Name} ({Value})")]
    public class Bool(string name, bool value) : Base<bool>(name, value);

    [DebuggerDisplay("Texture {Name} ({Value})")]
    public class Texture(string name, Texture2D value) : Base<Texture2D>(name, value);

    public abstract class Base<T>(string name, T value)
    {
        public string Name { get; } = name;
        public T Value { get; } = value;
    }
}