namespace Olve.Trains.AssetPipeline;

public static class Paths
{
public const string ApplicationRoot = "/app";

    public static readonly string TempFolder = EnvHelper.ReadEnvVariableOrDefault("ASSET_TEMP_FOLDER", Directory.CreateTempSubdirectory().FullName);
    public static readonly string ShaderSourceFolder = Path.Combine(ApplicationRoot, "shaders");
    public static readonly string TemplatesSourceFolder = Path.Combine(ApplicationRoot, "Templates");

    public static readonly string OutputFolder = EnvHelper.ReadEnvVariableOrDefault("ASSET_OUTPUT_FOLDER", Path.Combine(ApplicationRoot, "output"));
    public static readonly string MeshOutputFolder = Path.Combine(OutputFolder, "meshes");
    public static readonly string TextureOutputFolder = Path.Combine(OutputFolder, "textures");
    public static readonly string ShaderOutputFolder = Path.Combine(OutputFolder, "shaders");
    public static readonly string TerrainOutputFolder = Path.Combine(OutputFolder, "terrains");
}
