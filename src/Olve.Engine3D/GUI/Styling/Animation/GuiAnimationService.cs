using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Styling.Animation;

public class GuiAnimationService(
    ILogger<GuiAnimationService> logger,
    EventQueueFactory eventQueueFactory,
    GuiElementService guiElementService,
    GuiStyleRegistry guiStyleRegistry,
    GuiNodeStateService guiNodeStateService) : ISceneService
{

    public readonly record struct GuiStateWeightsChangedMessage(Id<GuiNode> NodeId, StateWeights Weights);

    public Event<GuiStateWeightsChangedMessage> GuiStateWeightsChanged { get; } = new();

    private readonly record struct WeightAnimation(
        Id<GuiNode> NodeId,
        GuiNodeState State,
        bool IsOut,
        TimeSpan Elapsed);

    private readonly EventQueue<GuiNodeStateService.GuiNodeStateChanged> _guiNodeStateChangedQueue =
        eventQueueFactory.Create(guiNodeStateService.OnStateChanged);

    private readonly List<WeightAnimation> _animations = [];

    public Result Load()
    {
        _guiNodeStateChangedQueue
            .SetHandler(OnStateChanged)
            .Init();

        return Result.Success();
    }

    public Result Unload()
    {
        _guiNodeStateChangedQueue.Cleanup();
        _animations.Clear();
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        if (_guiNodeStateChangedQueue
            .Update()
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return UpdateAnimationWeights(deltaTime);
    }

    private Result OnStateChanged(GuiNodeStateService.GuiNodeStateChanged message)
    {
        var added = message.After & ~message.Before;
        var removed = message.Before & ~message.After;

        foreach (var state in GetIndividualFlags(added)) AddAnimation(message.NodeId, state, false);
        foreach (var state in GetIndividualFlags(removed)) AddAnimation(message.NodeId, state, true);

        return Result.Success();
    }

    private void AddAnimation(Id<GuiNode> nodeId, GuiNodeState state, bool isOut)
    {
        _animations.RemoveAll(x => x.NodeId == nodeId && x.State == state && x.IsOut == isOut);
        _animations.Add(new WeightAnimation(nodeId, state, isOut, TimeSpan.Zero));
    }

    private Result UpdateAnimationWeights(TimeSpan elapsedTime)
    {
        for (var i = 0; i < _animations.Count; i++)
        {
            _animations[i] = _animations[i] with
            {
                Elapsed = _animations[i].Elapsed + elapsedTime
            };
        }

        foreach (var (nodeId, animations) in _animations.GroupBy(x => x.NodeId).Unpack())
        {
            StateWeights stateWeights = new();

            if (!guiNodeStateService.TryGetState(nodeId, out var guiNodeState))
            {
                logger.LogWarning("Could not get state for node id '{NodeId}'", nodeId);
                continue;
            }

            foreach (var state in GuiNodeStates.Values)
            {
                var value = guiNodeState.HasFlag(state) ? 1 : 0;
                stateWeights.Set(state, value);
            }

            if (TryGetTransitions(nodeId, out var stateTransitions))
            {
                foreach (var (state, stateAnimations) in animations
                             .GroupBy(x => x.State)
                             .Unpack())
                {
                    var animation = stateAnimations
                        .OrderByDescending(x => x.Elapsed)
                        .First();

                    if (!stateTransitions.TryGetValue(state, out var stateTransition))
                    {
                        continue;
                    }

                    var transition = animation.IsOut ? stateTransition.Out : stateTransition.In;
                    var t = (float)(animation.Elapsed.TotalSeconds / transition.Duration.ToTimeSpan().TotalSeconds);
                    t = float.Clamp(t, 0f, 1f);
                    t = animation.IsOut ? 1 - t : t;

                    stateWeights.Set(state, transition.Easing.CalculateWeight(t));
                }
            }

            GuiStateWeightsChanged.Invoke(new GuiStateWeightsChangedMessage(nodeId, stateWeights));
        }

        _animations.RemoveAll(IsExpired);

        return Result.Success();
    }

    private bool IsExpired(WeightAnimation animation)
    {
        if (!TryGetTransitions(animation.NodeId, out var stateTransitions)
            || !stateTransitions.TryGetValue(animation.State, out var stateTransition))
        {
            return true;
        }

        var transition = animation.IsOut ? stateTransition.Out : stateTransition.In;
        return animation.Elapsed >= transition.Duration.ToTimeSpan();
    }

    private bool TryGetTransitions(Id<GuiNode> nodeId, [MaybeNullWhen(false)] out IReadOnlyDictionary<GuiNodeState, StateTransition> stateTransitions)
    {
        stateTransitions = null;
        if (!guiElementService.TryGetElement(nodeId, out var guiElement))
        {
            logger.LogWarning("Could not get element for node id '{NodeId}'", nodeId);
            return false;
        }

        if (!guiStyleRegistry.TryGetStyle(guiElement, out var style))
        {
            if (guiElement.StyleKey.HasValue)
            {
                logger.LogWarning("Could not get style for node id '{NodeId}' and style key '{StyleKey}'", nodeId, guiElement.StyleKey);
            }
            return false;
        }

        if (style.StateTransitions is null)
        {
            stateTransitions = null;
            return false;
        }

        stateTransitions = style.StateTransitions;
        return true;
    }

    private static IEnumerable<GuiNodeState> GetIndividualFlags(GuiNodeState flags)
    {
        return GuiNodeStates.Values.Where(state =>
            state != GuiNodeState.None
            && state != GuiNodeState.All
            && flags.HasFlag(state));
    }
}
