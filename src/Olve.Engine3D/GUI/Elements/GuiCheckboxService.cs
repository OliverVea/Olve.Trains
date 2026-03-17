using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Elements;

public class GuiCheckboxService(
    ILogger<GuiCheckboxService> logger,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiNodeStateService guiNodeStateService) : ISceneService
{
    public int Priority => 0;

    public readonly record struct CheckboxValueChangedMessage(
        Id<GuiElement> CheckboxId,
        bool IsChecked);

    public Event<CheckboxValueChangedMessage> OnValueChanged { get; } = new();

    private readonly Dictionary<Id<GuiNode>, Checkbox> _backgroundNodeToCheckbox = new();
    private readonly Dictionary<Id<GuiNode>, Checkbox> _indicatorNodeToCheckbox = new();

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
        UpdateDirtyCheckboxes();
        return Result.Success();
    }

    private void OnElementAdded(GuiElementArgs args)
    {
        if (!guiElementService.TryGetElement(args.NodeId, out var element))
        {
            return;
        }

        if (element is not Checkbox checkbox)
        {
            return;
        }

        if (!guiElementService.TryGetGuiNodeId(checkbox.Background.Id, args.RegistrationId, out var bgNodeId)
            || !guiElementService.TryGetGuiNodeId(checkbox.Indicator.Id, args.RegistrationId, out var indicatorNodeId))
        {
            logger.LogWarning("Could not resolve background or indicator node for checkbox {CheckboxId}", checkbox.Id);
            return;
        }

        _backgroundNodeToCheckbox[bgNodeId] = checkbox;
        _indicatorNodeToCheckbox[indicatorNodeId] = checkbox;

        checkbox.IsCheckedDirty = true;
    }

    private void OnElementRemoved(GuiElementArgs args)
    {
        _backgroundNodeToCheckbox.Remove(args.NodeId);
        _indicatorNodeToCheckbox.Remove(args.NodeId);
    }

    private void OnElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (!_backgroundNodeToCheckbox.TryGetValue(message.NodeId, out var checkbox))
        {
            return;
        }

        checkbox.IsChecked = !checkbox.IsChecked;
        checkbox.IsCheckedDirty = false;

        UpdateIndicatorVisibility(checkbox);

        logger.LogDebug("Checkbox {CheckboxId} toggled to {IsChecked}", checkbox.Id, checkbox.IsChecked);
        OnValueChanged.Invoke(new CheckboxValueChangedMessage(checkbox.Id, checkbox.IsChecked));
    }

    private void UpdateDirtyCheckboxes()
    {
        foreach (var (_, checkbox) in _backgroundNodeToCheckbox)
        {
            if (!checkbox.IsCheckedDirty)
            {
                continue;
            }

            UpdateIndicatorVisibility(checkbox);
            checkbox.IsCheckedDirty = false;
        }
    }

    private void UpdateIndicatorVisibility(Checkbox checkbox)
    {
        if (!guiElementService.TryGetAnyGuiNodeId(checkbox.Indicator.Id, out var indicatorNodeId))
        {
            return;
        }

        guiNodeStateService.UpdateState(indicatorNodeId, state =>
            checkbox.IsChecked
                ? state | GuiNodeState.Show
                : state & ~GuiNodeState.Show);
    }
}
