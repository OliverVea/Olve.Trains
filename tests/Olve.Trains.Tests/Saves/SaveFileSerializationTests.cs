using System.Runtime.CompilerServices;
using Olve.Trains.Saves;

namespace Olve.Trains.Tests.Saves;

/// <summary>
/// Golden-file tests for the save serialization layer. The golden JSON is the breaking-change alarm:
/// any change to a save model's shape changes the serialized output and fails <see cref="Serialize_matches_golden"/>,
/// forcing a deliberate update (and, for a real format change, a <see cref="SaveFileSchema.CurrentVersion"/> bump).
/// </summary>
public class SaveFileSerializationTests
{
    private static SaveFile BuildSample() => new()
    {
        Money = 12_345,
        Time = new SaveTime
        {
            DayStartHours = 5.5f,
            DayDurationSeconds = 900.0,
            CurrentGameHours = 13.25,
        },
        Terrain = new SaveTerrain
        {
            Width = 3,
            Length = 2,
            Step = 0.125f,
            Heights = [1, 1, 2, 1, 3, 1],
        },
        Environment = new SaveEnvironment
        {
            Objects =
            [
                new SaveEnvironmentalObject
                {
                    Id = "11111111-1111-1111-1111-111111111111",
                    BlueprintId = "22222222-2222-2222-2222-222222222222",
                    Position = new SaveVector3 { X = 1.5f, Y = 0f, Z = 2.5f },
                    Rotation = new SaveQuaternion { X = 0f, Y = 0.7071068f, Z = 0f, W = 0.7071068f },
                },
            ],
        },
        Camera = new SaveCamera { OrthographicSize = 40f },
    };

    [Test]
    public async Task Serialize_matches_golden()
    {
        var actual = SaveFileSchema.Serialize(BuildSample());
        await Assert.That(actual).IsEqualTo(ReadGolden());
    }

    [Test]
    public async Task Golden_round_trips_back_to_itself()
    {
        var golden = ReadGolden();

        var result = SaveFileSchema.Deserialize(golden);
        await Assert.That(result.TryPickProblems(out _, out var save)).IsFalse();

        await Assert.That(SaveFileSchema.Serialize(save!)).IsEqualTo(golden);
    }

    [Test]
    public async Task Deserialize_rejects_newer_schema_version()
    {
        var newer = ReadGolden()
            .Replace($"\"version\": {SaveFileSchema.CurrentVersion}", $"\"version\": {SaveFileSchema.CurrentVersion + 1}");

        var rejected = SaveFileSchema.Deserialize(newer).TryPickProblems(out _, out _);

        await Assert.That(rejected).IsTrue();
    }

    [Test]
    public async Task Deserialize_rejects_missing_version()
    {
        var rejected = SaveFileSchema.Deserialize("{ \"money\": 1 }").TryPickProblems(out _, out _);

        await Assert.That(rejected).IsTrue();
    }

    private static string ReadGolden([CallerFilePath] string sourcePath = "")
    {
        var goldenPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(sourcePath)!, "golden-save-v1.json");

        if (!File.Exists(goldenPath))
        {
            File.WriteAllText(goldenPath, SaveFileSchema.Serialize(BuildSample()));
            throw new InvalidOperationException($"Golden file seeded at '{goldenPath}'. Inspect it, then re-run the tests.");
        }

        return File.ReadAllText(goldenPath);
    }
}
