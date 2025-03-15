using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Olve.Engine3D.Core;

public readonly record struct KeyRotation(Keys Key, Quaternion Rotation);