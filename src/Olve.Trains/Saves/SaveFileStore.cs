using Olve.Paths;

namespace Olve.Trains.Saves;

/// <summary>Explicit save intent. Every save names one — there is no implicit default.</summary>
public enum SaveKind
{
    /// <summary>User-named save.</summary>
    Manual,

    /// <summary>Player-triggered quick save.</summary>
    Quicksave,

    /// <summary>Game-triggered automatic save.</summary>
    Autosave,
}

/// <summary>
/// App-level store for save files: resolves a (kind, name) to a path under the user's local application
/// data and reads/writes the JSON via <see cref="SaveFileSchema"/>. A single instance is shared by the
/// save (game) and load (loading) scenes.
/// </summary>
public sealed class SaveFileStore
{
    private readonly IPath _saveRoot;

    public SaveFileStore()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _saveRoot = Path.Create(localAppData) / "Olve.Trains" / "saves";
    }

    /// <summary>Root directory all saves live under.</summary>
    public IPath SaveRoot => _saveRoot;

    /// <summary>Resolves the on-disk path for a save without touching the filesystem.</summary>
    public Result<IPath> ResolvePath(SaveKind kind, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new ResultProblem("Save name must not be empty.");
        }

        if (name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            return new ResultProblem("Save name '{0}' contains invalid characters.", name);
        }

        return Result<IPath>.Success(_saveRoot / KindFolder(kind) / (name + ".json"));
    }

    /// <summary>Serializes a save and writes it to disk, creating the directory if needed.</summary>
    public Result<IPath> Write(SaveFile saveFile, SaveKind kind, string name)
    {
        if (ResolvePath(kind, name).TryPickProblems(out var problems, out var path))
        {
            return problems;
        }

        path.Parent.EnsurePathExists();

        try
        {
            File.WriteAllText(path.Path, SaveFileSchema.Serialize(saveFile));
        }
        catch (IOException exception)
        {
            return new ResultProblem("Failed to write save file '{0}': {1}", path.Path, exception.Message);
        }

        return Result<IPath>.Success(path);
    }

    /// <summary>Reads and deserializes a save by kind and name.</summary>
    public Result<SaveFile> Read(SaveKind kind, string name)
        => ResolvePath(kind, name).Bind(path => Read(path));

    /// <summary>Reads and deserializes a save from an explicit path.</summary>
    public Result<SaveFile> Read(IPath path)
    {
        if (!path.Exists())
        {
            return new ResultProblem("Save file '{0}' does not exist.", path.Path);
        }

        string json;
        try
        {
            json = File.ReadAllText(path.Path);
        }
        catch (IOException exception)
        {
            return new ResultProblem("Failed to read save file '{0}': {1}", path.Path, exception.Message);
        }

        return SaveFileSchema.Deserialize(json);
    }

    private static string KindFolder(SaveKind kind) => kind switch
    {
        SaveKind.Manual => "manual",
        SaveKind.Quicksave => "quicksave",
        SaveKind.Autosave => "autosave",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown save kind."),
    };
}
