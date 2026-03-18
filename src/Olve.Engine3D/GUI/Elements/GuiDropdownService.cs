using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Elements;

public class GuiDropdownService(
    ILogger<GuiDropdownService> logger,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
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

    private readonly Dictionary<Id<GuiNode>, Dropdown> _dropdownNodes = new();
    private Dropdown? _expandedDropdown;

    private Id<GuiAnchor> _overlayAnchorId;
    private Id<GuiElementRegistrations> _overlayRegistrationId;
    private Id<GuiNode> _overlayNodeId;

    private Id<GuiAnchor> _optionsPanelAnchorId;
    private Id<GuiElementRegistrations> _optionsPanelRegistrationId;
    private Id<GuiNode> _optionsPanelNodeId;
    private Box _optionsPanelBox = null!;
    private Box _overlayBox = null!;

    private bool _panelRegistered;

    public Result Load()
    {
        // Create anchors up front (lightweight, no visible elements yet)

        // Fullscreen overlay anchor (depth 0, behind the options panel)
        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight, depth: 0)
            .TryPickProblems(out var problems, out _overlayAnchorId))
        {
            return problems.Prepend("Failed to register overlay anchor");
        }

        // Options panel anchor (depth 1, above the overlay)
        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight, depth: 1)
            .TryPickProblems(out problems, out _optionsPanelAnchorId))
        {
            return problems.Prepend("Failed to register options panel anchor");
        }

        // Prepare element definitions but don't register them yet
        _overlayBox = new Box
        {
            Id = Id.New<GuiElement>(),
            Name = "Dropdown/Overlay",
            Interactive = true,
            InheritParentState = false,
            Weight = 1f,
            BackgroundColor = new RGBA(0f, 0f, 0f, 0f),
        };

        _optionsPanelBox = new Box
        {
            Id = Id.New<GuiElement>(),
            Name = "Dropdown/SharedOptionsPanel",
            Interactive = true,
            InheritParentState = false,
            Width = 200,
            Height = 200,
            Weight = 0f,
            BackgroundColor = new RGBA(0.25f, 0.25f, 0.25f, 1f),
            Vertical = true,
            Justify = Justify.Start,
            Align = Align.Stretch,
            Children = [],
        };

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

        if (nodeId == _optionsPanelNodeId)
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
        if (!_dropdownNodes.TryGetValue(message.NodeId, out var dropdown))
        {
            return;
        }

        var expanding = _expandedDropdown != dropdown;

        if (_expandedDropdown != null && _expandedDropdown != dropdown)
        {
            _expandedDropdown = null;
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

        // Register elements if not already registered
        if (!_panelRegistered)
        {
            if (guiElementService.RegisterElementAndChildren(_overlayAnchorId, _overlayBox)
                .TryPickProblems(out problems, out _overlayRegistrationId))
            {
                logger.LogWarning("Failed to register overlay element: {Problems}", problems);
                return;
            }

            if (!guiElementService.TryGetGuiNodeId(_overlayBox.Id, _overlayRegistrationId, out _overlayNodeId))
            {
                logger.LogWarning("Failed to resolve overlay node ID");
                return;
            }

            if (guiElementService.RegisterElementAndChildren(_optionsPanelAnchorId, _optionsPanelBox)
                .TryPickProblems(out problems, out _optionsPanelRegistrationId))
            {
                logger.LogWarning("Failed to register options panel element: {Problems}", problems);
                return;
            }

            if (!guiElementService.TryGetGuiNodeId(_optionsPanelBox.Id, _optionsPanelRegistrationId, out _optionsPanelNodeId))
            {
                logger.LogWarning("Failed to resolve options panel node ID");
                return;
            }

            _panelRegistered = true;
        }

        guiLayoutService.SetDirty();
    }

    private void CollapsePanel()
    {
        if (!_panelRegistered)
        {
            return;
        }

        guiElementService.UnregisterElementAndChildren(_optionsPanelRegistrationId);
        guiElementService.UnregisterElementAndChildren(_overlayRegistrationId);
        _panelRegistered = false;
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
