using Olve.Engine3D.GUI.Layout;

namespace Olve.Engine3D.GUI.Elements;

public class Checkbox : GuiElement
{
    public Box Background { get; }
    public Box Indicator { get; }

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            IsCheckedDirty = true;
        }
    }

    internal bool IsCheckedDirty { get; set; }

    public int Size { get; set; } = 20;
    public float Weight { get; set; }
    public float Margin { get; set; }
    public float? MarginBottom { get; set; }
    public float? MarginTop { get; set; }
    public float? MarginLeft { get; set; }
    public float? MarginRight { get; set; }
    public float? MarginVertical { get; set; }
    public float? MarginHorizontal { get; set; }

    public RGBA BackgroundColor { get; init; } = new(0.3f, 0.3f, 0.3f, 1f);
    public float BackgroundBorderRadius { get; init; } = 4f;

    public int IndicatorSize { get; init; } = 12;
    public RGBA IndicatorColor { get; init; } = new(0.8f, 0.8f, 0.8f, 1f);

    public Checkbox()
    {
        Interactive = false;

        Indicator = new Box
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Checkbox/Indicator",
            Interactive = false,
            InheritParentState = false,
            Width = IndicatorSize,
            Height = IndicatorSize,
            BackgroundColor = IndicatorColor,
            BorderRadius = IndicatorSize / 2f,
        };

        Background = new Box
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Checkbox/Background",
            Interactive = true,
            InheritParentState = false,
            Width = Size,
            Height = Size,
            BackgroundColor = BackgroundColor,
            BorderRadius = BackgroundBorderRadius,
            Justify = Justify.Center,
            Align = Align.Center,
            Children = [Indicator],
        };

        Children = [Background];
    }

    public override LayoutBox? LayoutBox => new LayoutBox
    {
        Size = new SizeSpec(
            PreferredWidth: new Dp(Size),
            PreferredHeight: new Dp(Size),
            ResizingWeight: Weight),
        LayoutAxis = UIAxis.X,
        Justify = Justify.Center,
        Align = Align.Center,
        Margin = new Thickness(
            new Dp(MarginLeft ?? MarginHorizontal ?? Margin),
            new Dp(MarginTop ?? MarginVertical ?? Margin),
            new Dp(MarginRight ?? MarginHorizontal ?? Margin),
            new Dp(MarginBottom ?? MarginVertical ?? Margin)),
    };
}
