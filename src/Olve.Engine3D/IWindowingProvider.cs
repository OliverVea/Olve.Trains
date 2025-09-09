using Jab;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Olve.Engine3D;

[ServiceProviderModule]
[Singleton(typeof(Provider<IWindow>))]
[Singleton(typeof(Provider<IInputContext>))]
public interface IWindowingProvider;