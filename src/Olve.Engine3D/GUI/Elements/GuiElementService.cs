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

    public bool IsElementNodeId(Id<GuiNode> guiNode, GuiElement element, Id<GuiElementRegistrations> registrationId)
    {
        return TryGetGuiNodeId(element.Id, registrationId, out Id<GuiNode> node) && node == guiNode;
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

    public Result UnregisterElementAndChildren(Id<GuiElementRegistrations> registrationId)
    {
        // Collect all entries for this registration
        var entriesToRemove = _forwardLookup
            .Where(kvp => kvp.Key.RegistrationId == registrationId)
            .ToList();

        if (entriesToRemove.Count == 0)
        {
            return new ResultProblem("No elements found for registration '{0}'", registrationId);
        }

        var nodeIds = new HashSet<Id<GuiNode>>(entriesToRemove.Select(e => e.Value));

        // Find root nodes (parent is an anchor, not another node in this registration)
        var rootNodeIds = nodeIds
            .Where(nodeId =>
                _guiNodeService.TryGetParent(nodeId, out var parent)
                && parent.TryGetT1(out _, out _))
            .ToList();

        // Remove root nodes via GuiNodeService (cascades to children)
        foreach (var rootNodeId in rootNodeIds)
        {
            _guiNodeService.RemoveNode(rootNodeId);
        }

        // Clean up element maps
        foreach (var (key, nodeId) in entriesToRemove)
        {
            _forwardLookup.Remove(key);
            _backwardLookup.Remove(nodeId);
            if (_guiElements.Remove(nodeId, out var element))
            {
                OnRemoved.Invoke(new GuiElementArgs(element.Id, registrationId, nodeId));
            }
        }

        VerifyMapConsistency();

        return Result.Success();
    }

    public bool TryGetElement(Id<GuiNode> nodeId, [MaybeNullWhen(false)] out GuiElement guiElement) => _guiElements.TryGetValue(nodeId, out guiElement);
}