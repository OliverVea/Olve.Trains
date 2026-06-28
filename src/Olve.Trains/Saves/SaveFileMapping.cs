using Olve.Engine3D.Assets.Entities;
using Olve.Trains.Scenes.GameLogic.Environment;

namespace Olve.Trains.Saves;

/// <summary>
/// Domain → DTO boundary for save serialization. The save DTOs in <see cref="SaveFile"/> own their own
/// shape and never reference runtime domain types; these helpers materialize each domain value into its
/// serialization twin. This is the only place that bridges the two.
/// </summary>
public static class SaveFileMapping
{
    public static SaveTerrain ToSaveTerrain(this HeightmapData heightmap) => new()
    {
        Width = heightmap.Width,
        Length = heightmap.Length,
        Step = heightmap.Step,
        Heights = (int[])heightmap.Heights.Clone(),
    };

    public static SaveEnvironmentalObject ToSaveEnvironmentalObject(this EnvironmentalObject obj) => new()
    {
        Id = obj.Id.ToString(),
        BlueprintId = obj.BlueprintId.ToString(),
        Position = obj.Position.Position.ToSaveVector3(),
        Rotation = obj.Position.Rotation.ToSaveQuaternion(),
    };

    public static SaveVector3 ToSaveVector3(this Vector3D<float> vector) => new()
    {
        X = vector.X,
        Y = vector.Y,
        Z = vector.Z,
    };

    public static SaveQuaternion ToSaveQuaternion(this Quaternion<float> quaternion) => new()
    {
        X = quaternion.X,
        Y = quaternion.Y,
        Z = quaternion.Z,
        W = quaternion.W,
    };
}
