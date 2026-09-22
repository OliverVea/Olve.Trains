using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

internal interface ISceneParameters
{
    void Complete();
}

public sealed class SceneParameters<T>(Id<IScene> sceneId, Func<T> defaults) : ISceneParameters
    where T : class
{
    private T? _value;
    private bool _completed;

    public T Value => _completed
        ? _value!
        : throw new InvalidOperationException(
            $"Parameters of type {typeof(T).Name} for scene '{sceneId}' were read before the scene started loading.");

    internal void Set(T value) => _value = value;

    void ISceneParameters.Complete()
    {
        _value ??= defaults();
        _completed = true;
    }
}
