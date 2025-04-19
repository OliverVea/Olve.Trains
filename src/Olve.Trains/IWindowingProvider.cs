using Jab;
using Olve.Engine3D;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Olve.Trains;

[ServiceProviderModule]
[Singleton(typeof(Provider<IWindow>))]
[Singleton(typeof(Provider<IInputContext>))]
public interface IWindowingProvider;