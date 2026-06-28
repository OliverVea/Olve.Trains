using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Math;
using Olve.Trains.Scenes.GameLogic.Environment;

namespace Olve.Trains.Saves;

/// <summary>
/// Mapping boundary between the runtime domain and the save DTOs in <see cref="SaveFile"/>. The DTOs own
/// their own shape and never reference runtime domain types; these helpers materialize each value into its
/// twin on the way out (domain → DTO) and back into the domain on the way in (DTO → domain). This is the
/// only place that bridges the two.
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

    public static HeightmapData ToHeightmapData(this SaveTerrain terrain) => new()
    {
        Width = terrain.Width,
        Length = terrain.Length,
        Step = terrain.Step,
        Heights = (int[])terrain.Heights.Clone(),
    };

    public static Vector3D<float> ToVector3D(this SaveVector3 vector) => new(vector.X, vector.Y, vector.Z);

    public static Quaternion<float> ToQuaternion(this SaveQuaternion quaternion)
        => new(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

    public static Position3D ToPosition3D(this SaveVector3 position, SaveQuaternion rotation)
        => new(position.ToVector3D(), rotation.ToQuaternion());

    /// <summary>
    /// Materializes a saved environmental object back into its domain form. Mesh/texture are intentionally
    /// not persisted — the renderer resolves them from the blueprint on load.
    /// </summary>
    public static Result<EnvironmentalObject> ToEnvironmentalObject(this SaveEnvironmentalObject obj)
    {
        if (!Id.TryParse<EnvironmentalObject>(obj.Id, out var id))
        {
            return new ResultProblem("Save environmental object has invalid id '{0}'.", obj.Id);
        }

        if (!Id.TryParse<EnvironmentalObjectBlueprint>(obj.BlueprintId, out var blueprintId))
        {
            return new ResultProblem("Save environmental object '{0}' has invalid blueprint id '{1}'.", obj.Id, obj.BlueprintId);
        }

        return new EnvironmentalObject(id, blueprintId, obj.Position.ToPosition3D(obj.Rotation));
    }
}
