using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Elements;

public class GuiRadioButtonService(
    ILogger<GuiRadioButtonService> logger,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiNodeStateService guiNodeStateService) : ISceneService
{
    public int Priority => 0;

    public readonly record struct RadioButtonValueChangedMessage(
        Id<RadioButtonGroup> GroupId,
        Id<GuiElement>? OldSelectedId,
        Id<GuiElement> NewSelectedId);

    public Event<RadioButtonValueChangedMessage> OnValueChanged { get; } = new();

    private readonly Dictionary<Id<GuiNode>, RadioButton> _backgroundNodeToRadioButton = new();
    private readonly Dictionary<Id<RadioButtonGroup>, List<RadioButton>> _groupToRadioButtons = new();

    public Result Load()
    {
        guiElementService.OnAdded.Subscribe(OnElementAdded);
        guiElementService.OnRemoved.Subscribe(OnElementRemoved);
        guiActivationService.GuiElementActivated.Subscribe(OnElementActivated);
        return Result.Success();
    }

    public Result Unload()
    {
        guiElementService.OnAdded.Unsubscribe(OnElementAdded);
        guiElementService.OnRemoved.Unsubscribe(OnElementRemoved);
        guiActivationService.GuiElementActivated.Unsubscribe(OnElementActivated);
        return Result.Success();
    }

    public Result Update()
    {
        UpdateDirtyRadioButtons();
        return Result.Success();
    }

    private void OnElementAdded(GuiElementArgs args)
    {
        if (!guiElementService.TryGetElement(args.NodeId, out var element))
        {
            return;
        }

        if (element is not RadioButton radioButton)
        {
            return;
        }

        if (!guiElementService.TryGetGuiNodeId(radioButton.Background.Id, args.RegistrationId, out var bgNodeId))
        {
            logger.LogWarning("Could not resolve background node for radio button {RadioButtonId}", radioButton.Id);
            return;
        }

        _backgroundNodeToRadioButton[bgNodeId] = radioButton;

        if (!_groupToRadioButtons.TryGetValue(radioButton.Group, out var group))
        {
            group = [];
            _groupToRadioButtons[radioButton.Group] = group;
        }

        group.Add(radioButton);
        radioButton.IsSelectedDirty = true;
    }

    private void OnElementRemoved(GuiElementArgs args)
    {
        if (_backgroundNodeToRadioButton.Remove(args.NodeId, out var radioButton)
            && _groupToRadioButtons.TryGetValue(radioButton.Group, out var group))
        {
            group.Remove(radioButton);
            if (group.Count == 0)
            {
                _groupToRadioButtons.Remove(radioButton.Group);
            }
        }
    }

    private void OnElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (!_backgroundNodeToRadioButton.TryGetValue(message.NodeId, out var clickedButton))
        {
            return;
        }

        if (clickedButton.IsSelected)
        {
            return;
        }

        if (!_groupToRadioButtons.TryGetValue(clickedButton.Group, out var group))
        {
            return;
        }

        Id<GuiElement>? previousSelectedId = null;

        foreach (var button in group)
        {
            if (button.IsSelected)
            {
                previousSelectedId = button.Id;
                button.IsSelected = false;
                button.IsSelectedDirty = false;
                UpdateIndicatorVisibility(button);
            }
        }

        clickedButton.IsSelected = true;
        clickedButton.IsSelectedDirty = false;
        UpdateIndicatorVisibility(clickedButton);

        logger.LogDebug("Radio button {RadioButtonId} selected in group {GroupId}", clickedButton.Id, clickedButton.Group);
        OnValueChanged.Invoke(new RadioButtonValueChangedMessage(clickedButton.Group, previousSelectedId, clickedButton.Id));
    }

    private void UpdateDirtyRadioButtons()
    {
        foreach (var (_, radioButton) in _backgroundNodeToRadioButton)
        {
            if (!radioButton.IsSelectedDirty)
            {
                continue;
            }

            UpdateIndicatorVisibility(radioButton);
            radioButton.IsSelectedDirty = false;
        }
    }

    private void UpdateIndicatorVisibility(RadioButton radioButton)
    {
        if (!guiElementService.TryGetAnyGuiNodeId(radioButton.Indicator.Id, out var indicatorNodeId))
        {
            return;
        }

        guiNodeStateService.UpdateState(indicatorNodeId, state =>
            radioButton.IsSelected
                ? state | GuiNodeState.Show
                : state & ~GuiNodeState.Show);
    }
}
