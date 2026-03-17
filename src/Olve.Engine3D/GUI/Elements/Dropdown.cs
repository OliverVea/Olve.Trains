using Olve.Engine3D.GUI.Layout;

namespace Olve.Engine3D.GUI.Elements;

public class Dropdown : GuiElement
{
    public Box Button { get; }
    public Text ButtonLabel { get; }
    public Box OptionsContainer { get; }
    public List<Box> OptionBoxes { get; } = [];

    private string[] _options = [];
    public required string[] Options
    {
        get => _options;
        init
        {
            _options = value;
            BuildOptionElements();
        }
    }

    private int _selectedIndex = -1;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = value < 0 || Options.Length == 0 ? -1 : int.Clamp(value, 0, Options.Length - 1);
            if (_selectedIndex == clamped) return;
            _selectedIndex = clamped;
            IsSelectedIndexDirty = true;
        }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            IsExpandedDirty = true;
        }
    }

    internal bool IsSelectedIndexDirty { get; set; }
    internal bool IsExpandedDirty { get; set; }

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

    public int ButtonHeight { get; init; } = 30;
    public RGBA ButtonBackgroundColor { get; init; } = new(0.3f, 0.3f, 0.3f, 1f);
    public float ButtonBorderRadius { get; init; } = 4f;

    public RGBA ButtonLabelColor { get; init; } = new(0.9f, 0.9f, 0.9f, 1f);
    public float ButtonLabelFontSize { get; init; } = 14f;

    public int OptionHeight { get; init; } = 26;
    public RGBA OptionBackgroundColor { get; init; } = new(0.25f, 0.25f, 0.25f, 1f);

    public RGBA OptionLabelColor { get; init; } = new(0.85f, 0.85f, 0.85f, 1f);
    public float OptionLabelFontSize { get; init; } = 12f;

    public string PlaceholderText { get; init; } = "Select...";

    public Dropdown()
    {
        Interactive = false;

        ButtonLabel = new Text
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Dropdown/Button/Label",
            Interactive = false,
            Content = PlaceholderText,
            Color = ButtonLabelColor,
            FontSize = ButtonLabelFontSize,
            Align = Align.Start,
        };

        Button = new Box
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Dropdown/Button",
            Interactive = true,
            InheritParentState = false,
            Height = ButtonHeight,
            Weight = 1f,
            BackgroundColor = ButtonBackgroundColor,
            BorderRadius = ButtonBorderRadius,
            Justify = Justify.Start,
            Align = Align.Center,
            PaddingHorizontal = 8f,
            Children = [ButtonLabel],
        };

        OptionsContainer = new Box
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Dropdown/OptionsContainer",
            Interactive = false,
            InheritParentState = false,
            Weight = 0f,
            BackgroundColor = new RGBA(0f, 0f, 0f, 0f),
            Vertical = true,
            Justify = Justify.Start,
            Align = Align.Stretch,
            Children = [],
        };

        Children = [Button, OptionsContainer];
    }

    private void BuildOptionElements()
    {
        OptionBoxes.Clear();
        var optionElements = new List<GuiElement>();

        for (var i = 0; i < Options.Length; i++)
        {
            var optionText = Options[i];

            var label = new Text
            {
                Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
                Name = $"Dropdown/Option{i}/Label",
                Interactive = false,
                Content = optionText,
                Color = OptionLabelColor,
                FontSize = OptionLabelFontSize,
                Align = Align.Start,
            };

            var optionBox = new Box
            {
                Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
                Name = $"Dropdown/Option{i}",
                Interactive = true,
                InheritParentState = false,
                Height = OptionHeight,
                Weight = 0f,
                BackgroundColor = OptionBackgroundColor,
                Justify = Justify.Start,
                Align = Align.Center,
                PaddingHorizontal = 8f,
                Children = [label],
            };

            optionElements.Add(optionBox);
            OptionBoxes.Add(optionBox);
        }

        OptionsContainer.Children = optionElements.ToArray();
    }

    public override LayoutBox? LayoutBox => new LayoutBox
    {
        Size = new SizeSpec(
            PreferredWidth: Dp.FromNullable(Width),
            PreferredHeight: Dp.FromNullable(Height),
            ResizingWeight: Weight),
        LayoutAxis = UIAxis.Y,
        Justify = Justify.Start,
        Align = Align.Stretch,
        Margin = new Thickness(
            new Dp(MarginLeft ?? MarginHorizontal ?? Margin),
            new Dp(MarginTop ?? MarginVertical ?? Margin),
            new Dp(MarginRight ?? MarginHorizontal ?? Margin),
            new Dp(MarginBottom ?? MarginVertical ?? Margin)),
    };
}
