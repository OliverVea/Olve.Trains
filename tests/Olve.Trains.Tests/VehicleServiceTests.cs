using Olve.Trains.Scenes.Game;

namespace Olve.Trains.Tests;

public class VehicleServiceTests
{
    [Test]
    public async Task AddAndRemoveVehicle()
    {
        // Arrange
        TrackService trackService = new();
        VehicleService vehicleService = new(trackService);

        var trackIdResult = trackService.AddTrack(new TrackPoint(default, default), new TrackPoint(default, default));
        var trackId = trackIdResult.Value;

        // Act
        var vehicleIdResult = vehicleService.AddVehicle(trackId, 0.0f);

        // Assert
        await Assert.That(vehicleIdResult.Succeeded).IsTrue();
        var vehicleId = vehicleIdResult.Value;

        await Assert.That(vehicleService.RemoveVehicle(vehicleId).Succeeded).IsTrue();
    }

    [Test]
    public async Task UpdateVehiclePosition()
    {
        // Arrange
        TrackService trackService = new();
        VehicleService vehicleService = new(trackService);

        var trackIdResult = trackService.AddTrack(new TrackPoint(default, default), new TrackPoint(default, default));
        var trackId = trackIdResult.Value;
        var vehicleId = vehicleService.AddVehicle(trackId, 0.0f).Value;

        var newTrackIdResult = trackService.AddTrack(new TrackPoint(default, default), new TrackPoint(default, default));
        var newTrackId = newTrackIdResult.Value;

        // Act
        var updateResult = vehicleService.UpdateVehiclePosition(vehicleId, newTrackId, 0.5f);

        // Assert
        await Assert.That(updateResult.Succeeded).IsTrue();
        await Assert.That(vehicleService.TryGetVehicle(vehicleId, out var vehicle)).IsTrue();
        await Assert.That(vehicle.TrackId).IsEqualTo(newTrackId);
        await Assert.That(vehicle.Position).IsEqualTo(0.5f);
    }
}

