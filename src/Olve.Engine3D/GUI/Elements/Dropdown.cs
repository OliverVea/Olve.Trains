using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;

namespace Olve.Engine3D.GUI.Elements;

public class Dropdown : GuiElement
{
    public Box Background { get; }
    public Text Label { get; }

    private string[] _options = [];
    public required string[] Options
    {
        get => _options;
        init => _options = value;
    }

    public int SelectedIndex
    {
        get;
        set
        {
            var clamped = value < 0 || Options.Length == 0 ? -1 : int.Clamp(value, 0, Options.Length - 1);
            if (field == clamped) return;
            field = clamped;
            IsSelectedIndexDirty = true;
        }
    } = -1;

    internal bool IsSelectedIndexDirty { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; } = 30;
    public float Weight { get; set; }
    public float Margin { get; set; }
    public float? MarginBottom { get; set; }
    public float? MarginTop { get; set; }
    public float? MarginLeft { get; set; }
    public float? MarginRight { get; set; }
    public float? MarginVertical { get; set; }
    public float? MarginHorizontal { get; set; }

    public RGBA BackgroundColor { get; init; } = new(0.3f, 0.3f, 0.3f, 1f);
    public float BorderRadius { get; init; } = 4f;
    public float PaddingHorizontal { get; init; } = 8f;

    public RGBA LabelColor { get; init; } = new(0.9f, 0.9f, 0.9f, 1f);
    public float LabelFontSize { get; init; } = 14f;

    public string PlaceholderText { get; init; } = "Select...";

    public Dropdown()
    {
        Interactive = false;

        Label = new Text
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Dropdown/Label",
            Interactive = false,
            Content = PlaceholderText,
            Color = LabelColor,
            FontSize = LabelFontSize,
            Align = Align.Start,
        };

        Background = new Box
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Dropdown/Background",
            Interactive = true,
            InheritParentState = false,
            StyleKey = new StyleKey("DropdownStyle"),
            BackgroundColor = BackgroundColor,
            BorderRadius = BorderRadius,
            PaddingHorizontal = PaddingHorizontal,
            Justify = Justify.Start,
            Align = Align.Center,
            Children = [Label],
        };

        Children = [Background];
    }

    public override LayoutBox? LayoutBox => new LayoutBox
    {
        Size = new SizeSpec(
            PreferredWidth: Dp.FromNullable(Width),
            PreferredHeight: Dp.FromNullable(Height),
            ResizingWeight: Weight),
        LayoutAxis = UIAxis.X,
        Justify = Justify.Start,
        Align = Align.Center,
        Margin = new Thickness(
            new Dp(MarginLeft ?? MarginHorizontal ?? Margin),
            new Dp(MarginTop ?? MarginVertical ?? Margin),
            new Dp(MarginRight ?? MarginHorizontal ?? Margin),
            new Dp(MarginBottom ?? MarginVertical ?? Margin)),
    };
}
