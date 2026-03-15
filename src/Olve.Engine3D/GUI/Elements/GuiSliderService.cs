using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Elements;

public class GuiSliderService(
    ILogger<GuiSliderService> logger,
    GuiElementService guiElementService,
    GuiMouseInputService guiMouseInputService,
    GuiLayoutService guiLayoutService,
    MouseManager mouseManager,
    Provider<LayoutContext> layoutContextProvider) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependents([guiMouseInputService]);

    public readonly record struct SliderValueChangedMessage(
        Id<GuiElement> SliderId,
        float OldValue,
        float NewValue);

    public Event<SliderValueChangedMessage> OnValueChanged { get; } = new();

    private readonly Dictionary<Id<GuiNode>, Slider> _thumbNodeToSlider = new();
    private readonly Dictionary<Id<GuiNode>, Slider> _trackNodeToSlider = new();
    private Id<GuiNode>? _draggingThumbNode;

    public Result Load()
    {
        guiElementService.OnAdded.Subscribe(OnElementAdded);
        guiElementService.OnRemoved.Subscribe(OnElementRemoved);
        guiMouseInputService.OnPressedNode.Subscribe(OnNodePressed);
        guiMouseInputService.OnReleasedNode.Subscribe(OnNodeReleased);
        return Result.Success();
    }

    public Result Unload()
    {
        guiElementService.OnAdded.Unsubscribe(OnElementAdded);
        guiElementService.OnRemoved.Unsubscribe(OnElementRemoved);
        guiMouseInputService.OnPressedNode.Unsubscribe(OnNodePressed);
        guiMouseInputService.OnReleasedNode.Unsubscribe(OnNodeReleased);
        return Result.Success();
    }

    public Result<Pass> Input()
    {
        UpdateDirtySliders();

        if (_draggingThumbNode is not { } thumbNode)
        {
            return Pass.Pass;
        }

        logger.LogDebug("Dragging thumb node {ThumbNode}", thumbNode);

        if (!_thumbNodeToSlider.TryGetValue(thumbNode, out var slider))
        {
            logger.LogWarning("Dragging thumb node {ThumbNode} not found in slider lookup", thumbNode);
            _draggingThumbNode = null;
            return Pass.Pass;
        }

        if (!guiElementService.TryGetElementIds(thumbNode, out _, out var registrationId))
        {
            logger.LogWarning("Could not resolve element IDs for thumb node {ThumbNode}", thumbNode);
            _draggingThumbNode = null;
            return Pass.Pass;
        }

        if (!guiElementService.TryGetGuiNodeId(slider.Track.Id, registrationId, out var trackNodeId))
        {
            logger.LogWarning("Could not resolve track node for slider {SliderId}", slider.Id);
            _draggingThumbNode = null;
            return Pass.Pass;
        }

        if (!guiLayoutService.TryGetBoxPosition(trackNodeId, out var trackBox))
        {
            logger.LogWarning("Could not get track box position for node {TrackNode}", trackNodeId);
            return Pass.Pass;
        }

        var mouseX = new Px((int)mouseManager.State.Position.X);
        var trackLeft = trackBox.Position.X;
        var trackWidth = trackBox.Size.X;
        var thumbWidthPx = layoutContextProvider.Value.ToPx(new Dp(slider.ThumbWidth));

        var usableWidth = trackWidth - thumbWidthPx;
        if (usableWidth.Value <= 0)
        {
            logger.LogWarning("Track usable width is zero or negative for slider {SliderId}", slider.Id);
            return Pass.Pass;
        }

        var relativeX = mouseX - trackLeft - new Px(thumbWidthPx.Value / 2);
        var t = System.Math.Clamp((float)relativeX.Value / usableWidth.Value, 0f, 1f);

        var newValue = slider.MinValue + t * (slider.MaxValue - slider.MinValue);
        SetSliderValue(slider, thumbNode, newValue);

        logger.LogDebug("Slider {SliderId} dragged to value {Value:F3}", slider.Id, newValue);

        return Pass.Pass;
    }

    private void OnElementAdded(GuiElementArgs args)
    {
        if (!guiElementService.TryGetElement(args.NodeId, out var element))
        {
            logger.LogWarning("Could not get element for node {NodeId} on add", args.NodeId);
            return;
        }

        if (element is not Slider slider)
        {
            return;
        }

        if (!guiElementService.TryGetGuiNodeId(slider.Thumb.Id, args.RegistrationId, out var thumbNodeId)
            || !guiElementService.TryGetGuiNodeId(slider.Track.Id, args.RegistrationId, out var trackNodeId))
        {
            logger.LogWarning("Could not resolve thumb or track node for slider {SliderId}", slider.Id);
            return;
        }

        _thumbNodeToSlider[thumbNodeId] = slider;
        _trackNodeToSlider[trackNodeId] = slider;

        // Mark dirty so UpdateDirtySliders retries on subsequent frames
        // if layout isn't ready yet (UpdateThumbPosition returns early when
        // track width is zero).
        slider.IsValueDirty = true;
    }

    private void OnElementRemoved(GuiElementArgs args)
    {
        if (!guiElementService.TryGetElement(args.NodeId, out var element) || element is not Slider slider)
        {
            _thumbNodeToSlider.Remove(args.NodeId);
            _trackNodeToSlider.Remove(args.NodeId);
            return;
        }

        if (guiElementService.TryGetGuiNodeId(slider.Thumb.Id, args.RegistrationId, out var thumbNodeId))
        {
            _thumbNodeToSlider.Remove(thumbNodeId);
            if (_draggingThumbNode == thumbNodeId)
            {
                _draggingThumbNode = null;
            }
        }

        if (guiElementService.TryGetGuiNodeId(slider.Track.Id, args.RegistrationId, out var trackNodeId))
        {
            _trackNodeToSlider.Remove(trackNodeId);
        }
    }

    private void OnNodePressed(Id<GuiNode> nodeId)
    {
        if (_thumbNodeToSlider.ContainsKey(nodeId))
        {
            _draggingThumbNode = nodeId;
            logger.LogDebug("Started dragging slider thumb {NodeId}", nodeId);
        }
    }

    private void OnNodeReleased(Id<GuiNode> nodeId)
    {
        if (_draggingThumbNode == nodeId)
        {
            _draggingThumbNode = null;
            logger.LogDebug("Released slider thumb {NodeId}", nodeId);
        }
    }

    private void UpdateDirtySliders()
    {
        foreach (var (thumbNodeId, slider) in _thumbNodeToSlider)
        {
            if (!slider.IsValueDirty)
            {
                continue;
            }

            if (!guiElementService.TryGetElementIds(thumbNodeId, out _, out var registrationId))
            {
                continue;
            }

            if (UpdateThumbPosition(slider, registrationId))
            {
                slider.IsValueDirty = false;
            }
        }
    }

    private void SetSliderValue(Slider slider, Id<GuiNode> thumbNodeId, float newValue)
    {
        var oldValue = slider.Value;
        slider.Value = newValue;
        slider.IsValueDirty = false;

        if (System.Math.Abs(slider.Value - oldValue) < float.Epsilon)
        {
            return;
        }

        if (!guiElementService.TryGetElementIds(thumbNodeId, out _, out var registrationId))
        {
            return;
        }

        UpdateThumbPosition(slider, registrationId);

        OnValueChanged.Invoke(new SliderValueChangedMessage(slider.Id, oldValue, slider.Value));
    }

    /// <returns>true if the thumb position was successfully set, false if layout wasn't ready.</returns>
    private bool UpdateThumbPosition(Slider slider, Id<GuiElementRegistrations> registrationId)
    {
        if (!guiElementService.TryGetGuiNodeId(slider.Thumb.Id, registrationId, out var thumbNodeId))
        {
            return false;
        }

        var range = slider.MaxValue - slider.MinValue;
        var t = range > 0f ? (slider.Value - slider.MinValue) / range : 0f;

        // We need the track's computed width to calculate the margin.
        if (!guiElementService.TryGetGuiNodeId(slider.Track.Id, registrationId, out var trackNodeId))
        {
            return false;
        }

        if (!guiLayoutService.TryGetBoxPosition(trackNodeId, out var trackBox))
        {
            return false;
        }

        var trackWidthPx = trackBox.Size.X;
        var thumbWidthPx = layoutContextProvider.Value.ToPx(new Dp(slider.ThumbWidth));
        var usableWidthPx = trackWidthPx - thumbWidthPx;

        if (usableWidthPx.Value <= 0)
        {
            return false;
        }

        var marginLeftPx = new Px((int)(t * usableWidthPx.Value));
        var marginLeftDp = layoutContextProvider.Value.ToDp(marginLeftPx);

        slider.Thumb.MarginLeft = marginLeftDp.Value;

        var thumbLayout = slider.Thumb.LayoutBox;
        if (thumbLayout is null)
        {
            return false;
        }

        var updatedLayout = thumbLayout.Value with
        {
            Margin = thumbLayout.Value.Margin with
            {
                Left = marginLeftDp,
            },
        };

        guiLayoutService.SetNodeBox(thumbNodeId, updatedLayout);
        return true;
    }
}
