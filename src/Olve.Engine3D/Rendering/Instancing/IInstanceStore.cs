namespace Olve.Engine3D.Rendering.Instancing;

internal interface IInstanceStore
{
    int Count { get; }
    float[] MarshalToFloats();
}
