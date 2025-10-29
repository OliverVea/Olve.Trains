namespace Olve.Engine3D.GUI.Layout;

/// <summary>
/// Defines how an element with an aspect ratio constraint should fit within available space.
/// Similar to CSS object-fit property.
/// </summary>
public enum FitMode
{
    /// <summary>
    /// Scale to fit entirely within bounds while maintaining aspect ratio.
    /// May leave empty space on one axis. This is the default.
    /// </summary>
    Contain,

    /// <summary>
    /// Scale to fill bounds completely while maintaining aspect ratio.
    /// May crop content on one axis.
    /// </summary>
    Cover,

    /// <summary>
    /// Stretch to fill bounds, ignoring aspect ratio.
    /// </summary>
    Fill
}
