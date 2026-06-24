using System.Text.Json;

namespace Olve.Trains.Saves;

/// <summary>
/// Serialization entry point for <see cref="SaveFile"/>. Owns the current schema version and the JSON
/// options, and reads a document's <see cref="SaveFile.Version"/> before binding the rest so future
/// versions can be upgraded forward.
/// </summary>
public static class SaveFileSchema
{
    /// <summary>Current schema version written by <see cref="Serialize"/>.</summary>
    public const int CurrentVersion = 1;

    /// <summary>JSON options shared by save serialization and deserialization.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Serializes a save to JSON, stamping it with the current schema version.</summary>
    public static string Serialize(SaveFile saveFile)
        => JsonSerializer.Serialize(saveFile with { Version = CurrentVersion }, JsonOptions);

    /// <summary>
    /// Deserializes a save from JSON. The <c>version</c> field is read first; documents newer than
    /// <see cref="CurrentVersion"/> are rejected rather than silently mis-parsed.
    /// </summary>
    public static Result<SaveFile> Deserialize(string json)
    {
        int version;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("version", out var versionElement)
                || versionElement.ValueKind != JsonValueKind.Number
                || !versionElement.TryGetInt32(out version))
            {
                return new ResultProblem("Save file is missing a numeric 'version' field.");
            }
        }
        catch (JsonException exception)
        {
            return new ResultProblem("Save file is not valid JSON: {0}", exception.Message);
        }

        if (version > CurrentVersion)
        {
            return new ResultProblem(
                "Save file schema version {0} is newer than the supported version {1}.",
                version, CurrentVersion);
        }

        // Only v1 exists today. When new versions are introduced, branch here to upgrade older documents.
        SaveFile? saveFile;
        try
        {
            saveFile = JsonSerializer.Deserialize<SaveFile>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            return new ResultProblem("Save file could not be deserialized: {0}", exception.Message);
        }

        if (saveFile is null)
        {
            return new ResultProblem("Save file deserialized to null.");
        }

        return saveFile;
    }
}
