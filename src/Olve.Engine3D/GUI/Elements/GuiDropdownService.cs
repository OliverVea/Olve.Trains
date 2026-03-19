using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Elements;

public class GuiDropdownService(
    ILogger<GuiDropdownService> logger,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiNodeStateService guiNodeStateService,
    GuiMouseInputService guiMouseInputService,
    GuiAnchorService guiAnchorService,
    GuiLayoutService guiLayoutService) : ISceneService
{
    public int Priority => 0;

    public readonly record struct DropdownValueChangedMessage(
        Id<GuiElement> DropdownId,
        int OldSelectedIndex,
        int NewSelectedIndex,
        string? OldSelectedText,
        string? NewSelectedText);

    public Event<DropdownValueChangedMessage> OnValueChanged { get; } = new();

    private const int MaxOptions = 20;

    private readonly Dictionary<Id<GuiNode>, Dropdown> _dropdownNodes = new();
    private readonly Dictionary<Id<GuiNode>, int> _optionNodeToIndex = new();
    private Dropdown? _expandedDropdown;

    private Id<GuiAnchor> _overlayAnchorId;
    private Id<GuiNode> _overlayNodeId;

    private Id<GuiAnchor> _optionsPanelAnchorId;
    private Id<GuiElementRegistrations> _optionsPanelRegistrationId;
    private Id<GuiNode> _optionsPanelNodeId;
    private Box _optionsPanelRoot = null!;
    private Box _optionsPanelBox = null!;

    // Pool of pre-registered option slots
    private readonly record struct OptionSlot(Box Box, Text Label, Id<GuiNode> BoxNodeId, Id<GuiNode> LabelNodeId);
    private readonly List<OptionSlot> _optionSlots = new();
    // Each entry holds (dividerWrapperNodeId, dividerLineNodeId)
    private readonly List<(Id<GuiNode> Wrapper, Id<GuiNode> Line)> _dividerNodes = new();

    public Result Load()
    {
        // Fullscreen overlay anchor (depth 0, behind the options panel)
        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight, depth: 0)
            .TryPickProblems(out var problems, out _overlayAnchorId))
        {
            return problems.Prepend("Failed to register overlay anchor");
        }

        var overlayBox = new Box
        {
            Id = Id.FromName<GuiElement>("Dropdown/Overlay"),
            Name = "Dropdown/Overlay",
            Interactive = true,
            InheritParentState = false,
            Weight = 1f,
            BackgroundColor = new RGBA(0f, 0f, 0f, 0f),
        };

        if (guiElementService.RegisterElementAndChildren(_overlayAnchorId, overlayBox)
            .TryPickProblems(out problems, out _))
        {
            return problems.Prepend("Failed to register overlay element");
        }

        if (!guiElementService.TryGetAnyGuiNodeId(overlayBox.Id, out _overlayNodeId))
        {
            return new ResultProblem("Failed to resolve overlay node ID");
        }

        // Options panel anchor (depth 1, above the overlay)
        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight, depth: 1)
            .TryPickProblems(out problems, out _optionsPanelAnchorId))
        {
            return problems.Prepend("Failed to register options panel anchor");
        }

        // Build pool of option slot elements: [divider0, option0, divider1, option1, ...]
        // Divider before each option except the first
        var poolChildren = new List<GuiElement>();
        var optionBoxes = new List<(Box box, Text label)>();

        for (var i = 0; i < MaxOptions; i++)
        {
            if (i > 0)
            {
                poolChildren.Add(new Divider(marginHorizontal: 6f, color: new RGBA(1f, 0.4f, 0.7f, 0.5f))
                {
                    Id = Id.New<GuiElement>(),
                    Name = $"Dropdown/Pool/Divider/{i}",
                });
            }

            var label = new Text
            {
                Id = Id.New<GuiElement>(),
                Name = $"Dropdown/Pool/Option/{i}/Label",
                Interactive = false,
                Content = string.Empty,
            };

            var optionBox = new Box
            {
                Id = Id.New<GuiElement>(),
                Name = $"Dropdown/Pool/Option/{i}",
                Interactive = true,
                InheritParentState = false,
                StyleKey = new StyleKey("DropdownOptionStyle"),
                PaddingHorizontal = 8f,
                PaddingVertical = 4f,
                Align = Align.Center,
                Justify = Justify.Start,
                Children = [label],
            };

            poolChildren.Add(optionBox);
            optionBoxes.Add((optionBox, label));
        }

        _optionsPanelBox = new Box
        {
            Id = Id.New<GuiElement>(),
            Name = "Dropdown/SharedOptionsPanel",
            Interactive = true,
            InheritParentState = false,
            Weight = 0f,
            BackgroundColor = new RGBA(0.25f, 0.25f, 0.25f, 1f),
            Vertical = true,
            Justify = Justify.Start,
            Align = Align.Stretch,
            Children = poolChildren.ToArray(),
        };

        _optionsPanelRoot = new Box
        {
            Id = Id.New<GuiElement>(),
            Name = "Dropdown/SharedOptionsPanelRoot",
            Interactive = false,
            InheritParentState = false,
            Justify = Justify.Start,
            Align = Align.Start,
            Children = [_optionsPanelBox],
        };

        if (guiElementService.RegisterElementAndChildren(_optionsPanelAnchorId, _optionsPanelRoot)
            .TryPickProblems(out problems, out _optionsPanelRegistrationId))
        {
            return problems.Prepend("Failed to register options panel element");
        }

        if (!guiElementService.TryGetGuiNodeId(_optionsPanelBox.Id, _optionsPanelRegistrationId, out _optionsPanelNodeId))
        {
            return new ResultProblem("Failed to resolve options panel node ID");
        }

        // Resolve node IDs for all pool slots (box + label)
        foreach (var (box, label) in optionBoxes)
        {
            if (!guiElementService.TryGetGuiNodeId(box.Id, _optionsPanelRegistrationId, out var boxNodeId))
            {
                return new ResultProblem("Failed to resolve pool option node ID for '{0}'", box.Name);
            }

            if (!guiElementService.TryGetGuiNodeId(label.Id, _optionsPanelRegistrationId, out var labelNodeId))
            {
                return new ResultProblem("Failed to resolve pool option label node ID for '{0}'", label.Name);
            }

            _optionSlots.Add(new OptionSlot(box, label, boxNodeId, labelNodeId));
        }

        // Resolve node IDs for dividers (wrapper + line child)
        foreach (var child in poolChildren)
        {
            if (child is not Divider divider)
                continue;

            if (!guiElementService.TryGetGuiNodeId(divider.Id, _optionsPanelRegistrationId, out var wrapperNodeId))
            {
                return new ResultProblem("Failed to resolve pool divider node ID for '{0}'", divider.Name);
            }

            if (!guiElementService.TryGetGuiNodeId(divider.Line.Id, _optionsPanelRegistrationId, out var lineNodeId))
            {
                return new ResultProblem("Failed to resolve pool divider line node ID for '{0}'", divider.Name);
            }

            _dividerNodes.Add((wrapperNodeId, lineNodeId));
        }

        // Hide everything immediately
        HideAllPoolSlots();
        HidePanel();

        guiElementService.OnAdded.Subscribe(OnElementAdded);
        guiElementService.OnRemoved.Subscribe(OnElementRemoved);
        guiActivationService.GuiElementActivated.Subscribe(OnElementActivated);
        guiMouseInputService.OnPressedNode.Subscribe(OnNodePressed);

        return Result.Success();
    }

    public Result Unload()
    {
        guiElementService.OnAdded.Unsubscribe(OnElementAdded);
        guiElementService.OnRemoved.Unsubscribe(OnElementRemoved);
        guiActivationService.GuiElementActivated.Unsubscribe(OnElementActivated);
        guiMouseInputService.OnPressedNode.Unsubscribe(OnNodePressed);
        return Result.Success();
    }

    public Result Update()
    {
        UpdateDirtyDropdowns();
        return Result.Success();
    }

    private void OnElementAdded(GuiElementArgs args)
    {
        if (!guiElementService.TryGetElement(args.NodeId, out var element))
        {
            return;
        }

        if (element is not Dropdown dropdown)
        {
            return;
        }

        if (!guiElementService.TryGetGuiNodeId(dropdown.Background.Id, args.RegistrationId, out var bgNodeId))
        {
            logger.LogWarning("Could not resolve background node for dropdown {DropdownId}", dropdown.Id);
            return;
        }

        logger.LogDebug("Mapped background node {BgNodeId} for dropdown {DropdownId}", bgNodeId, dropdown.Id);
        _dropdownNodes[bgNodeId] = dropdown;
        _dropdownNodes[args.NodeId] = dropdown; // Also map the Dropdown wrapper node for activate-gui
        dropdown.IsSelectedIndexDirty = true;
    }

    private void OnElementRemoved(GuiElementArgs args)
    {
        if (_dropdownNodes.Remove(args.NodeId, out var dropdown))
        {
            if (_expandedDropdown == dropdown)
            {
                _expandedDropdown = null;
                CollapsePanel();
            }
        }
    }

    private void OnNodePressed(Id<GuiNode> nodeId)
    {
        if (_dropdownNodes.ContainsKey(nodeId))
        {
            return;
        }

        if (nodeId == _optionsPanelNodeId || _optionNodeToIndex.ContainsKey(nodeId))
        {
            return;
        }

        // Clicked overlay or any other node — collapse
        if (_expandedDropdown != null)
        {
            _expandedDropdown = null;
            CollapsePanel();
        }
    }

    private void OnElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        // Check if an option was clicked
        if (_optionNodeToIndex.TryGetValue(message.NodeId, out var optionIndex) && _expandedDropdown is { } active)
        {
            var oldIndex = active.SelectedIndex;
            var oldText = oldIndex >= 0 && oldIndex < active.Options.Length ? active.Options[oldIndex] : null;

            active.SelectedIndex = optionIndex;

            var newText = active.SelectedIndex >= 0 && active.SelectedIndex < active.Options.Length
                ? active.Options[active.SelectedIndex]
                : null;

            OnValueChanged.Invoke(new DropdownValueChangedMessage(active.Id, oldIndex, active.SelectedIndex, oldText, newText));
            logger.LogDebug("Dropdown {DropdownId} selected option {Index}: {Text}", active.Id, optionIndex, newText);

            _expandedDropdown = null;
            CollapsePanel();
            return;
        }

        if (!_dropdownNodes.TryGetValue(message.NodeId, out var dropdown))
        {
            return;
        }

        var expanding = _expandedDropdown != dropdown;

        if (_expandedDropdown != null && _expandedDropdown != dropdown)
        {
            _expandedDropdown = null;
            CollapsePanel();
        }

        if (expanding)
        {
            _expandedDropdown = dropdown;
            ShowPanelBelow(message.NodeId, dropdown);
        }
        else
        {
            _expandedDropdown = null;
            CollapsePanel();
        }

        logger.LogDebug("Dropdown {DropdownId} expanding={Expanding}", dropdown.Id, expanding);
    }

    private void ShowPanelBelow(Id<GuiNode> dropdownNodeId, Dropdown dropdown)
    {
        // Position the options panel anchor below the dropdown
        var updatedAnchor = new GuiAnchor(
            _optionsPanelAnchorId,
            AnchorPosition.BottomLeft,
            GrowthDirection.DownRight,
            Depth: 1,
            dropdownNodeId);

        if (guiAnchorService.UpdateAnchor(_optionsPanelAnchorId, updatedAnchor).TryPickProblems(out var problems))
        {
            logger.LogWarning("Failed to update options panel anchor: {Problems}", problems);
            return;
        }

        var optionCount = int.Min(dropdown.Options.Length, MaxOptions);

        // Update pool slots: show active options, hide excess
        _optionNodeToIndex.Clear();

        for (var i = 0; i < _optionSlots.Count; i++)
        {
            var slot = _optionSlots[i];

            if (i < optionCount)
            {
                // Configure and show this slot
                slot.Label.Content = dropdown.Options[i];
                slot.Label.Color = dropdown.LabelColor;
                slot.Label.FontSize = dropdown.LabelFontSize;
                slot.Box.PaddingHorizontal = dropdown.PaddingHorizontal;

                guiNodeStateService.UpdateState(slot.BoxNodeId, state => state | GuiNodeState.Show);
                guiNodeStateService.UpdateState(slot.LabelNodeId, state => state | GuiNodeState.Show);
                _optionNodeToIndex[slot.BoxNodeId] = i;
            }
            else
            {
                // Hide unused slot
                guiNodeStateService.UpdateState(slot.BoxNodeId, state => state & ~GuiNodeState.Show);
                guiNodeStateService.UpdateState(slot.LabelNodeId, state => state & ~GuiNodeState.Show);
            }
        }

        // Show/hide dividers: divider[i] is between option[i] and option[i+1]
        for (var i = 0; i < _dividerNodes.Count; i++)
        {
            var (wrapper, line) = _dividerNodes[i];

            // Divider i is before option i+1, so show if option i+1 is visible
            if (i + 1 < optionCount)
            {
                guiNodeStateService.UpdateState(wrapper, state => state | GuiNodeState.Show);
                guiNodeStateService.UpdateState(line, state => state | GuiNodeState.Show);
            }
            else
            {
                guiNodeStateService.UpdateState(wrapper, state => state & ~GuiNodeState.Show);
                guiNodeStateService.UpdateState(line, state => state & ~GuiNodeState.Show);
            }
        }

        // Update panel width and push to layout system
        _optionsPanelBox.Width = dropdown.Width;
        _optionsPanelBox.Height = null;

        if (_optionsPanelBox.LayoutBox is { } panelLayoutBox)
        {
            guiLayoutService.SetNodeBox(_optionsPanelNodeId, panelLayoutBox);
        }

        // Show overlay and panel
        guiNodeStateService.UpdateState(_overlayNodeId, state => state | GuiNodeState.Show);
        guiNodeStateService.UpdateState(_optionsPanelNodeId, state => state | GuiNodeState.Show);
        guiLayoutService.SetDirty();
    }

    private void CollapsePanel()
    {
        HidePanel();
    }

    private void HideAllPoolSlots()
    {
        foreach (var slot in _optionSlots)
        {
            guiNodeStateService.UpdateState(slot.BoxNodeId, state => state & ~GuiNodeState.Show);
            guiNodeStateService.UpdateState(slot.LabelNodeId, state => state & ~GuiNodeState.Show);
        }

        foreach (var (wrapper, line) in _dividerNodes)
        {
            guiNodeStateService.UpdateState(wrapper, state => state & ~GuiNodeState.Show);
            guiNodeStateService.UpdateState(line, state => state & ~GuiNodeState.Show);
        }
    }

    private void HidePanel()
    {
        HideAllPoolSlots();
        guiNodeStateService.UpdateState(_optionsPanelNodeId, state => state & ~GuiNodeState.Show);
        guiNodeStateService.UpdateState(_overlayNodeId, state => state & ~GuiNodeState.Show);
    }

    private void UpdateDirtyDropdowns()
    {
        foreach (var (_, dropdown) in _dropdownNodes)
        {
            if (dropdown.IsSelectedIndexDirty)
            {
                UpdateLabel(dropdown);
                dropdown.IsSelectedIndexDirty = false;
            }
        }
    }

    private void UpdateLabel(Dropdown dropdown)
    {
        var text = dropdown.SelectedIndex >= 0 && dropdown.SelectedIndex < dropdown.Options.Length
            ? dropdown.Options[dropdown.SelectedIndex]
            : dropdown.PlaceholderText;

        dropdown.Label.Content = text;
    }
}
