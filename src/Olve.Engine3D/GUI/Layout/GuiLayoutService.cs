using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Layout;

public class GuiLayoutService(
    ILoggingManager loggingManager,
    GuiNodeService guiNodeService,
    Provider<LayoutContext> layoutContextProvider) : BaseEntityAuxiliaryService<GuiNode>(loggingManager, guiNodeService)
{
    private readonly Dictionary<Id<GuiNode>, int> _nodeIndexById = new();

    private readonly List<LayoutData> _layoutData = [];
    private LayoutContext LayoutContext => layoutContextProvider.Value;

    protected override void OnAdded(Id<GuiNode> id)
    {
        var index = _layoutData.Count;
        _nodeIndexById.Add(id, index);
        _layoutData.Add(new LayoutData(id));
    }

    protected override void OnRemoved(Id<GuiNode> id)
    {
        if (!_nodeIndexById.Remove(id, out var index))
        {
            LoggingManager.Log(LogLevel.Warning, $"GUI node with id '{id}' and no layout entry was removed");
            return;
        }

        var lastIdx = _layoutData.Count - 1;
        if (index != lastIdx)
        {
            var moved = _layoutData[lastIdx];
            _layoutData[index] = moved;
            _nodeIndexById[moved.NodeId] = index;
        }

        _layoutData[lastIdx] = default;
        _layoutData.RemoveAt(lastIdx);
    }

    public bool TryGetBoxPosition(Id<GuiNode> nodeId, out BoxPosition position)
    {
        if (!_nodeIndexById.TryGetValue(nodeId, out var index))
        {
            position = default;
            return false;
        }

        var dpPosition = _layoutData[index].Position;
        var dpWidth = _layoutData[index].Width;
        var dpHeight = _layoutData[index].Height;

        if (!dpWidth.HasValue || !dpHeight.HasValue || !dpPosition.HasValue)
        {
            LoggingManager.Log(LogLevel.Warning, $"Tried to get position from unpositioned node with id '{nodeId}'");
            position = default;
            return false;
        }

        var pxX = new Px((int)float.Round(dpPosition.Value.X.Value * LayoutContext.DpToPx));
        var pxY = new Px((int)float.Round(dpPosition.Value.Y.Value * LayoutContext.DpToPx));
        Vector2D<Px> pxPosition = new(pxX, pxY);

        var pxW = new Px((int)float.Round(dpWidth.Value.Value * LayoutContext.DpToPx));
        var pxH = new Px((int)float.Round(dpHeight.Value.Value * LayoutContext.DpToPx));
        Vector2D<Px> pxSize = new(pxW, pxH);

        position = new BoxPosition(pxPosition, pxSize);
        return true;
    }

    // Consider renaming to CreateOrSetBox and renaming LayoutBox → GuiBox in a future pass.
    public void CreateOrSetNodeBox(Id<GuiNode> nodeId, LayoutBox layoutBox)
    {
        if (!_nodeIndexById.TryGetValue(nodeId, out var index))
        {
            index = _layoutData.Count;
            _nodeIndexById.Add(nodeId, index);
            _layoutData.Add(new LayoutData(nodeId));
        }

        _layoutData[index] = _layoutData[index] with
        {
            LayoutBox = layoutBox,
            Width = null,
            Height = null,
            Position = null
        };
    }

    public Result ComputeLayout()
    {
        var rootNodeIds = guiNodeService.GetRootNodes();
        var results = rootNodeIds.Select(ComputeLayoutFor);
        if (results.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    private Result ComputeLayoutFor(Id<GuiNode> rootNode)
    {
        if (!_nodeIndexById.TryGetValue(rootNode, out var nodeIndex))
        {
            return new ResultProblem("Could not find node box spec for node with id '{0}'", rootNode);
        }

        // TODO: Text / Image sizes

        if (!TryComputePreferredDimensionFor(nodeIndex, UIAxis.X, out var problems)
            || !TryComputeActualDimensionFor(nodeIndex, UIAxis.X, out problems)
            || !TryComputePreferredDimensionFor(nodeIndex, UIAxis.Y, out problems)
            || !TryComputeActualDimensionFor(nodeIndex, UIAxis.Y, out problems))
        {
            return problems;
        }

        ref var root = ref CollectionsMarshal.AsSpan(_layoutData)[nodeIndex];
        root = root with { Position = new Vector2D<Dp>(Dp.Zero, Dp.Zero) };

        if (!TryComputePositionsFor(nodeIndex, out problems))
        {
            return problems;
        }

        return Result.Success();
    }

    private bool TryComputePreferredDimensionFor(int nodeIndex, UIAxis axis, [MaybeNullWhen(true)] out ResultProblem problem)
    {
        problem = null;

        ref var layoutData = ref CollectionsMarshal.AsSpan(_layoutData)[nodeIndex];
        var dimension = axis == UIAxis.X ? layoutData.Width : layoutData.Height;
        if (dimension.HasValue)
        {
            return true;
        }

        var childrenSize = Dp.Zero;
        var childrenCount = 0;

        if (guiNodeService.TryGetChildren(layoutData.NodeId, out var children))
        {
            foreach (var childId in children)
            {
                if (!_nodeIndexById.TryGetValue(childId, out var childIndex))
                {
                    problem = new ResultProblem("Could not find node box spec for node with id '{0}'", childId);
                    return false;
                }

                if (!TryComputePreferredDimensionFor(childIndex, axis, out problem))
                {
                    return false;
                }

                var childLayoutData = _layoutData[childIndex];
                var childDimension = axis == UIAxis.X ? childLayoutData.Width : childLayoutData.Height;
                if (!childDimension.HasValue)
                {
                    problem = new ResultProblem("Child node with id '{0}' does not have a '{1}'", childId, axis == UIAxis.X ? "width" : "height");
                    return false;
                }

                if (layoutData.LayoutBox.LayoutAxis == axis)
                {
                    childrenSize += childDimension.Value;
                }
                else
                {
                    childrenSize = Dp.Max(childrenSize, childDimension.Value);
                }

                childrenCount += 1;
            }
        }

        var nodeDimensions = GetSizeForAxis(layoutData.LayoutBox, childrenCount, childrenSize, axis);

        if (axis == UIAxis.X)
        {
            layoutData = layoutData with { Width = nodeDimensions };
        }
        else
        {
            layoutData = layoutData with { Height = nodeDimensions };
        }

        return true;
    }

    private static Dp GetSizeForAxis(LayoutBox layoutBox, int childCount, Dp childContentSize, UIAxis axis)
    {
        var (preferred, chrome) = GetDimensionsForAxis(layoutBox, axis);
        if (preferred.HasValue)
        {
            return preferred.Value + chrome;
        }

        var gap = layoutBox.GetGapForAxis(axis);
        return GetDimensionFromChildren(gap, childCount, childContentSize) + chrome;
    }

    private static (Dp? Preferred, Dp Chrome) GetDimensionsForAxis(LayoutBox box, UIAxis axis)
    {
        var isHorizontal = axis == UIAxis.X;
        return (isHorizontal ? box.Size.PreferredWidth : box.Size.PreferredHeight,
                isHorizontal ? box.HorizontalChrome : box.VerticalChrome);
    }

    private static int GetGapCount(int childCount) => childCount > 0 ? childCount - 1 : 0;

    private bool TryComputeActualDimensionFor(int nodeIndex, UIAxis axis, [MaybeNullWhen(true)] out ResultProblem problem)
    {
        problem = null;
        ref var parent = ref CollectionsMarshal.AsSpan(_layoutData)[nodeIndex];

        var isMainAxis = parent.LayoutBox.LayoutAxis == axis;
        if (!guiNodeService.TryGetChildren(parent.NodeId, out var childIds) || childIds.Count == 0)
            return true;

        var childIndices = new int[childIds.Count];
        for (var i = 0; i < childIds.Count; i++)
        {
            if (!_nodeIndexById.TryGetValue(childIds[i], out var ci))
            {
                problem = new ResultProblem("Could not find node box spec for node with id '{0}'", childIds[i]);
                return false;
            }
            childIndices[i] = ci;
        }

        if (isMainAxis)
        {
            var totalChildOuter = childIndices
                .Select(x => _layoutData[x])
                .Select(x => (axis == UIAxis.X ? x.Width : x.Height) ?? Dp.Zero).Sum();

            var gap = parent.LayoutBox.GetGapForAxis(axis);
            var gaps = GetGapCount(childIndices.Length) * gap;

            var parentOuter = axis == UIAxis.X
                ? parent.Width ?? Dp.Zero
                : parent.Height ?? Dp.Zero;

            var parentChrome = axis == UIAxis.X
                ? parent.LayoutBox.HorizontalChrome
                : parent.LayoutBox.VerticalChrome;

            var parentInner = parentOuter - parentChrome;

            var targetSum = parentInner - gaps;
            var remaining = targetSum - totalChildOuter;

            if (float.Abs(remaining.Value) > Epsilon)
            {
                float totalGrow = 0f, totalShrink = 0f;
                var sizes = new Dp[childIndices.Length];
                var growW = new float[childIndices.Length];
                var shrinkW = new float[childIndices.Length];

                for (var i = 0; i < childIndices.Length; i++)
                {
                    var c = _layoutData[childIndices[i]];
                    var d = axis == UIAxis.X
                        ? c.Width ?? Dp.Zero
                        : c.Height ?? Dp.Zero;
                    sizes[i] = d;

                    var g = c.LayoutBox.Size.ResizingWeight;
                    growW[i] = g;
                    if (g > 0f) totalGrow += g;

                    var s = c.LayoutBox.Size.ResizingWeight;
                    if (s <= 0f) s = float.Max(0.0001f, d.Value);
                    shrinkW[i] = s;
                    totalShrink += s;
                }

                if (remaining.Value > 0f)
                {
                    if (totalGrow > 0f)
                    {
                        for (var i = 0; i < childIndices.Length; i++)
                        {
                            if (growW[i] <= 0f) continue;
                            var add = new Dp((growW[i] / totalGrow) * remaining.Value);
                            sizes[i] = new Dp(sizes[i].Value + add.Value);
                        }
                    }
                }
                else
                {
                    var deficit = -remaining.Value;
                    if (totalShrink > 0f)
                    {
                        for (var i = 0; i < childIndices.Length; i++)
                        {
                            var sub = new Dp((shrinkW[i] / totalShrink) * deficit);
                            var newSize = sizes[i].Value - sub.Value;
                            if (newSize < 0f) newSize = 0f;
                            sizes[i] = new Dp(newSize);
                        }
                    }
                }

                for (var i = 0; i < childIndices.Length; i++)
                {
                    var c = _layoutData[childIndices[i]];
                    if (axis == UIAxis.X) _layoutData[childIndices[i]] = c with { Width = sizes[i] };
                    else                  _layoutData[childIndices[i]] = c with { Height = sizes[i] };
                }
            }
        }
        else
        {
            var parentOuter = axis == UIAxis.X
                ? parent.Width ?? Dp.Zero
                : parent.Height ?? Dp.Zero;

            var parentChrome = axis == UIAxis.X
                ? parent.LayoutBox.HorizontalChrome
                : parent.LayoutBox.VerticalChrome;

            var parentInner = parentOuter - parentChrome;

            for (var i = 0; i < childIndices.Length; i++)
            {
                var childIndex = childIndices[i];
                var child = _layoutData[childIndex];

                if (axis == UIAxis.X)
                {
                    // Stretch child width to parent width on cross-axis of a vertical stack.
                    // Only do it if child didn't set PreferredWidth.
                    if (!child.LayoutBox.Size.PreferredWidth.HasValue)
                    {
                        _layoutData[childIndex] = child with { Width = parentInner };
                    }
                }
                else
                {
                    // Stretch child height to parent height on cross-axis of a horizontal stack.
                    // Only do it if child didn't set PreferredHeight.
                    if (!child.LayoutBox.Size.PreferredHeight.HasValue)
                    {
                        _layoutData[childIndex] = child with { Height = parentInner };
                    }
                }
            }
        }


        foreach (var childIndex in childIndices)
        {
            if (!TryComputeActualDimensionFor(childIndex, axis, out problem))
                return false;
        }

        return true;
    }

    private bool TryComputePositionsFor(int nodeIndex, [MaybeNullWhen(true)] out ResultProblem problem)
    {
        problem = null;
        ref var self = ref CollectionsMarshal.AsSpan(_layoutData)[nodeIndex];

        var contentOrigin = new Vector2D<Dp>(
            self.Position!.Value.X + (self.LayoutBox.HorizontalChrome / 2f),
            self.Position!.Value.Y + (self.LayoutBox.VerticalChrome / 2f));

        if (!guiNodeService.TryGetChildren(self.NodeId, out var childIds) || childIds.Count == 0)
        {
            return true;
        }

        var axis = self.LayoutBox.LayoutAxis;
        var gap  = self.LayoutBox.GetGapForAxis(axis);
        var cursor = contentOrigin;

        foreach (var childId in childIds)
        {
            if (!_nodeIndexById.TryGetValue(childId, out var ci))
            {
                problem = new ResultProblem("Missing layout data for child {0}", childId);
                return false;
            }

            ref var child = ref CollectionsMarshal.AsSpan(_layoutData)[ci];
            var cw = child.Width  ?? Dp.Zero;
            var ch = child.Height ?? Dp.Zero;

            var childPos = axis == UIAxis.X
                ? new Vector2D<Dp>(cursor.X, contentOrigin.Y)
                : new Vector2D<Dp>(contentOrigin.X, cursor.Y);

            child = child with { Position = childPos };

            if (!TryComputePositionsFor(ci, out problem))
                return false;

            cursor = axis == UIAxis.X
                ? new Vector2D<Dp>(childPos.X + cw + gap, cursor.Y)
                : new Vector2D<Dp>(cursor.X, childPos.Y + ch + gap);
        }

        return true;
    }

    private static Dp GetDimensionFromChildren(Dp gap, int childCount, Dp childContentSize)
        => GetGapCount(childCount) * gap + childContentSize;
}