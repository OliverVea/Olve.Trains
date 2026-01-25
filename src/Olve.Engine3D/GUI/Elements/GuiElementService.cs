using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Elements;

public class GuiElementService
{
    private readonly record struct GuiElementRegistration(Id<GuiElementRegistrations> RegistrationId, Id<GuiElement> ElementId);

    private readonly GuiNodeService _guiNodeService;
    private readonly Dictionary<GuiElementRegistration, Id<GuiNode>> _forwardLookup;
    private readonly Dictionary<Id<GuiNode>, GuiElementRegistration> _backwardLookup;
    private readonly Dictionary<Id<GuiNode>, GuiElement> _guiElements = new();
    private readonly (Dictionary<GuiElementRegistration, Id<GuiNode>>, Dictionary<Id<GuiNode>, GuiElementRegistration>) _dictionaries;

    public Event<GuiElementArgs> OnAdded { get; } = new();
    public Event<GuiElementArgs> OnRemoved { get; } = new();

    public GuiElementService(GuiNodeService guiNodeService)
    {
        _guiNodeService = guiNodeService;

        var forwardLookup = new Dictionary<GuiElementRegistration, Id<GuiNode>>();
        var backwardLookup = new  Dictionary<Id<GuiNode>, GuiElementRegistration>();

        _forwardLookup = forwardLookup;
        _backwardLookup = backwardLookup;
        _dictionaries = (forwardLookup, backwardLookup);
    }

    // TODO: Failures might cause memory leaks!
    public Result<Id<GuiElementRegistrations>> RegisterElementAndChildren(UnionId<GuiAnchor, GuiNode> parentId, GuiElement guiElement)
    {
        var registrationId = Id.New<GuiElementRegistrations>();

        if (RegisterElementAndChildren(registrationId, parentId, guiElement)
            .TryPickProblems(out var problems))
        {
            VerifyMapConsistency();
            ResultProblem notice =
                new("NOTICE: This is a serious issue as failures during gui element registration can cause memory leaks")
                {
                    Severity = ProblemSeverities.Critical,
                };
            return problems.Prepend(notice);
        }

        VerifyMapConsistency();

        return registrationId;
    }

    private Result RegisterElementAndChildren(Id<GuiElementRegistrations> registrationId,
        UnionId<GuiAnchor, GuiNode> parentId,
        GuiElement guiElement)
    {
        if (_guiNodeService.AddNode(guiElement.Name, parentId)
            .TryPickProblems(out var problems, out var nodeId))
        {
            return problems;
        }

        GuiElementRegistration elementKey = new(registrationId, guiElement.Id);
        if (!_dictionaries.Add(elementKey, nodeId))
        {
            _guiNodeService.RemoveNode(nodeId);
            return new ResultProblem("Found conflicting GUI element with key {0}", elementKey);
        }

        foreach (var child in guiElement.Children)
        {
            if (RegisterElementAndChildren(registrationId, nodeId, child).TryPickProblems(out problems))
            {
                _guiNodeService.RemoveNode(nodeId);
                if (!_dictionaries.Remove(elementKey))
                {
                    problems = problems.Prepend("Failed removing recently added GUI element with key {0} following error cleanup", elementKey);
                }
                return problems;
            }
        }

        _guiElements[nodeId] = guiElement;
        OnAdded.Invoke(new GuiElementArgs(guiElement.Id, registrationId, nodeId));

        return Result.Success();
    }

    public bool TryGetGuiNodeId(Id<GuiElement> guiElementId,
        Id<GuiElementRegistrations> registrationId,
        out Id<GuiNode> guiNodeId)
    {
        GuiElementRegistration key = new(registrationId, guiElementId);
        return _forwardLookup.TryGetValue(key, out guiNodeId);
    }

    public bool TryGetElementIds(Id<GuiNode> guiNodeId,
        out Id<GuiElement> guiElementId,
        out Id<GuiElementRegistrations> registrationId)
    {
        if (!_backwardLookup.TryGetValue(guiNodeId, out var elementKey))
        {
            guiElementId = default;
            registrationId = default;
            return false;
        }

        guiElementId = elementKey.ElementId;
        registrationId = elementKey.RegistrationId;
        return true;
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void VerifyMapConsistency()
    {
        // Same cardinality and mutual containment for all entries.
        System.Diagnostics.Debug.Assert(_forwardLookup.Count == _backwardLookup.Count, "Bi-map size mismatch");
        foreach (var (k, v) in _forwardLookup)
        {
            System.Diagnostics.Debug.Assert(_backwardLookup.TryGetValue(v, out var backK) && backK.Equals(k),
                "Bi-map mismatch: forward has key/value that backward doesn't mirror");
        }
    }

    public bool TryGetElement(Id<GuiNode> nodeId, [MaybeNullWhen(false)] out GuiElement guiElement) => _guiElements.TryGetValue(nodeId, out guiElement);
}

public static class EventExtensions
{
    public static void Invoke<T1, T2, T3>(this Event<(T1, T2, T3)> e, T1 arg1, T2 arg2, T3 arg3)
    {
        e.Invoke((arg1, arg2, arg3));
    }
}