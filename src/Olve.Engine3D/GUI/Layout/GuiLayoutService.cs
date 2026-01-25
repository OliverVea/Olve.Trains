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

        if (_layoutData[index].Width is not { } dpWidth
            || _layoutData[index].Height is not { } dpHeight
            || _layoutData[index].Position is not { } dpPosition)
        {
            LoggingManager.Log(LogLevel.Warning, $"Tried to get position from unpositioned node with id '{nodeId}'");
            position = default;
            return false;
        }

        var dpSize = new Vector2D<Dp>(dpWidth, dpHeight);

        position = new BoxPosition(LayoutContext.ToPx(dpPosition), LayoutContext.ToPx(dpSize));

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

    private bool TryComputePreferredDimensionFor(int nodeIndex, UIAxis axis,
        [MaybeNullWhen(true)] out ResultProblem problem)
    {
        problem = null;

        ref var layoutData = ref CollectionsMarshal.AsSpan(_layoutData)[nodeIndex];
        var dimension = layoutData.GetSizeForAxis(axis);
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
                var childDimension = childLayoutData.GetSizeForAxis(axis);
                if (!childDimension.HasValue)
                {
                    problem = new ResultProblem("Child node with id '{0}' does not have a '{1}'", childId,
                        axis == UIAxis.X ? "width" : "height");
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
        layoutData = layoutData.WithSize(axis, nodeDimensions);

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

    private static int GetGapCount(int childCount) => int.Max(0, childCount - 1);

    private bool TryComputeActualDimensionFor(int nodeIndex, UIAxis axis,
        [MaybeNullWhen(true)] out ResultProblem problem)
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

        var parentOuter = parent.GetSizeForAxis(axis) ?? Dp.Zero;
        var parentChrome = parent.LayoutBox.GetChromeForAxis(axis);
        var parentInner = parentOuter - parentChrome;

        if (isMainAxis)
        {
            var gap = parent.LayoutBox.GetGapForAxis(axis);
            var totalGapSize = GetGapCount(childIndices.Length) * gap;
            var totalChildOuter = childIndices
                .Select(x => _layoutData[x])
                .Select(x => (axis == UIAxis.X ? x.Width : x.Height) ?? Dp.Zero).Sum();

            var remaining = parentInner - totalGapSize - totalChildOuter;

            if (float.Abs(remaining.Value) > Epsilon)
            {
                var totalWeight = childIndices.Select(x => _layoutData[x].LayoutBox.Size.ResizingWeight).Sum();

                foreach (var childIndex in childIndices)
                {
                    var weight = _layoutData[childIndex].LayoutBox.Size.ResizingWeight;
                    if (weight < Epsilon)
                    {
                        continue;
                    }

                    var delta = weight / totalWeight * remaining;
                    var size = _layoutData[childIndex].GetSizeForAxis(axis) ?? Dp.Zero;
                    var newSize = size + delta;
                    _layoutData[childIndex] = _layoutData[childIndex].WithSize(axis, newSize);
                }
            }
        }
        else
        {
            foreach (var childIndex in childIndices)
            {
                var child = _layoutData[childIndex];

                if (axis == UIAxis.X)
                {
                    if (!child.LayoutBox.Size.PreferredWidth.HasValue)
                    {
                        var aspectRatio = child.LayoutBox.Size.AspectRatio;
                        if (aspectRatio.HasValue && aspectRatio.Value > 0f && child.Height.HasValue)
                        {
                            var fitMode = child.LayoutBox.Size.FitMode;
                            var computedSize = ComputeSizeWithAspectRatio(
                                aspectRatio.Value,
                                fitMode,
                                parentInner,
                                child.Height.Value);

                            _layoutData[childIndex] = child with
                            {
                                Width = computedSize.Width, Height = computedSize.Height
                            };
                        }
                        else
                        {
                            _layoutData[childIndex] = child with { Width = parentInner };
                        }
                    }
                }
                else
                {
                    if (!child.LayoutBox.Size.PreferredHeight.HasValue)
                    {
                        var aspectRatio = child.LayoutBox.Size.AspectRatio;
                        if (aspectRatio.HasValue && aspectRatio.Value > 0f && child.Width.HasValue)
                        {
                            var fitMode = child.LayoutBox.Size.FitMode;
                            var computedSize = ComputeSizeWithAspectRatio(
                                aspectRatio.Value,
                                fitMode,
                                child.Width.Value,
                                parentInner);

                            _layoutData[childIndex] = child with
                            {
                                Width = computedSize.Width, Height = computedSize.Height
                            };
                        }
                        else
                        {
                            _layoutData[childIndex] = child with { Height = parentInner };
                        }
                    }
                }
            }
        }


        foreach (var childIndex in childIndices)
        {
            ApplyAspectRatioConstraint(childIndex);

            if (!TryComputeActualDimensionFor(childIndex, axis, out problem))
                return false;
        }

        return true;
    }

    private void ApplyAspectRatioConstraint(int nodeIndex)
    {
        ref var layoutData = ref CollectionsMarshal.AsSpan(_layoutData)[nodeIndex];
        var aspectRatio = layoutData.LayoutBox.Size.AspectRatio;

        if (aspectRatio is null or <= 0f)
            return;

        var width = layoutData.Width;
        var height = layoutData.Height;

        if (width.HasValue && height.HasValue)
            return;

        if (width.HasValue && !height.HasValue)
        {
            var computedHeight = new Dp(width.Value.Value / aspectRatio.Value);
            layoutData = layoutData with { Height = computedHeight };
        }
        else if (!width.HasValue && height.HasValue)
        {
            var computedWidth = new Dp(height.Value.Value * aspectRatio.Value);
            layoutData = layoutData with { Width = computedWidth };
        }
    }

    private static (Dp Width, Dp Height) ComputeSizeWithAspectRatio(
        float aspectRatio,
        FitMode fitMode,
        Dp availableWidth,
        Dp availableHeight)
    {
        // aspectRatio = width / height

        switch (fitMode)
        {
            case FitMode.Contain:
            {
                // Fit within both bounds (maintain aspect ratio, may have empty space)
                var widthFromHeight = new Dp(availableHeight.Value * aspectRatio);
                var heightFromWidth = new Dp(availableWidth.Value / aspectRatio);

                if (widthFromHeight.Value <= availableWidth.Value)
                {
                    // Height is the limiting factor, use full height
                    return (widthFromHeight, availableHeight);
                }
                else
                {
                    // Width is the limiting factor, use full width
                    return (availableWidth, heightFromWidth);
                }
            }

            case FitMode.Cover:
            {
                // Fill both bounds (maintain aspect ratio, may crop)
                var widthFromHeight = new Dp(availableHeight.Value * aspectRatio);
                var heightFromWidth = new Dp(availableWidth.Value / aspectRatio);

                if (widthFromHeight.Value >= availableWidth.Value)
                {
                    // Use full height, width exceeds
                    return (widthFromHeight, availableHeight);
                }
                else
                {
                    // Use full width, height exceeds
                    return (availableWidth, heightFromWidth);
                }
            }

            case FitMode.Fill:
            default:
            {
                // Ignore aspect ratio, fill bounds
                return (availableWidth, availableHeight);
            }
        }
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
        var gap = self.LayoutBox.GetGapForAxis(axis);
        var cursor = contentOrigin;

        foreach (var childId in childIds)
        {
            if (!_nodeIndexById.TryGetValue(childId, out var ci))
            {
                problem = new ResultProblem("Missing layout data for child {0}", childId);
                return false;
            }

            ref var child = ref CollectionsMarshal.AsSpan(_layoutData)[ci];
            var cw = child.Width ?? Dp.Zero;
            var ch = child.Height ?? Dp.Zero;

            var margin = child.LayoutBox.Margin;

            var childPos = axis == UIAxis.X
                ? new Vector2D<Dp>(cursor.X + margin.Left, contentOrigin.Y + margin.Top)
                : new Vector2D<Dp>(contentOrigin.X + margin.Left, cursor.Y + margin.Top);


            child = child with { Position = childPos };

            if (!TryComputePositionsFor(ci, out problem))
                return false;

            var outerWidth = margin.Left + cw + margin.Right;
            var outerHeight = margin.Top + ch + margin.Bottom;

            cursor = axis == UIAxis.X
                ? new Vector2D<Dp>(cursor.X + outerWidth + gap, cursor.Y)
                : new Vector2D<Dp>(cursor.X, cursor.Y + outerHeight + gap);
        }

        return true;
    }

    private static Dp GetDimensionFromChildren(Dp gap, int childCount, Dp childContentSize)
        => GetGapCount(childCount) * gap + childContentSize;
}