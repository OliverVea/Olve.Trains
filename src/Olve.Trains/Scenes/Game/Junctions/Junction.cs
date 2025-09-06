using System.Runtime.InteropServices;
using Olve.Engine3D;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.Game.Junctions;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Junction(Id<Junction> Id, TilePosition Position) : IHasId<Id<Junction>>;