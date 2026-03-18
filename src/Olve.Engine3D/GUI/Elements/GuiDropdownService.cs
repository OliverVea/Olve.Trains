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
    GuiNodeStateService guiNodeStateService,
    GuiMouseInputService guiMouseInputService) : ISceneService
{
    public int Priority => 0;

    public readonly record struct DropdownValueChangedMessage(
        Id<GuiElement> DropdownId,
        int OldSelectedIndex,
        int NewSelectedIndex,
        string? OldSelectedText,
        string? NewSelectedText);

    public Event<DropdownValueChangedMessage> OnValueChanged { get; } = new();

    private readonly Dictionary<Id<GuiNode>, Dropdown> _buttonNodeToDropdown = new();
    private readonly Dictionary<Id<GuiNode>, (Dropdown Dropdown, int OptionIndex)> _optionNodeToDropdown = new();
    private readonly HashSet<Dropdown> _expandedDropdowns = new();

    public Result Load()
    {
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

        if (!guiElementService.TryGetGuiNodeId(dropdown.Button.Id, args.RegistrationId, out var buttonNodeId))
        {
            logger.LogWarning("Could not resolve button node for dropdown {DropdownId}", dropdown.Id);
            return;
        }

        _buttonNodeToDropdown[buttonNodeId] = dropdown;

        // Enable button
        guiNodeStateService.UpdateState(buttonNodeId, state => state | GuiNodeState.Show | GuiNodeState.Enabled);

        // Register option box node mappings and enable them
        for (var i = 0; i < dropdown.OptionBoxes.Count; i++)
        {
            if (guiElementService.TryGetGuiNodeId(dropdown.OptionBoxes[i].Id, args.RegistrationId, out var optionNodeId))
            {
                _optionNodeToDropdown[optionNodeId] = (dropdown, i);
                guiNodeStateService.UpdateState(optionNodeId, state => state | GuiNodeState.Show | GuiNodeState.Enabled);
            }
        }

        // Mark state as dirty to initialize
        dropdown.IsSelectedIndexDirty = true;
        dropdown.IsExpandedDirty = true;
    }

    private void OnElementRemoved(GuiElementArgs args)
    {
        if (_buttonNodeToDropdown.Remove(args.NodeId, out var dropdown))
        {
            // Clean up option node mappings
            var optionNodesToRemove = _optionNodeToDropdown
                .Where(kvp => kvp.Value.Dropdown == dropdown)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var optionNode in optionNodesToRemove)
            {
                _optionNodeToDropdown.Remove(optionNode);
            }
        }
    }

    private void OnNodePressed(Id<GuiNode> nodeId)
    {
        // Check if click is outside all expanded dropdowns
        var clickedDropdown = _buttonNodeToDropdown.GetValueOrDefault(nodeId);
        var clickedOption = _optionNodeToDropdown.ContainsKey(nodeId);

        if (clickedDropdown == null && !clickedOption)
        {
            // Clicked outside - collapse all expanded dropdowns
            foreach (var expanded in _expandedDropdowns.ToList())
            {
                expanded.IsExpanded = false;
                _expandedDropdowns.Remove(expanded);
            }
        }
    }

    private void OnElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        // Check if button was clicked
        if (_buttonNodeToDropdown.TryGetValue(message.NodeId, out var dropdown))
        {
            dropdown.IsExpanded = !dropdown.IsExpanded;

            if (dropdown.IsExpanded)
            {
                _expandedDropdowns.Add(dropdown);
            }
            else
            {
                _expandedDropdowns.Remove(dropdown);
            }

            logger.LogDebug("Dropdown {DropdownId} expanded={IsExpanded}", dropdown.Id, dropdown.IsExpanded);
            return;
        }

        // Check if option was clicked
        if (!_optionNodeToDropdown.TryGetValue(message.NodeId, out var optionInfo))
        {
            return;
        }

        var (clickedDropdown, optionIndex) = optionInfo;
        var oldIndex = clickedDropdown.SelectedIndex;
        var oldText = oldIndex >= 0 && oldIndex < clickedDropdown.Options.Length
            ? clickedDropdown.Options[oldIndex]
            : null;

        clickedDropdown.SelectedIndex = optionIndex;
        clickedDropdown.IsExpanded = false;
        _expandedDropdowns.Remove(clickedDropdown);

        var newText = optionIndex >= 0 && optionIndex < clickedDropdown.Options.Length
            ? clickedDropdown.Options[optionIndex]
            : null;

        logger.LogDebug("Dropdown {DropdownId} option {OptionIndex} selected", clickedDropdown.Id, optionIndex);
        OnValueChanged.Invoke(new DropdownValueChangedMessage(
            clickedDropdown.Id,
            oldIndex,
            optionIndex,
            oldText,
            newText));
    }

    private void UpdateDirtyDropdowns()
    {
        foreach (var (_, dropdown) in _buttonNodeToDropdown)
        {
            if (dropdown.IsSelectedIndexDirty)
            {
                UpdateButtonLabel(dropdown);
                dropdown.IsSelectedIndexDirty = false;
            }

            if (dropdown.IsExpandedDirty)
            {
                UpdateOptionsContainerVisibility(dropdown);
                dropdown.IsExpandedDirty = false;
            }
        }
    }

    private void UpdateButtonLabel(Dropdown dropdown)
    {
        var text = dropdown.SelectedIndex >= 0 && dropdown.SelectedIndex < dropdown.Options.Length
            ? dropdown.Options[dropdown.SelectedIndex]
            : dropdown.PlaceholderText;

        dropdown.ButtonLabel.Content = text;
    }

    private void UpdateOptionsContainerVisibility(Dropdown dropdown)
    {
        if (!guiElementService.TryGetAnyGuiNodeId(dropdown.OptionsContainer.Id, out var containerNodeId))
        {
            logger.LogWarning("Could not resolve options container node for dropdown {DropdownId}", dropdown.Id);
            return;
        }

        guiNodeStateService.UpdateState(containerNodeId, state =>
            dropdown.IsExpanded
                ? state | GuiNodeState.Show
                : state & ~GuiNodeState.Show);
    }
}
