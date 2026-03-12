namespace Olve.Engine3D.Rendering;

public interface IFrameFormat;

public interface IFrameFormat<T0> : IFrameFormat where T0 : unmanaged;

public interface IFrameFormat<T0, T1> : IFrameFormat where T0 : unmanaged where T1 : unmanaged;

public interface IFrameFormat<T0, T1, T2> : IFrameFormat where T0 : unmanaged where T1 : unmanaged where T2 : unmanaged;
