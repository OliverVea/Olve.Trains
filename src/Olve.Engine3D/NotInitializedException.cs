namespace Olve.Engine3D;

public class NotInitializedException<T>() : Exception($"The {typeof(T).Name} has not been initialized.");