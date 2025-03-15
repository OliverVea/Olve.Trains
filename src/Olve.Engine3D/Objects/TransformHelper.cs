using Microsoft.Xna.Framework;
using Olve.Engine3D.Core;

namespace Olve.Engine3D.Objects;

public static class TransformHelper
{
    public static Matrix GetWorldMatrix(this Transform transform)
    {
        var translation = Matrix.CreateTranslation(transform.Position);
        var rotation = Matrix.CreateFromQuaternion(transform.Rotation);

        return rotation * translation;
    }
}