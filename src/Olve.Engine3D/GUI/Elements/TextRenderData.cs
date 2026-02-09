using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.GUI.Layout;

namespace Olve.Engine3D.GUI.Elements;

public record TextRenderData(
    FontData? Font,
    string Content,
    float FontSize,
    Vector4D<float> Color,
    Align Align
);