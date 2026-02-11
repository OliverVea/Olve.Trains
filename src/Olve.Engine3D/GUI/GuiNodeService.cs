using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Utilities.CollectionExtensions;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI;

// TODO: consider loop detection on creation.
public class GuiNodeService(ILogger<GuiNodeService> logger) : BaseEntityService<GuiNode>(logger)
{
    public const int MaxAncestorTraversal = 4_000;

    private readonly Dictionary<Id<GuiAnchor>, HashSet<Id<GuiNode>>> _anchorChildren = new();
    private readonly HashSet<Id<GuiNode>> _disabledNodes = [];
    private readonly Dictionary<Id<GuiNode>, UnionId<GuiAnchor, GuiNode>> _parent = [];
    private readonly Dictionary<Id<GuiNode>, List<Id<GuiNode>>> _children = [];

    public Event<Id<GuiNode>> OnEnabled { get; } = new();
    public Event<Id<GuiNode>> OnDisabled { get; } = new();

    public Result<Id<GuiNode>> AddNode(string name, UnionId<GuiAnchor, GuiNode> parentId, bool enabled = true)
    {
        var nodeId = Id.New<GuiNode>();
        GuiNode node = new(nodeId, name);

        var parentIsNode = parentId.TryGetT2(out var parentNodeId, out var parentGuiAnchor);
        if (parentIsNode && !Exists(parentNodeId))
        {
            return new ResultProblem("GUI node with id '{0}' cannot be an ancestor because it does not exist", parentNodeId);
        }

        if (!enabled)
        {
            _disabledNodes.Add(nodeId);
        }

        if (parentIsNode) _children.GetOrAdd(parentNodeId, () => []).Add(nodeId);
        else _anchorChildren.GetOrAdd(parentGuiAnchor, () => []).Add(nodeId);

        _parent[nodeId] = parentId;

        if (Add(node).TryPickProblems(out var problems, out _))
        {
            _disabledNodes.Remove(nodeId);
            _parent.Remove(nodeId);
            if (parentIsNode)  _children[parentNodeId].Remove(nodeId);
            else _anchorChildren[parentGuiAnchor].Remove(nodeId);
            return problems;
        }

        return nodeId;
    }

    public DeletionResult RemoveNode(Id<GuiNode> nodeId)
    {
        if (!Exists(nodeId))
        {
            return DeletionResult.NotFound();
        }

        // Delete children recursively
        if (_children.TryGetValue(nodeId, out var childIds))
        {
            var childIdsCopy = childIds.ToImmutableArray();
            foreach (var childId in childIdsCopy)
            {
                if (RemoveNode(childId).MapToResult(allowNotFound: false).TryPickProblems(out var problems))
                {
                    return DeletionResult.Error(problems);
                }
            }
        }

        _children.Remove(nodeId);

        // Delete the entity
        if (base.Remove(nodeId).MapToResult(allowNotFound: false).TryPickProblems(out var baseProblems))
        {
            return DeletionResult.Error(baseProblems);
        }

        _disabledNodes.Remove(nodeId);

        // Remove hierarchical information
        if (_parent.TryGetValue(nodeId, out var parentId))
        {
            if (parentId.TryGetT2(out var parentNodeId, out _))
            {
                if (_children.TryGetValue(parentNodeId, out var parentChildren))
                {
                    parentChildren.Remove(nodeId);
                    if (parentChildren.Count == 0)
                    {
                        _children.Remove(parentNodeId);
                    }
                }
                else
                {
                    logger.LogWarning("Parent children list missing for '{ParentNodeId}' when removing child '{NodeId}'", parentNodeId, nodeId);
                }
            }
            else
            {
                if (_anchorChildren.TryGetValue(parentId.AsT1(), out var set))
                {
                    set.Remove(nodeId);
                    if (set.Count == 0)
                    {
                        _anchorChildren.Remove(parentId.AsT1());
                    }
                }
            }
        }
        _parent.Remove(nodeId);

        return DeletionResult.Success();
    }

    public bool TryGetChildren(Id<GuiNode> nodeId, [MaybeNullWhen(false)] out IReadOnlyList<Id<GuiNode>> children)
    {
        if (_children.TryGetValue(nodeId, out var mutableChildren))
        {
            children = mutableChildren.AsReadOnly();
            return true;
        }

        if (Exists(nodeId))
        {
            children = [];
            return true;
        }

        children = null;
        return false;
    }

    public bool TryGetParent(Id<GuiNode> nodeId, out UnionId<GuiAnchor, GuiNode> parent)
    {
        return _parent.TryGetValue(nodeId, out parent);
    }

    public Result SetEnabled(Id<GuiNode> nodeId, bool enabled)
    {
        if (!Exists(nodeId))
        {
            return new ResultProblem("Could not find GUI node with id '{0}' while trying to set enabled state to '{1}'",
                nodeId, enabled);
        }

        if (_disabledNodes.Set(nodeId, !enabled))
        {
            (enabled ? OnEnabled : OnDisabled).Invoke(nodeId);
        }

        return Result.Success();
    }

    public Result<bool> IsEnabled(Id<GuiNode> nodeId)
    {
        if (!Exists(nodeId))
        {
            return new ResultProblem("Could not find GUI node with id '{0}' while trying to get enabled state", nodeId);
        }

        return !_disabledNodes.Contains(nodeId);
    }

    public Result<bool> AreNodeAndAncestorsEnabled(Id<GuiNode> nodeId)
    {
        if (GetNodeAndAncestors(nodeId).TryPickProblems(out var problems, out var nodeIds))
        {
            return problems;
        }

        return !nodeIds.Any(_disabledNodes.Contains);
    }

    public IEnumerable<Result<Id<GuiNode>>> GetNodeAndAncestors(Id<GuiNode> nodeId)
    {
        if (!Exists(nodeId))
        {
            yield return new ResultProblem("No node with id '{0}' exists", nodeId);
            yield break;
        }

        HashSet<Id<GuiNode>> visited = [];
        var current = nodeId;

        for (var i = 0; i < MaxAncestorTraversal; i++)
        {
            if (!visited.Add(current))
            {
                yield return new ResultProblem("Cycle detected in GUI hierarchy at '{0}' while traversing ancestors for '{1}'", current, nodeId);
                yield break;
            }

            yield return current;

            var parentResult = GetNodeParent(current);
            if (parentResult.TryPickProblems(out var problems, out var parentId))
            {
                yield return problems;
                yield break;
            }

            if (parentId.TryGetT1(out _, out var parentNode))
            {
                yield break;
            }

            current = parentNode;
        }

        yield return new ResultProblem(
            "Ancestor traversal exceeded limit ({0}) for GUI node '{1}'.",
            MaxAncestorTraversal, nodeId);
    }

    public Result<UnionId<GuiAnchor, GuiNode>> GetNodeParent(Id<GuiNode> nodeId)
    {
        return _parent.TryGetValue(nodeId, out var parentId)
            ? parentId
            : new ResultProblem("Could not find parent for GUI node with id '{0}'", nodeId);
    }

    public IEnumerable<Result<Id<GuiNode>>> GetNodeAndDescendants(Id<GuiNode> nodeId)
    {
        if (!Exists(nodeId))
        {
            yield return new ResultProblem($"No node with id '{nodeId}' exists");
            yield break;
        }

        HashSet<Id<GuiNode>> visited = [];
        var queue = new Queue<Id<GuiNode>>();
        queue.Enqueue(nodeId);

        while (queue.TryDequeue(out var current))
        {
            if (!visited.Add(current))
            {
                yield return new ResultProblem("Cycle detected in GUI hierarchy at '{0}' while traversing descendants for '{1}'", current, nodeId);
                yield break;
            }

            yield return current;

            if (!_children.TryGetValue(current, out var directChildren))
            {
                continue;
            }

            foreach (var child in directChildren)
            {
                queue.Enqueue(child);
            }
        }
    }

    public IEnumerable<Id<GuiNode>> GetRootNodes() => _anchorChildren.Values.SelectMany(s => s);

    public IReadOnlyCollection<Id<GuiNode>> GetChildrenForAnchor(Id<GuiAnchor> anchor) =>
        _anchorChildren.TryGetValue(anchor, out var set) ? set : Array.Empty<Id<GuiNode>>();
}
