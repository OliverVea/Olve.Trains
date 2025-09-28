namespace Olve.Trains.AssetPipeline;

public static class Paths
{
public const string ApplicationRoot = "/app";

    public static readonly string TempFolder = EnvHelper.ReadEnvVariableOrDefault("ASSET_TEMP_FOLDER", Directory.CreateTempSubdirectory().FullName);
    public static readonly string ShadersSourceFolder = Path.Combine(ApplicationRoot, "shaders");
    public static readonly string LayoutsSourceFolder = Path.Combine(ApplicationRoot, "layouts");
    public static readonly string TemplatesSourceFolder = Path.Combine(ApplicationRoot, "templates");

    public static readonly string OutputsFolder = EnvHelper.ReadEnvVariableOrDefault("ASSET_OUTPUT_FOLDER", Path.Combine(ApplicationRoot, "output"));
    public static readonly string MeshesOutputFolder = Path.Combine(OutputsFolder, "Meshes");
    public static readonly string TexturesOutputFolder = Path.Combine(OutputsFolder, "Textures");
    public static readonly string ShadersOutputFolder = Path.Combine(OutputsFolder, "Shaders");
    public static readonly string LayoutsOutputFolder = Path.Combine(OutputsFolder, "Layouts");
    public static readonly string TerrainsOutputFolder = Path.Combine(OutputsFolder, "Terrains");
}
