using System.Runtime.InteropServices;
using Olve.Utilities.Lookup;
using Olve.Trains.Scenes.GameLogic.Ordering;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Track(Id<Track> Id, TrackEndpoint Start, TrackEndpoint End, long CreatedSequence)
    : IHasId<Id<Track>>, IHasCreatedSequence;