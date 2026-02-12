using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.UI.Tools;

public readonly record struct Tool(Id<Tool> Id, string Name) : IHasId<Id<Tool>>;