using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine;

public readonly record struct KeyDirection(Keys Key, Vector3 Direction);