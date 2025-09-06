namespace Olve.Engine3D.Utilities;

public class NotInitializedException<T>() : Exception($"The {typeof(T).Name} has not been initialized.");