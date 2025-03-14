using Microsoft.Xna.Framework.Input;

namespace Olve.Engine3D.Input;

public readonly record struct KeyStateChange(Keys Key, KeyState State);