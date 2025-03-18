namespace Olve.Engine3D.Scenes;

public enum SceneState
{
    /// <summary>
    /// Scene is loaded and active
    /// </summary>
    Active,

    /// <summary>
    /// Scene is loaded but not active
    /// </summary>
    Inactive,

    /// <summary>
    /// Scene is not loaded
    /// </summary>
    Unloaded,
}