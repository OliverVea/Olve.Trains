using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Shaders;
using Silk.NET.OpenGL;

namespace Olve.Trains.RenderPassPrototype;

public static class RenderStateExtensions
{
    public static void Apply(this RenderState state, GL gl)
    {
        state.Blend.Apply(gl);

        gl.DepthMask(state.DepthWrite);

        if (state.DepthTest)
            gl.Enable(GLEnum.DepthTest);
        else
            gl.Disable(GLEnum.DepthTest);
    }

    public static void Apply(this BlendMode blend, GL gl)
    {
        switch (blend)
        {
            case BlendMode.None:
                gl.Disable(GLEnum.Blend);
                break;
            case BlendMode.Alpha:
                gl.Enable(GLEnum.Blend);
                gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Premultiplied:
                gl.Enable(GLEnum.Blend);
                gl.BlendFunc(GLEnum.One, GLEnum.OneMinusSrcAlpha);
                break;
            case BlendMode.Additive:
                gl.Enable(GLEnum.Blend);
                gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
                break;
        }
    }

    public static void Apply(this ClearFlags flags, GL gl)
    {
        var mask = (ClearBufferMask)0;
        if (flags.HasFlag(ClearFlags.Color)) mask |= ClearBufferMask.ColorBufferBit;
        if (flags.HasFlag(ClearFlags.Depth)) mask |= ClearBufferMask.DepthBufferBit;

        if (mask != 0)
            gl.Clear((uint)mask);
    }
}
