# Pathfinding

The core part of the game is to allow trains to drive on tracks from A to B. An incremental approach to implementing train navigation is required. The end state is the two following goals:
1. Trains should be able to navigate networks with thousands of nodes efficiently without slowing down the game.
2. A set of track signals must be implemented, allowing the train network designer useful and granular tools for directing traffic. We should have systems that allow the user to specify signal behavior with logic such as round-robin assigning of paths, prioritized paths, and more. The logic system should be it's own system and UI controlling the path signal.


## Progress
### Current state

Currently, we only have path segments and lookups.  `TrackService` allows to lookup specific track segments. 

Since tracks are only allowed on the intersection of tiles (integer 3D coordinates), it's possible to lookup tracks with endings in specific discrete coorinates. This can be done using `TrackLookupService`. 

### Next steps

We will need to implement basic movement along a train, establishing a `VehicleService` which tracks the position of point-based trains.

To move the trains, we will need to make a movement system which focuses on maintainability and extensibility. For example `VehicleNavigationStrategy` where we can implement a dummy `KeepRightVehicleNavigationStrategy`.

Then we implement pathfinding, getting the sequence of tracks from (closest to) A to (closest to) B. This should be implemented as A* and, again, the `TrackPathfindingStrategy` should be modular and replacable in nature. for now, sample the points of the spline similarly to how the splines are drawn.

Paths from the pathfinding should be cached under a From, To. we can keep the cache with a limited size (e.g. 50). Any updates to the train network at all should mark the path as Old (but still allowed for direct lookup for existing trains), but changes to any of a track's path must invalidate the path and mark it for recalculation, even for trains with the actual path id.
