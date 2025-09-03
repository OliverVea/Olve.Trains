using Olve.Engine3D;
using Olve.Trains.Scenes.Game.Junctions;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class VehicleJunctionService(VehiclePositionService vehiclePositionService, TrackService trackService, TrackSplineService trackSplineService, JunctionService junctionService)
{
    public Result<VehicleJunction> GetVehicleJunction(Id<Vehicle> vehicleId)
    {
        if (!vehiclePositionService.TryGetTrackPosition(vehicleId, out var trackPosition))
        {
            return new ResultProblem("Vehicle does not have a track position - likely not on a track");
        } 
        
        if (trackService.Get(trackPosition.TrackId).TryPickProblems(out var problems, out var track))
        {
            return problems;
        }

        if (trackPosition.Time < MathConstants.Epsilon && junctionService.TryGetJunctionId(track.Start, out var junctionId)
            || float.Abs(1 - trackPosition.Time) < MathConstants.Epsilon && junctionService.TryGetJunctionId(track.End, out junctionId))
        {
            return new VehicleJunction(junctionId);
        }

        return VehicleJunction.None;
    }
    
    public Result<TrackPoint> GetVehicleTrackPoint(Id<Vehicle> vehicleId)
    {
        if (!vehiclePositionService.TryGetTrackPosition(vehicleId, out var trackPosition))
        {
            return new ResultProblem("Vehicle does not have a track position - likely not on a track");
        } 
        
        if (trackService.Get(trackPosition.TrackId).TryPickProblems(out var problems, out var track))
        {
            return problems;
        }

        if (trackPosition.Time < 0.5f)
        {
            return track.Start;
        }
        
        return track.End;
    }

    public Result<bool> IsAtTrackEnd(Id<Vehicle> vehicleId, Id<Track> trackId)
    {
        if (!vehiclePositionService.TryGetTrackPosition(vehicleId, out var trackPosition))
        {
            return new ResultProblem("Vehicle does not have a track position - likely not on a track");
        } 

        if (trackService.Get(trackId).TryPickProblems(out var problems, out var track))
        {
            return problems;
        }
            
        if (trackSplineService.GetPosition(trackPosition.TrackId, trackPosition.Time).TryPickProblems(out problems, out var position))
        {
            return problems.Prepend("Failed to sample point with t '{0}' on track with id '{1}' for vehicle with id '{2}'", trackPosition.Time, trackPosition.TrackId, vehicleId);
        }
        
        var endDelta = position.Position - track.End.Point;
        
        return endDelta.Length < MathConstants.Epsilon;
    }
}

