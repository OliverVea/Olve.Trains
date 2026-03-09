using Olve.Utilities.Collections;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainGroupService
{
    private readonly ManyToManyLookup<Id<TrainGroup>, Id<Train>> _membership = new();

    public void AddToGroup(Id<Train> trainId, Id<TrainGroup> groupId)
    {
        _membership.Set(groupId, trainId, true);
    }

    public void RemoveFromGroup(Id<Train> trainId, Id<TrainGroup> groupId)
    {
        _membership.Set(groupId, trainId, false);
    }

    public bool IsMemberOf(Id<Train> trainId, Id<TrainGroup> groupId)
    {
        return _membership.Contains(groupId, trainId);
    }
}
