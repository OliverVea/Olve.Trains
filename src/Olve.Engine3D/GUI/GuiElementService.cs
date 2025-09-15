using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Logging;
using Olve.Utilities.CollectionExtensions;
using Olve.Utilities.Collections;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI;

// TODO: consider loop detection on creation.
public class GuiElementService(ILoggingManager loggingManager) : BaseEntityService<GuiElement>(loggingManager)
{
    private const int MaxParentRecursionSize = 100_000;
    
    private readonly OneToManyLookup<Id<GuiAnchor>, Id<GuiElement>> _anchorChildren = [];
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
        else
        {
            _anchorChildren.Set(parentId.AsT1(), guiElementId, true);
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
                    LoggingManager.Log(LogLevel.Warning, $"Parent children list missing for '{parentGuiElementId}' when removing child '{guiElementId}'");
                }
            }
            else
            {
                _anchorChildren.Set(parentId.AsT1(), guiElementId, false);
            }
        }
        _guiElementParent.Remove(guiElementId);
        
        return DeletionResult.Success();
    }

    public bool TryGetChildren(Id<GuiElement> guiElementId, [MaybeNullWhen(false)] out IReadOnlyList<Id<GuiElement>> children)
    {
        if (_guiElementChildren.TryGetValue(guiElementId, out var mutableChildren))
        {
            children = mutableChildren.AsReadOnly();
            return true;
        }

        if (Exists(guiElementId))
        {
            children = [];
            return true;
        }

        children = null;
        return false;
    }

    public bool TryGetParent(Id<GuiElement> guiElementId, out UnionId<GuiAnchor, GuiElement> parent)
    {
        return  _guiElementParent.TryGetValue(guiElementId, out parent);
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
        if (!Exists(guiElementId))
        {
            yield return new ResultProblem("No element with id '{0}' exists", guiElementId);
            yield break;
        }
        
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

    public IEnumerable<Result<Id<GuiElement>>> GetElementAndChildren(Id<GuiElement> guiElementId)
    {
        if (!Exists(guiElementId))
        {
            yield return new ResultProblem($"No element with id '{guiElementId}' exists");
            yield break;
        }
        
        HashSet<Id<GuiElement>> visited = [];
        var queue = new Queue<Id<GuiElement>>();
        queue.Enqueue(guiElementId);

        while (queue.TryDequeue(out var current))
        {
            if (!visited.Add(current))
            {
                yield return new ResultProblem("Cycle detected in GUI hierarchy at '{0}' while traversing children for '{1}'", current, guiElementId);
                yield break;
            }
            
            yield return current;

            if (!_guiElementChildren.TryGetValue(current, out var children))
            {
                continue;
            }
            
            foreach (var child in children)
            {
                queue.Enqueue(child);
            }
        }
    }

    public IEnumerable<Id<GuiElement>> GetRootElements() => _anchorChildren.Rights;

    public IReadOnlyCollection<Id<GuiElement>> GetChildrenForAnchor(Id<GuiAnchor> anchor) =>
        _anchorChildren.Get(anchor).Match<IReadOnlyCollection<Id<GuiElement>>>(
            set => set,
            notFound => []);
}