using Olve.Engine3D.GUI.Layout;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Elements;

public class RadioButton : GuiElement
{
    public Box Background { get; }
    public Box Indicator { get; }

    public required Id<RadioButtonGroup> Group { get; init; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            IsSelectedDirty = true;
        }
    }

    internal bool IsSelectedDirty { get; set; }

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

    public int IndicatorSize { get; init; } = 12;
    public RGBA IndicatorColor { get; init; } = new(0.8f, 0.8f, 0.8f, 1f);

    public RadioButton()
    {
        Interactive = false;

        Indicator = new Box
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "RadioButton/Indicator",
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
            Name = "RadioButton/Background",
            Interactive = true,
            InheritParentState = false,
            Width = Size,
            Height = Size,
            BackgroundColor = BackgroundColor,
            BorderRadius = Size / 2f,
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
