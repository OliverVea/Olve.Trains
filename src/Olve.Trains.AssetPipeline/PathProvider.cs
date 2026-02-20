using Microsoft.Extensions.Options;
using Olve.Trains.AssetPipeline.Options;

namespace Olve.Trains.AssetPipeline;

public class PathProvider(
    IOptions<BuildOptions> buildOptions,
    IOptions<MeshOptions> meshOptions,
    IOptions<TextureOptions> textureOptions,
    IOptions<TerrainOptions> terrainOptions,
    IOptions<ShaderOptions> shaderOptions,
    IOptions<LayoutOptions> layoutOptions,
    IOptions<FontOptions> fontOptions)
{
    // AKA Temp
    public IPath BuildPath => Paths.Path.Create(buildOptions.Value.BuildDirectory);
    public IPath BuildS3CachePath => BuildPath / "s3-cache";
    public IPath BuildGeneratedPath => BuildPath / "generated";
    public IPath BuildGeneratedFontsPath => BuildGeneratedPath / "fonts";

    public IPath OutputPath => Paths.Path.Create(buildOptions.Value.OutputDirectory ?? throw new InvalidOperationException("Output directory not set"));

    public IPath ShadersSourceFolder => Paths.Path.Create(
        string.IsNullOrWhiteSpace(shaderOptions.Value.ShadersDirectory)
            ? throw new InvalidOperationException("ShaderOptions.ShadersDirectory must be configured when shaders target is enabled.")
            : shaderOptions.Value.ShadersDirectory);

    public IPath LayoutsSourceFolder => Paths.Path.Create(
        string.IsNullOrWhiteSpace(layoutOptions.Value.LayoutsDirectory)
            ? throw new InvalidOperationException("LayoutOptions.LayoutsDirectory must be configured when layouts target is enabled.")
            : layoutOptions.Value.LayoutsDirectory);

    private IPath AssemblyFolder => Paths.Path.TryGetAssemblyExecutable(out var assembly)
        ? assembly.Parent
        : throw new InvalidOperationException("Could not find assembly folder");
    public IPath TemplatesSourceFolder => AssemblyFolder / "templates";

    public IPath MeshesOutputFolder => OutputPath / meshOptions.Value.OutputFolder;
    public IPath TexturesOutputFolder => OutputPath / textureOptions.Value.OutputFolder;
    public IPath TerrainsOutputFolder => OutputPath / terrainOptions.Value.OutputFolder;
    public IPath ShadersOutputFolder => OutputPath / shaderOptions.Value.OutputFolder;
    public IPath LayoutsOutputFolder => OutputPath / layoutOptions.Value.OutputFolder;
    public IPath FontsOutputFolder => OutputPath / fontOptions.Value.OutputFolder;
}