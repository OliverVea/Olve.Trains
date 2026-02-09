using System.Diagnostics;

namespace Olve.Engine3D.GUI.Layout;

[DebuggerDisplay("{Display()}")]
public readonly record struct DpPxRatio(float DpToPx)
{
    private readonly float _pxToDp = 1f / DpToPx;
    public Px ToPx(Dp dp) => new((int)MathF.Round(dp.Value * DpToPx));
    public Dp ToDp(Px px) => new(px.Value * _pxToDp);

    public string Display() => DpToPx >= 1 ? $"{DpToPx}dp per px" :  $"{_pxToDp}px per dp";
}