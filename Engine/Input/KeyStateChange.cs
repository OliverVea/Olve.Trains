using Microsoft.Xna.Framework.Input;

namespace Engine.Input;

public readonly record struct KeyStateChange(Keys Key, KeyState State);