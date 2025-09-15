using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Utilities.Assertions;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Layout;

public class GuiElementLayoutService(ILoggingManager loggingManager, GuiElementService guiElementService) : BaseEntityAuxiliaryService<GuiElement>(loggingManager, guiElementService)
{
    private readonly Dictionary<Id<GuiElement>, int> _guiElementIndexLookup = new();
    private readonly List<LayoutData> _layoutData = [];

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
        return Result.Concat(results);
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

        // TODO: Positions

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

                if (layoutData.GuiElementBox.LayoutAxis == UIAxis.X)
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

        var childIndices = childIds.Count <= 64 ? stackalloc int[childIds.Count] : new int[childIds.Count];
        for (var i = 0; i < childIds.Count; i++)
        {
            if (!_guiElementIndexLookup.TryGetValue(childIds[i], out var ci))
            {
                problem = new ResultProblem("Could not find element box spec for element with id '{0}'", childIds[i]);
                return false;
            }
            childIndices[i] = ci;
        }

        var span = CollectionsMarshal.AsSpan(_layoutData);

        if (isMainAxis)
        {
            var totalChildOuter = Dp.Zero;
            for (var i = 0; i < childIndices.Length; i++)
            {
                ref readonly var c = ref span[childIndices[i]];
                var d = axis == UIAxis.X ? c.Width : c.Height;
                Assert.That(() => d.HasValue, "Child dimension should be known at this point");
                totalChildOuter += d ?? Dp.Zero;
            }

            var gap = parent.GuiElementBox.GetGapForAxis(axis);
            var gaps = GetGapCount(childIndices.Length) * gap;

            var parentOuter = axis == UIAxis.X ? (parent.Width ?? Dp.Zero) : (parent.Height ?? Dp.Zero);
            var parentChrome = axis == UIAxis.X ? parent.GuiElementBox.HorizontalChrome : parent.GuiElementBox.VerticalChrome;
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
                    ref readonly var c = ref span[childIndices[i]];
                    var d = axis == UIAxis.X ? (c.Width ?? Dp.Zero) : (c.Height ?? Dp.Zero);
                    sizes[i] = d;

                    var g = c.GuiElementBox.Size.ResizingWeightWeight;
                    growW[i] = g;
                    if (g > 0f) totalGrow += g;

                    var s = c.GuiElementBox.Size.ResizingWeightWeight;
                    if (s <= 0f) s = float.Max(0.0001f, d.Value);
                    shrinkW[i] = s;
                    totalShrink += s;
                }

                if (float.Abs(remaining.Value) > Epsilon)
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
                    ref var c = ref span[childIndices[i]];
                    if (axis == UIAxis.X) c = c with { Width = sizes[i] };
                    else                  c = c with { Height = sizes[i] };
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



    private static Dp GetDimensionFromChildren(Dp gap, int childCount, Dp childContentSize)
        => GetGapCount(childCount) * gap + childContentSize;
}