using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Layout;

public class GuiLayoutService(
    ILogger<GuiLayoutService> logger,
    GuiNodeService guiNodeService,
    GuiAnchorService guiAnchorService,
    Provider<LayoutContext> layoutContextProvider) : ISceneService
{
    private readonly Dictionary<Id<GuiNode>, int> _nodeIndexById = new();

    private readonly List<LayoutData> _layoutData = [];
    private bool _isDirty = true;
    private static bool _shrinkWarningLogged;

    private LayoutContext LayoutContext => layoutContextProvider.Value;

    private (Id<GuiNode> NodeId, BoxPosition Position)[]? _nodePositions;

    public Result Load()
    {
        // TODO: Move these to DI registration method as event services instead
        guiNodeService.OnNodeAdded.Subscribe(OnAdded);
        guiNodeService.OnNodeRemoved.Subscribe(OnRemoved);
        return Result.Success();
    }

    public Result Unload()
    {
        // TODO: Move these to DI registration method as event services instead
        guiNodeService.OnNodeAdded.Unsubscribe(OnAdded);
        guiNodeService.OnNodeRemoved.Unsubscribe(OnRemoved);
        return Result.Success();
    }

    private void OnAdded(Id<GuiNode> id)
    {
        var index = _layoutData.Count;
        _nodeIndexById.Add(id, index);
        _layoutData.Add(new LayoutData(id));
        SetDirty();
    }

    private void OnRemoved(Id<GuiNode> id)
    {
        if (!_nodeIndexById.Remove(id, out var index))
        {
            logger.LogWarning("GUI node with id '{Id}' and no layout entry was removed", id);
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
        SetDirty();
    }

    public bool TryGetBoxPosition(Id<GuiNode> nodeId, out BoxPosition position)
    {
        position = default;
        if (ComputeLayout().TryPickProblems(out var problems))
        {
            logger.LogError("Failed to compute layout while getting box position for node with id '{NodeId}': {Problems}", nodeId, problems);
            return false;
        }

        if (!_nodeIndexById.TryGetValue(nodeId, out var index))
        {
            return false;
        }

        if (_layoutData[index].Width is not { } dpWidth
            || _layoutData[index].Height is not { } dpHeight
            || _layoutData[index].Position is not { } dpPosition)
        {
            logger.LogWarning("Tried to get position from unpositioned node with id '{NodeId}'", nodeId);
            return false;
        }

        var dpSize = new Vector2D<Dp>(dpWidth, dpHeight);

        position = new BoxPosition(LayoutContext.ToPx(dpPosition), LayoutContext.ToPx(dpSize));

        return true;
    }

    public Result SetNodeBox(Id<GuiNode> nodeId, LayoutBox layoutBox)
    {
        if (!_nodeIndexById.TryGetValue(nodeId, out var index))
        {
            return new ResultProblem("Tried to set box for unregistered node id '{0}'", nodeId);
        }

        if (_layoutData[index].LayoutBox == layoutBox)
        {
            return Result.Success();
        }

        SetDirty();

        _layoutData[index] = _layoutData[index] with
        {
            LayoutBox = layoutBox,
            Width = null,
            Height = null,
            Position = null
        };

        return Result.Success();
    }

    public IReadOnlyList<(Id<GuiNode> NodeId, BoxPosition Position)> GetNodePositions()
    {
        ComputeLayout();
        _nodePositions ??= EnumerateNodePositions()
            .ToArray();
        return _nodePositions;
    }

    private IEnumerable<(Id<GuiNode> NodeId, BoxPosition Position)> EnumerateNodePositions()
    {
        foreach (var layoutData in _layoutData)
        {
            if (layoutData.Width is not { } dpWidth
                || layoutData.Height is not { } dpHeight
                || layoutData.Position is not { } dpPosition)
            {
                logger.LogWarning("Found node without a valid position '{NodeId}'", layoutData.NodeId);
                continue;
            }

            var pxPosition = LayoutContext.ToPx(dpPosition);
            var pxSize = LayoutContext.ToPx(new Vector2D<Dp>(dpWidth, dpHeight));

            yield return (layoutData.NodeId, new BoxPosition(pxPosition, pxSize));
        }
    }

    public Result ComputeLayout()
    {
        if (!_isDirty)
        {
            return Result.Success();
        }

        var rootNodeIds = guiNodeService.GetRootNodes();
        var results = new List<Result>();

        // Partition into screen anchors (no reference) and relative anchors (has reference).
        // Screen anchors are computed first so relative anchors can read their reference node's layout.
        // TODO: This assumes relative anchors only reference nodes from screen-anchored trees.
        //       If relative-to-relative chaining is needed, a topological sort would be required.
        var screenAnchored = new List<(Id<GuiNode> NodeId, GuiAnchor Anchor)>();
        var relativeAnchored = new List<(Id<GuiNode> NodeId, GuiAnchor Anchor)>();

        foreach (var rootNodeId in rootNodeIds)
        {
            if (!guiNodeService.TryGetParent(rootNodeId, out var parentUnion)
                || !parentUnion.TryGetT1(out var anchorId, out _))
            {
                results.Add(new ResultProblem("Root node '{0}' has no anchor parent", rootNodeId));
                continue;
            }

            if (!guiAnchorService.TryGetAnchor(anchorId, out var anchor))
            {
                results.Add(new ResultProblem("Anchor '{0}' not found", anchorId));
                continue;
            }

            if (anchor.ReferenceNode.HasValue)
            {
                relativeAnchored.Add((rootNodeId, anchor));
            }
            else
            {
                screenAnchored.Add((rootNodeId, anchor));
            }
        }

        foreach (var (nodeId, anchor) in screenAnchored)
        {
            results.Add(ComputeLayoutFor(nodeId, anchor));
        }

        foreach (var (nodeId, anchor) in relativeAnchored)
        {
            results.Add(ComputeLayoutFor(nodeId, anchor));
        }

        if (results.TryPickProblems(out var problems))
        {
            return problems;
        }

        _isDirty = false;

        return Result.Success();
    }

    private Result ComputeLayoutFor(Id<GuiNode> rootNode, GuiAnchor anchor)
    {
        if (!_nodeIndexById.TryGetValue(rootNode, out var nodeIndex))
        {
            return new ResultProblem("Could not find node box spec for node with id '{0}'", rootNode);
        }

        // Calculate ghost box bounds based on anchor position and growth direction
        var screenSize = LayoutContext.DesignSize;

        BoxBounds ghostBox;
        if (anchor.ReferenceNode is { } referenceNodeId)
        {
            if (!TryCalculateRelativeGhostBox(anchor, referenceNodeId, screenSize, out var relativeGhostBox))
            {
                logger.LogWarning(
                    "Reference node '{ReferenceNode}' for anchor '{AnchorId}' has no computed layout — skipping",
                    referenceNodeId, anchor.Id);
                return Result.Success();
            }

            ghostBox = relativeGhostBox;
        }
        else
        {
            ghostBox = CalculateGhostBox(anchor, screenSize);
        }

        // Temporarily set ghost box size constraints on root
        ref var root = ref CollectionsMarshal.AsSpan(_layoutData)[nodeIndex];
        var originalBox = root.LayoutBox;

        root = root with
        {
            LayoutBox = originalBox with
            {
                Size = originalBox.Size with
                {
                    PreferredWidth = originalBox.Size.PreferredWidth ?? ghostBox.Width,
                    PreferredHeight = originalBox.Size.PreferredHeight ?? ghostBox.Height
                }
            }
        };

        // Compute sizes with ghost box constraints
        if (!TryComputePreferredDimensionFor(nodeIndex, UIAxis.X, out var problems)
            || !TryComputeActualDimensionFor(nodeIndex, UIAxis.X, out problems)
            || !TryComputePreferredDimensionFor(nodeIndex, UIAxis.Y, out problems)
            || !TryComputeActualDimensionFor(nodeIndex, UIAxis.Y, out problems))
        {
            return problems;
        }

        // Restore original layout box
        root = ref CollectionsMarshal.AsSpan(_layoutData)[nodeIndex];
        root = root with { LayoutBox = originalBox };

        // Calculate position based on ghost box and growth direction
        var elementWidth = root.Width ?? Dp.Zero;
        var elementHeight = root.Height ?? Dp.Zero;

        var xPos = anchor.Growth.Horizontal switch
        {
            HorizontalGrowth.Left => ghostBox.Position.X + ghostBox.Width - elementWidth, // Align right within ghost box
            HorizontalGrowth.Right => ghostBox.Position.X,                                 // Align left within ghost box
            HorizontalGrowth.Both => ghostBox.Position.X + (ghostBox.Width - elementWidth) / 2f, // Center within ghost box
            _ => throw new ArgumentOutOfRangeException()
        };

        var yPos = anchor.Growth.Vertical switch
        {
            VerticalGrowth.Up => ghostBox.Position.Y + ghostBox.Height - elementHeight, // Align bottom within ghost box
            VerticalGrowth.Down => ghostBox.Position.Y,                                  // Align top within ghost box
            VerticalGrowth.Both => ghostBox.Position.Y + (ghostBox.Height - elementHeight) / 2f, // Center within ghost box
            _ => throw new ArgumentOutOfRangeException()
        };

        root = root with { Position = new Vector2D<Dp>(xPos, yPos) };

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

        // Border-box: preferred size IS the outer size (includes padding/border)
        if (preferred.HasValue)
        {
            return preferred.Value;
        }

        // Check if we can derive this dimension from AspectRatio + perpendicular preferred dimension
        var aspectRatio = layoutBox.Size.AspectRatio;
        if (aspectRatio is > 0f)
        {
            var perpendicularPreferred = axis == UIAxis.X
                ? layoutBox.Size.PreferredHeight
                : layoutBox.Size.PreferredWidth;

            // Border-box: perpendicular preferred is outer size, derived size is also outer size
            if (perpendicularPreferred.HasValue)
            {
                var derivedSize = axis == UIAxis.X
                    ? new Dp(perpendicularPreferred.Value.Value * aspectRatio.Value)  // Width = Height * AspectRatio
                    : new Dp(perpendicularPreferred.Value.Value / aspectRatio.Value); // Height = Width / AspectRatio
                return derivedSize;
            }
        }

        // No preferred size - compute from children and add chrome
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

            if (remaining.Value < -Epsilon)
            {
                // Need to shrink - not implemented yet
                if (!_shrinkWarningLogged)
                {
                    _shrinkWarningLogged = true;
                    logger.LogWarning("Layout requires shrinking children but shrink logic is not implemented");
                }
            }
            else if (remaining.Value > Epsilon)
            {
                DistributeRemainingSpaceEqualized(childIndices, axis, remaining);
            }
        }
        else
        {
            // Cross-axis: only stretch if Align is Stretch, otherwise keep computed size
            var shouldStretch = parent.LayoutBox.Align == Align.Stretch;

            foreach (var childIndex in childIndices)
            {
                var child = _layoutData[childIndex];

                if (axis == UIAxis.X)
                {
                    // Skip if child already has a width (computed from children or AspectRatio)
                    if (child.Width.HasValue && !shouldStretch)
                        continue;

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
                        else if (shouldStretch || !child.Width.HasValue)
                        {
                            _layoutData[childIndex] = child with { Width = parentInner };
                        }
                    }
                }
                else
                {
                    // Skip if child already has a height (computed from children or AspectRatio)
                    if (child.Height.HasValue && !shouldStretch)
                        continue;

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
                        else if (shouldStretch || !child.Height.HasValue)
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

        // Calculate content area size
        var contentWidth = self.Width!.Value - self.LayoutBox.HorizontalChrome;
        var contentHeight = self.Height!.Value - self.LayoutBox.VerticalChrome;
        var mainAxisContentSize = axis == UIAxis.X ? contentWidth : contentHeight;
        var crossAxisContentSize = axis == UIAxis.X ? contentHeight : contentWidth;

        // First pass: calculate total children size along main axis for Justify
        var totalChildrenMainSize = Dp.Zero;
        foreach (var childId in childIds)
        {
            if (!_nodeIndexById.TryGetValue(childId, out var ci))
            {
                problem = new ResultProblem("Missing layout data for child {0}", childId);
                return false;
            }

            ref var c = ref CollectionsMarshal.AsSpan(_layoutData)[ci];
            var cSize = c.GetSizeForAxis(axis) ?? Dp.Zero;
            var m = c.LayoutBox.Margin;
            var outerSize = axis == UIAxis.X
                ? m.Left + cSize + m.Right
                : m.Top + cSize + m.Bottom;
            totalChildrenMainSize += outerSize;
        }

        totalChildrenMainSize += GetGapCount(childIds.Count) * gap;

        // Calculate Justify offsets
        var remainingSpace = mainAxisContentSize - totalChildrenMainSize;
        var (mainAxisOffset, extraGap) = self.LayoutBox.Justify switch
        {
            Justify.Center => (remainingSpace / 2f, Dp.Zero),
            Justify.End => (remainingSpace, Dp.Zero),
            Justify.SpaceBetween when childIds.Count > 1 => (Dp.Zero, remainingSpace / (childIds.Count - 1)),
            _ => (Dp.Zero, Dp.Zero), // Start
        };

        // Initialize cursor with Justify offset
        var cursor = axis == UIAxis.X
            ? new Vector2D<Dp>(contentOrigin.X + mainAxisOffset, contentOrigin.Y)
            : new Vector2D<Dp>(contentOrigin.X, contentOrigin.Y + mainAxisOffset);

        // Second pass: position children with Align
        foreach (var childId in childIds)
        {
            var ci = _nodeIndexById[childId];
            ref var child = ref CollectionsMarshal.AsSpan(_layoutData)[ci];
            var cw = child.Width ?? Dp.Zero;
            var ch = child.Height ?? Dp.Zero;

            var margin = child.LayoutBox.Margin;

            // Calculate Align offset for cross axis
            var childCrossSize = axis == UIAxis.X ? ch : cw;
            var crossMarginBefore = axis == UIAxis.X ? margin.Top : margin.Left;
            var crossMarginAfter = axis == UIAxis.X ? margin.Bottom : margin.Right;
            var childOuterCrossSize = crossMarginBefore + childCrossSize + crossMarginAfter;

            var crossAxisOffset = self.LayoutBox.Align switch
            {
                Align.Center => (crossAxisContentSize - childOuterCrossSize) / 2f + crossMarginBefore,
                Align.End => crossAxisContentSize - childOuterCrossSize + crossMarginBefore,
                _ => crossMarginBefore, // Start and Stretch
            };

            var childPos = axis == UIAxis.X
                ? new Vector2D<Dp>(cursor.X + margin.Left, contentOrigin.Y + crossAxisOffset)
                : new Vector2D<Dp>(contentOrigin.X + crossAxisOffset, cursor.Y + margin.Top);

            child = child with { Position = childPos };

            if (!TryComputePositionsFor(ci, out problem))
                return false;

            var outerWidth = margin.Left + cw + margin.Right;
            var outerHeight = margin.Top + ch + margin.Bottom;

            cursor = axis == UIAxis.X
                ? new Vector2D<Dp>(cursor.X + outerWidth + gap + extraGap, cursor.Y)
                : new Vector2D<Dp>(cursor.X, cursor.Y + outerHeight + gap + extraGap);
        }

        return true;
    }

    private static Dp GetDimensionFromChildren(Dp gap, int childCount, Dp childContentSize)
        => GetGapCount(childCount) * gap + childContentSize;

    private static BoxBounds CalculateGhostBox(GuiAnchor anchor, Vector2D<Dp> screenSize)
    {
        var anchorNormalized = anchor.Position.ToNormalized();
        var anchorPosDp = new Vector2D<Dp>(
            anchorNormalized.X * screenSize.X,
            anchorNormalized.Y * screenSize.Y
        );

        // Calculate ghost box position and size based on growth direction
        var (xStart, width) = anchor.Growth.Horizontal switch
        {
            HorizontalGrowth.Left => (Dp.Zero, anchorPosDp.X),                  // [0, anchor.X]
            HorizontalGrowth.Right => (anchorPosDp.X, screenSize.X - anchorPosDp.X), // [anchor.X, screen.X]
            HorizontalGrowth.Both => (Dp.Zero, screenSize.X),                   // [0, screen.X]
            _ => throw new ArgumentOutOfRangeException()
        };

        var (yStart, height) = anchor.Growth.Vertical switch
        {
            VerticalGrowth.Up => (Dp.Zero, anchorPosDp.Y),                      // [0, anchor.Y]
            VerticalGrowth.Down => (anchorPosDp.Y, screenSize.Y - anchorPosDp.Y), // [anchor.Y, screen.Y]
            VerticalGrowth.Both => (Dp.Zero, screenSize.Y),                     // [0, screen.Y]
            _ => throw new ArgumentOutOfRangeException()
        };

        var ghostPosition = new Vector2D<Dp>(xStart, yStart);
        var ghostSize = new Vector2D<Dp>(width, height);

        return new BoxBounds(ghostPosition, ghostSize);
    }

    private bool TryCalculateRelativeGhostBox(
        GuiAnchor anchor,
        Id<GuiNode> referenceNodeId,
        Vector2D<Dp> screenSize,
        out BoxBounds ghostBox)
    {
        ghostBox = default;

        if (!_nodeIndexById.TryGetValue(referenceNodeId, out var refIndex))
        {
            return false;
        }

        var refLayout = _layoutData[refIndex];
        if (refLayout.Position is not { } refPos
            || refLayout.Width is not { } refWidth
            || refLayout.Height is not { } refHeight)
        {
            return false;
        }

        // Resolve anchor point from the reference element's box
        var anchorNormalized = anchor.Position.ToNormalized();
        var anchorPosDp = new Vector2D<Dp>(
            refPos.X + anchorNormalized.X * refWidth,
            refPos.Y + anchorNormalized.Y * refHeight
        );

        // Ghost box extends from anchor point to screen edge (same logic as screen anchors)
        var (xStart, width) = anchor.Growth.Horizontal switch
        {
            HorizontalGrowth.Left => (Dp.Zero, anchorPosDp.X),
            HorizontalGrowth.Right => (anchorPosDp.X, screenSize.X - anchorPosDp.X),
            HorizontalGrowth.Both => (Dp.Zero, screenSize.X),
            _ => throw new ArgumentOutOfRangeException()
        };

        var (yStart, height) = anchor.Growth.Vertical switch
        {
            VerticalGrowth.Up => (Dp.Zero, anchorPosDp.Y),
            VerticalGrowth.Down => (anchorPosDp.Y, screenSize.Y - anchorPosDp.Y),
            VerticalGrowth.Both => (Dp.Zero, screenSize.Y),
            _ => throw new ArgumentOutOfRangeException()
        };

        ghostBox = new BoxBounds(new Vector2D<Dp>(xStart, yStart), new Vector2D<Dp>(width, height));
        return true;
    }

    public void SetDirty()
    {
        _isDirty = true;
        _nodePositions = null;

        // Clear all computed sizes so layout is fully recomputed
        for (var i = 0; i < _layoutData.Count; i++)
        {
            _layoutData[i] = _layoutData[i] with
            {
                Width = null,
                Height = null,
                Position = null
            };
        }
    }

    /// <summary>
    /// Distributes remaining space by equalizing child sizes.
    /// Brings smallest children up to the next size tier, then repeats until all are equal.
    /// </summary>
    private void DistributeRemainingSpaceEqualized(int[] childIndices, UIAxis axis, Dp remaining)
    {
        // Get weighted children with their current sizes
        var weightedChildren = childIndices
            .Where(i => _layoutData[i].LayoutBox.Size.ResizingWeight > Epsilon)
            .ToList();

        if (weightedChildren.Count == 0)
            return;

        while (remaining.Value > Epsilon)
        {
            // Get current sizes of weighted children
            var sizes = weightedChildren
                .Select(i => (Index: i, Size: _layoutData[i].GetSizeForAxis(axis) ?? Dp.Zero))
                .OrderBy(x => x.Size.Value)
                .ToList();

            var minSize = sizes[0].Size;

            // Find all children at minimum size
            var minChildren = sizes.TakeWhile(x => x.Size.Value - minSize.Value < Epsilon).ToList();

            // Find the next size tier (first child not at min size)
            var nextTier = sizes.Skip(minChildren.Count).FirstOrDefault();
            var hasNextTier = nextTier.Index != 0 || sizes.Count > minChildren.Count;

            if (hasNextTier)
            {
                var targetSize = nextTier.Size;
                var gapPerChild = targetSize - minSize;
                var totalCost = minChildren.Count * gapPerChild;

                if (totalCost.Value <= remaining.Value + Epsilon)
                {
                    // We can afford to bring all min children up to the next tier
                    foreach (var (index, _) in minChildren)
                    {
                        _layoutData[index] = _layoutData[index].WithSize(axis, targetSize);
                    }
                    remaining -= totalCost;
                }
                else
                {
                    // Not enough space - distribute remaining equally among min children
                    var delta = remaining / minChildren.Count;
                    foreach (var (index, size) in minChildren)
                    {
                        _layoutData[index] = _layoutData[index].WithSize(axis, size + delta);
                    }
                    remaining = Dp.Zero;
                }
            }
            else
            {
                // All children are at the same size - distribute remaining equally
                var delta = remaining / weightedChildren.Count;
                foreach (var index in weightedChildren)
                {
                    var size = _layoutData[index].GetSizeForAxis(axis) ?? Dp.Zero;
                    _layoutData[index] = _layoutData[index].WithSize(axis, size + delta);
                }
                remaining = Dp.Zero;
            }
        }
    }
}
