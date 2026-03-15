using Olve.Engine3D.GUI.Layout;

namespace Olve.Engine3D.GUI.Elements;

public class Slider : GuiElement
{
    public Box Track { get; }
    public Box Thumb { get; }

    public float MinValue { get; set; }
    public float MaxValue { get; set; } = 1f;
    public float Step { get; set; } = 0.1f;

    private float _value;
    public float Value
    {
        get => _value;
        set
        {
            var clamped = System.Math.Clamp(value, MinValue, MaxValue);
            var offset = clamped - MinValue;
            var snapped = MinValue + float.Round(offset / Step) * Step;
            if (System.Math.Abs(_value - snapped) < float.Epsilon) return;
            _value = snapped;
            IsValueDirty = true;
        }
    }

    internal bool IsValueDirty { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }
    public float Weight { get; set; }
    public float Margin { get; set; }
    public float? MarginBottom { get; set; }
    public float? MarginTop { get; set; }
    public float? MarginLeft { get; set; }
    public float? MarginRight { get; set; }
    public float? MarginVertical { get; set; }
    public float? MarginHorizontal { get; set; }

    public int TrackHeight { get; init; } = 8;
    public RGBA TrackColor { get; init; } = new(0.3f, 0.3f, 0.3f, 1f);
    public float TrackBorderRadius { get; init; } = 4f;

    public int ThumbWidth { get; init; } = 16;
    public int ThumbHeight { get; init; } = 16;
    public RGBA ThumbColor { get; init; } = new(0.8f, 0.8f, 0.8f, 1f);
    public float ThumbBorderRadius { get; init; } = 8f;

    public Slider()
    {
        Interactive = false;

        Thumb = new Box
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Slider/Thumb",
            Interactive = true,
            InheritParentState = false,
            Width = ThumbWidth,
            Height = ThumbHeight,
            BackgroundColor = ThumbColor,
            BorderRadius = ThumbBorderRadius,
        };

        Track = new Box
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Slider/Track",
            Interactive = false,
            InheritParentState = true,
            Height = TrackHeight,
            Weight = 1f,
            BackgroundColor = TrackColor,
            BorderRadius = TrackBorderRadius,
            Align = Align.Center,
            Children = [Thumb],
        };

        Children = [Track];
    }

    public override LayoutBox? LayoutBox => new LayoutBox
    {
        Size = new SizeSpec(
            PreferredWidth: Dp.FromNullable(Width),
            PreferredHeight: Dp.FromNullable(Height),
            ResizingWeight: Weight),
        LayoutAxis = UIAxis.X,
        Align = Align.Center,
        Margin = new Thickness(
            new Dp(MarginLeft ?? MarginHorizontal ?? Margin),
            new Dp(MarginTop ?? MarginVertical ?? Margin),
            new Dp(MarginRight ?? MarginHorizontal ?? Margin),
            new Dp(MarginBottom ?? MarginVertical ?? Margin)),
    };
}
