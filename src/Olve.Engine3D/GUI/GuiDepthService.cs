using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI;

public class GuiDepthService(GuiNodeService nodeService)
{
    private readonly Dictionary<Id<GuiNode>, int> _depths = [];

    /// <summary>
    /// Returns the depth for this node. Depth is 0 if the node's direct parent is an anchor,
    /// otherwise parentDepth + 1.
    /// Also populates (and fixes) cached depths for this node and its descendants.
    /// </summary>
    public Result<int> GetDepth(Id<GuiNode> nodeId)
    {
        // Fast path: already cached
        if (_depths.TryGetValue(nodeId, out var cached))
        {
            return cached;
        }

        // Compute depth for this node (and cache it)
        var singleDepthResult = ComputeDepthForSingleNode(nodeId);
        if (singleDepthResult.TryPickProblems(out var problems, out var depth))
        {
            return problems;
        }

        // After we know this node's depth, BFS its subtree so its children get cached too.
        var assignResult = AssignDepthsBreadthFirst(nodeId, depth);
        if (assignResult.TryPickProblems(out problems))
        {
            return problems;
        }

        return depth;
    }

    /// <summary>
    /// Compute the depth for nodeId by walking ancestors until:
    /// - we find a cached depth, OR
    /// - we find an anchor parent (root => depth 0).
    /// Then assign depths top-down along that chain into _depths.
    /// </summary>
    private Result<int> ComputeDepthForSingleNode(Id<GuiNode> nodeId)
    {
        // If cached, done.
        if (_depths.TryGetValue(nodeId, out var existing))
        {
            return existing;
        }

        // Walk up to build the chain of nodes we need to assign.
        // upChain[0] = nodeId, upChain[1] = its parent node, etc.
        List<Id<GuiNode>> upChain = [];
        HashSet<Id<GuiNode>> visited = [];

        var current = nodeId;

        int? knownDepth = null; // the depth of the last element we stop at

        for (var steps = 0; steps < GuiNodeService.MaxAncestorTraversal; steps++)
        {
            if (!visited.Add(current))
            {
                return new ResultProblem(
                    "Cycle detected in GUI hierarchy while computing depth for '{0}'",
                    nodeId
                );
            }

            upChain.Add(current);

            // Do we already know this one's depth?
            if (_depths.TryGetValue(current, out var d))
            {
                knownDepth = d;
                break;
            }

            // Otherwise, look at its parent.
            var parentResult = nodeService.GetNodeParent(current);
            if (parentResult.TryPickProblems(out var pProblems, out var parentId))
            {
                return pProblems;
            }

            // Parent is an anchor => current is depth 0 and we stop climbing.
            if (parentId.TryGetT1(out _, out var parentNodeId))
            {
                knownDepth = 0;
                break;
            }

            // Parent is another node => keep climbing.
            current = parentNodeId;
        }

        if (knownDepth is null)
        {
            // We bailed because of MaxAncestorTraversal maybe.
            return new ResultProblem(
                "Ancestor traversal exceeded limit ({0}) while computing depth for GUI node '{1}'.",
                GuiNodeService.MaxAncestorTraversal,
                nodeId
            );
        }

        // Now walk DOWN the chain we collected upward, but in reverse order:
        // The last item in upChain is the ancestor with knownDepth.
        // The item before that is its child, which should get knownDepth+1, etc.
        //
        // Example:
        //   upChain = [C, B, A] where A is closest to root / knownDepth,
        //   knownDepth = depth(A).
        // We want:
        //   A = knownDepth
        //   B = knownDepth+1
        //   C = knownDepth+2
        //
        // So iterate reverse: A,B,C.
        var depthVal = knownDepth.Value;
        for (int i = upChain.Count - 1; i >= 0; i--)
        {
            var id = upChain[i];
            _depths[id] = depthVal;
            depthVal++;
        }

        // At this point nodeId must be cached.
        return _depths[nodeId];
    }

    /// <summary>
    /// After we know startNode's depth, walk its subtree and cache depth = parentDepth+1, etc.
    /// </summary>
    private Result AssignDepthsBreadthFirst(Id<GuiNode> startNode, int knownDepth)
    {
        Queue<(Id<GuiNode> node, int depth)> queue = new();
        HashSet<Id<GuiNode>> visited = [];

        queue.Enqueue((startNode, knownDepth));

        while (queue.TryDequeue(out var pair))
        {
            var (node, depth) = pair;

            if (!visited.Add(node))
            {
                return new ResultProblem(
                    "Cycle detected in GUI hierarchy at '{0}' while assigning depths",
                    node
                );
            }

            _depths[node] = depth;

            if (!nodeService.TryGetChildren(node, out var children))
            {
                // If nodeService says the node doesn't exist, just skip.
                continue;
            }

            foreach (var childId in children)
            {
                queue.Enqueue((childId, depth + 1));
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Optional: call this before removing a node from GuiNodeService so we clear cache for that subtree.
    /// </summary>
    public void OnNodeRemoved(Id<GuiNode> rootBeingRemoved)
    {
        var descendants = nodeService.GetNodeAndDescendants(rootBeingRemoved);
        foreach (var res in descendants)
        {
            if (res.TryPickProblems(out _, out var id)) continue;// or whatever helper you use to get the value
            _depths.Remove(id);
        }
    }
}
