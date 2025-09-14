using System.Collections.Immutable;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Logging;
using Olve.Utilities.CollectionExtensions;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.UI.GUI;

public readonly record struct GuiAnchor(Id<GuiAnchor> Id) : IHasId<Id<GuiAnchor>>;
public readonly record struct GuiElement(Id<GuiElement> Id, string Name) : IHasId<Id<GuiElement>>;

public class GuiElementService(ILoggingManager loggingManager) : BaseEntityService<GuiElement>(loggingManager)
{
    private const int MaxParentRecursionSize = 100_000;
    
    private readonly HashSet<Id<GuiElement>> _disabledGuiElements = [];
    private readonly Dictionary<Id<GuiElement>, UnionId<GuiAnchor, GuiElement>> _guiElementParent = [];
    private readonly Dictionary<Id<GuiElement>, List<Id<GuiElement>>>  _guiElementChildren = [];
    
    public Result<Id<GuiElement>> AddGuiElement(string name, UnionId<GuiAnchor, GuiElement> parentId, bool enabled = true)
    {
        var guiElementId = Id.New<GuiElement>();
        GuiElement guiElement = new(guiElementId, name);

        var parentIsGuiElement = parentId.TryGetT2(out var parentGuiElementId, out _);
        if (parentIsGuiElement && !Exists(parentGuiElementId))
        {
            return new ResultProblem("GUI element with id '{0}' cannot be a parent because it does not exist", parentGuiElementId);
        }

        if (!enabled)
        {
            _disabledGuiElements.Add(guiElementId);
        }

        if (Add(guiElement).TryPickProblems(out var problems, out _))
        {
            _disabledGuiElements.Remove(guiElementId);
            return problems;
        }

        if (parentIsGuiElement)
        {
            var parentChildren = _guiElementChildren.GetOrAdd(parentGuiElementId, static () => []);
            parentChildren.Add(guiElementId);
        }
        
        _guiElementParent[guiElementId] = parentId;

        return guiElementId;
    }

    public DeletionResult RemoveGuiElement(Id<GuiElement> guiElementId)
    {
        if (!Exists(guiElementId))
        {
            return DeletionResult.NotFound();
        }
        
        // Delete children recursively
        if (_guiElementChildren.TryGetValue(guiElementId, out var childIds))
        {
            var childIdsCopy = childIds.ToImmutableArray();
            foreach (var childId in childIdsCopy)
            {
                if (RemoveGuiElement(childId).MapToResult(allowNotFound: false).TryPickProblems(out var problems))
                {
                    return DeletionResult.Error(problems);
                }
            }
        }

        _guiElementChildren.Remove(guiElementId);
        
        // Delete the entity
        if (base.Remove(guiElementId).MapToResult(allowNotFound: false).TryPickProblems(out var baseProblems))
        {
            return DeletionResult.Error(baseProblems);
        }

        _disabledGuiElements.Remove(guiElementId);

        // Remove hierarchical information
        if (_guiElementParent.TryGetValue(guiElementId, out var parentId))
        {
            if (parentId.TryGetT2(out var parentGuiElementId, out _))
            {
                if (_guiElementChildren.TryGetValue(parentGuiElementId, out var parentChildren))
                {
                    parentChildren.Remove(guiElementId);
                    if (parentChildren.Count == 0)
                    {
                        _guiElementChildren.Remove(parentGuiElementId);
                    }
                }
                else
                {
                    LoggingManager.Log(LogLevel.Warning, "Could not find parent gui element");
                }
            }
        }
        _guiElementParent.Remove(guiElementId);
        
        return DeletionResult.Success();
    }

    public Result SetEnabled(Id<GuiElement> guiElementId, bool enabled)
    {
        if (!Exists(guiElementId))
        {
            return new ResultProblem("Could not find GUI element with id '{0}' while trying to set enabled state to '{1}'",
                guiElementId, enabled);
        }
        
        _disabledGuiElements.Set(guiElementId, !enabled);

        return Result.Success();
    }

    public Result<bool> IsEnabled(Id<GuiElement> guiElementId)
    {
        if (!Exists(guiElementId))
        {
            return new ResultProblem("Could not find GUI element with id '{0}' while trying to get enabled state", guiElementId);
        }
        
        return !_disabledGuiElements.Contains(guiElementId);
    }

    public Result<bool> TryGetElementAndParentsEnabled(Id<GuiElement> guiElementId)
    {
        if (GetElementAndParents(guiElementId).TryPickProblems(out var problems, out var guiElementIds))
        {
            return problems;
        }

        return !guiElementIds.Any(_disabledGuiElements.Contains);
    }

    public IEnumerable<Result<Id<GuiElement>>> GetElementAndParents(Id<GuiElement> guiElementId)
    {
        HashSet<Id<GuiElement>> visited = [];
        var current = guiElementId;

        for (var i = 0; i < MaxParentRecursionSize; i++)
        {
            if (!visited.Add(current))
            {
                yield return new ResultProblem("Cycle detected in GUI hierarchy at '{0}' while traversing parents for '{1}'", current, guiElementId);
                yield break;
            }

            yield return current;

            var parentResult = GetElementParent(current);
            if (parentResult.TryPickProblems(out var problems, out var parentId))
            {
                yield return problems;
                yield break;
            }

            if (parentId.TryGetT1(out _, out var parentGui))
            {
                yield break;
            }

            current = parentGui;
        }

        yield return new ResultProblem(
            "Parent traversal exceeded limit ({0}) for GUI element '{1}'.",
            MaxParentRecursionSize, guiElementId);
    }

    public Result<UnionId<GuiAnchor, GuiElement>> GetElementParent(Id<GuiElement> guiElementId)
    {
        return _guiElementParent.TryGetValue(guiElementId, out var parentId)
            ? parentId
            : new ResultProblem("Could not find parent for GUI element with id '{0}'", guiElementId);
    }
}


public enum LayoutElementSizeType
{
    Absolute,
    RelativeToParent,
    RelativeToScreen
}

public readonly record struct LayoutElementSize(float Value, LayoutElementSizeType Type);

public readonly record struct LayoutElementDimensions(LayoutElementSize Width, LayoutElementSize Height);

public readonly record struct LayoutElement(
    Id<LayoutElement> Id,
    Id<GuiElement> GuiElementId,
    LayoutElementDimensions Size);

public class GuiElementLayoutService(ILoggingManager loggingManager, GuiElementService guiElementService) : BaseEntityListeningService<GuiElement>(loggingManager, guiElementService)
{
    private readonly Dictionary<Id<GuiElement>, Id<LayoutElement>> _guiToLayoutIdLookup = new();
    
    protected override (bool SubscribeAdd, bool SubscribeDelete) GetSubscriptions() => (false, true);

    protected override Result OnRemoved(Id<GuiElement> entityId)
    {
        return base.OnRemoved(entityId);
    }
}