using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Layout;

public class GuiElementLayoutService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    Provider<LayoutContext> layoutContextProvider) : BaseEntityAuxiliaryService<GuiElement>(loggingManager, guiElementService)
{
    private readonly Dictionary<Id<GuiElement>, int> _guiElementIndexLookup = new();

    private readonly List<LayoutData> _layoutData = [];
    private LayoutContext LayoutContext => layoutContextProvider.Value;

    protected override void OnAdded(Id<GuiElement> id)
    {
        var index = _layoutData.Count;
        _guiElementIndexLookup.Add(id, index);
        _layoutData.Add(new LayoutData(id));
    }

    protected override void OnRemoved(Id<GuiElement> id)
    {
        if (!_guiElementIndexLookup.Remove(id, out var index))
        {
            LoggingManager.Log(LogLevel.Warning, $"GuiElement with id '{id}' and no layout element was removed");
            return;
        }

        var lastIdx = _layoutData.Count - 1;
        if (index != lastIdx)
        {
            var moved = _layoutData[lastIdx];
            _layoutData[index] = moved;
            _guiElementIndexLookup[moved.GuiElementId] = index;
        }

        _layoutData[lastIdx] = default;
        _layoutData.RemoveAt(lastIdx);

    }

    public bool TryGetBoxPosition(Id<GuiElement> guiElementId, out BoxPosition position)
    {
        if (!_guiElementIndexLookup.TryGetValue(guiElementId, out var index))
        {
            position = default;
            return false;
        }

        var dpPosition = _layoutData[index].Position;
        var dpWidth = _layoutData[index].Width;
        var dpHeight = _layoutData[index].Height;

        if (!dpWidth.HasValue || !dpHeight.HasValue || !dpPosition.HasValue)
        {
            LoggingManager.Log(LogLevel.Warning, $"Tried to get position from unpositioned element with id '{guiElementId}'");
            position = default;
            return false;
        }   

        var pxX = new Px((int)float.Round(dpPosition.Value.X.Value * LayoutContext.DpToPx));
        var pxY = new Px((int)float.Round(dpPosition.Value.Y.Value * LayoutContext.DpToPx));
        Vector2D<Px> pxPosition =new(pxX, pxY);
        
        var pxW = new Px((int)float.Round(dpWidth.Value.Value * LayoutContext.DpToPx));
        var pxH = new Px((int)float.Round(dpHeight.Value.Value * LayoutContext.DpToPx));
        Vector2D<Px> pxSize = new(pxW, pxH);
        
        position = new BoxPosition(pxPosition, pxSize);
        return true;
    }


    public void CreateOrSetElementBox(Id<GuiElement> guiElementId, GuiElementBox guiElementBox)
    {
        if (!_guiElementIndexLookup.TryGetValue(guiElementId, out var index))
        {
            index = _layoutData.Count;
            _guiElementIndexLookup.Add(guiElementId, index);
            _layoutData.Add(new LayoutData(guiElementId));
        }

        _layoutData[index] = _layoutData[index] with
        {
            GuiElementBox = guiElementBox,
            Width = null,
            Height = null,
            Position = null
        };
    }

    public Result ComputeLayout()
    {
        var rootElementIds = guiElementService.GetRootElements();
        var results = rootElementIds.Select(ComputeLayoutFor);
        if (results.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    private Result ComputeLayoutFor(Id<GuiElement> rootElement)
    {
        if (!_guiElementIndexLookup.TryGetValue(rootElement, out var guiElementIndex))
        {
            return new ResultProblem("Could not find element box spec for element with id '{0}'", rootElement);
        }
        
        // TODO: Text / Image sizes
        
        if (!TryComputePreferredDimensionFor(guiElementIndex, UIAxis.X, out var problems)
            || !TryComputeActualDimensionFor(guiElementIndex, UIAxis.X, out problems)
            || !TryComputePreferredDimensionFor(guiElementIndex, UIAxis.Y, out problems)
            || !TryComputeActualDimensionFor(guiElementIndex, UIAxis.Y, out problems))
        {
            return problems;
        }
        
        ref var root = ref CollectionsMarshal.AsSpan(_layoutData)[guiElementIndex];
        root = root with { Position = new Vector2D<Dp>(Dp.Zero, Dp.Zero) };

        if (!TryComputePositionsFor(guiElementIndex, out problems))
        {
            return problems;
        }

        return Result.Success();
    }

    private bool TryComputePreferredDimensionFor(int elementIndex, UIAxis axis, [MaybeNullWhen(true)] out ResultProblem problem)
    {
        problem = null;
        
        ref var layoutData = ref CollectionsMarshal.AsSpan(_layoutData)[elementIndex];
        var dimension = axis == UIAxis.X ? layoutData.Width : layoutData.Height;
        if (dimension.HasValue)
        {
            return true;
        }
        
        var childrenSize = Dp.Zero;
        var childrenCount = 0;
        
        if (guiElementService.TryGetChildren(layoutData.GuiElementId, out var children))
        {
            foreach (var childId in children)
            {
                if (!_guiElementIndexLookup.TryGetValue(childId, out var childIndex))
                {
                    problem = new ResultProblem("Could not find element box spec for element with id '{0}'", childId);
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
                    problem = new ResultProblem("Child element with id '{0}' does not have a '{1}'", childId, axis == UIAxis.X ? "width" : "height");
                    return false;
                }

                if (layoutData.GuiElementBox.LayoutAxis == axis)
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

        var guiElementDimensions = GetSizeForAxis(layoutData.GuiElementBox, childrenCount, childrenSize, axis);

        if (axis == UIAxis.X)
        {
            layoutData = layoutData with { Width = guiElementDimensions };
        }
        else
        {
            layoutData = layoutData with { Height = guiElementDimensions };
        }

        return true;
    }

    private static Dp GetSizeForAxis(GuiElementBox guiElementBox, int childCount, Dp childContentSize, UIAxis axis)
    {
        var (preferred, chrome) = GetDimensionsForAxis(guiElementBox, axis);
        if (preferred.HasValue)
        {
            return preferred.Value + chrome;
        }

        var gap = guiElementBox.GetGapForAxis(axis);
        return GetDimensionFromChildren(gap, childCount, childContentSize) + chrome;
    }

    private static (Dp? Preferred, Dp Chrome) GetDimensionsForAxis(GuiElementBox box, UIAxis axis)
    {
        var isHorizontal = axis == UIAxis.X;
        return (isHorizontal ? box.Size.PreferredWidth : box.Size.PreferredHeight, 
                isHorizontal ? box.HorizontalChrome : box.VerticalChrome);
    }

    private static int GetGapCount(int childCount) => childCount > 0 ? childCount - 1 : 0;

    private bool TryComputeActualDimensionFor(int elementIndex, UIAxis axis, [MaybeNullWhen(true)] out ResultProblem problem)
    {
        problem = null;
        ref var parent = ref CollectionsMarshal.AsSpan(_layoutData)[elementIndex];

        var isMainAxis = parent.GuiElementBox.LayoutAxis == axis;
        if (!guiElementService.TryGetChildren(parent.GuiElementId, out var childIds) || childIds.Count == 0)
            return true;

        var childIndices = new int[childIds.Count];
        for (var i = 0; i < childIds.Count; i++)
        {
            if (!_guiElementIndexLookup.TryGetValue(childIds[i], out var ci))
            {
                problem = new ResultProblem("Could not find element box spec for element with id '{0}'", childIds[i]);
                return false;
            }
            childIndices[i] = ci;
        }

        if (isMainAxis)
        {
            var totalChildOuter = childIndices
                .Select(x => _layoutData[x])
                .Select(x => (axis == UIAxis.X ? x.Width : x.Height) ?? Dp.Zero).Sum();

            var gap = parent.GuiElementBox.GetGapForAxis(axis);
            var gaps = GetGapCount(childIndices.Length) * gap;

            var parentOuter = axis == UIAxis.X
                ? parent.Width ?? Dp.Zero
                : parent.Height ?? Dp.Zero;
            
            var parentChrome = axis == UIAxis.X
                ? parent.GuiElementBox.HorizontalChrome
                : parent.GuiElementBox.VerticalChrome;
            
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

                    var g = c.GuiElementBox.Size.ResizingWeight;
                    growW[i] = g;
                    if (g > 0f) totalGrow += g;

                    var s = c.GuiElementBox.Size.ResizingWeight;
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
                ? parent.GuiElementBox.HorizontalChrome
                : parent.GuiElementBox.VerticalChrome;

            var parentInner = parentOuter - parentChrome;

            for (var i = 0; i < childIndices.Length; i++)
            {
                var childIndex = childIndices[i];
                var child = _layoutData[childIndex];
                if (axis == UIAxis.X)
                {
                    if (!child.GuiElementBox.Size.PreferredWidth.HasValue && child.GuiElementBox.Size.ResizingWeight != 0f)
                    {
                        _layoutData[childIndex] = child with { Width = parentInner };
                    }
                }
                else
                {
                    if (!child.GuiElementBox.Size.PreferredHeight.HasValue && child.GuiElementBox.Size.ResizingWeight != 0f)
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
    
    private bool TryComputePositionsFor(int elementIndex, [MaybeNullWhen(true)] out ResultProblem problem)
    {
        problem = null;
        ref var self = ref CollectionsMarshal.AsSpan(_layoutData)[elementIndex];
        var selfW = self.Width  ?? Dp.Zero;
        var selfH = self.Height ?? Dp.Zero;

        var contentOrigin = new Vector2D<Dp>(
            self.Position!.Value.X + (self.GuiElementBox.HorizontalChrome / 2f),
            self.Position!.Value.Y + (self.GuiElementBox.VerticalChrome / 2f));

        if (!guiElementService.TryGetChildren(self.GuiElementId, out var childIds) || childIds.Count == 0)
        {
            return true;
        }

        var axis = self.GuiElementBox.LayoutAxis;
        var gap  = self.GuiElementBox.GetGapForAxis(axis);
        var cursor = contentOrigin;

        foreach (var childId in childIds)
        {
            if (!_guiElementIndexLookup.TryGetValue(childId, out var ci))
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
