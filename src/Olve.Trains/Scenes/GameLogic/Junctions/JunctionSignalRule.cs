using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

public readonly record struct JunctionSignalRule(
    Id<JunctionSignalRule> Id,
    Id<Junction> JunctionId,
    IReadOnlyList<SignalRuleVehicle> Vehicles,
    IReadOnlyList<SignalRuleSource> Sources,
    IReadOnlyList<SignalRuleDestination> Destinations,
    SignalRuleDistribution Distribution) : IHasId<Id<JunctionSignalRule>>
{
    public override string ToString()
    {
        var vehiclesString = string.Join(", ", Vehicles.Select(x => x.ToString()));
        var sourcesString = string.Join(", ", Sources.Select(x => x.ToString()));
        var destinationsString = string.Join(", ", Destinations.Select(x => x.ToString()));
        var distributionString =  Distribution.ToString();
        
        return $"Rule(id: {Id}, Vehicles: [{vehiclesString}], Sources: [{sourcesString}], Destinations: [{destinationsString}], Distribution: [{distributionString}]";
    }
}