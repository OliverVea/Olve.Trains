using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine;

public readonly record struct KeyRotation(Keys Key, Quaternion Rotation);