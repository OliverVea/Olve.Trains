using Microsoft.Extensions.Logging;
using Olve.Paths;
using Olve.Results;
using Scriban;
using Scriban.Runtime;

namespace Olve.Trains.AssetPipeline.Assets;

public class TemplateWriter(ILogger<TemplateWriter> logger, PathProvider pathProvider)
{
    public async Task<Result> WriteTemplateAsync<T>(string className, string @namespace, IEnumerable<Asset<T>> assets, IPath outputPath,
        CancellationToken ct = default)
    {
        var scriptObject = MapToScriptObject(className, @namespace, typeof(T).Name, assets);
        var templatePath = pathProvider.TemplatesSourceFolder / "AssetClass.scriban";
        return await WriteTemplateAsync(templatePath, scriptObject, outputPath, ct);
    }

    private static ScriptObject MapToScriptObject<T>(string className, string @namespace, string assetType, IEnumerable<Asset<T>> assets)
    {

        IReadOnlyCollection<ScriptObject> assetScriptObjects = assets.Select(asset => new ScriptObject()
        {
            { "Name", asset.Name },
            { "Source", asset.Source },
            { "Destination", asset.Destination }
        }).ToArray();

        ScriptObject scriptObject = new()
        {
            { "Namespace", @namespace },
            { "ClassName", className },
            { "AssetType", assetType },
            { "Assets", assetScriptObjects }
        };

        return scriptObject;
    }

    public async Task<Result> WriteTemplateAsync(IPath templatePath, ScriptObject scriptObject, IPath outputPath, CancellationToken ct = default)
    {
        try
        {
            var templateResult = await LoadTemplateAsync(templatePath, ct);
            if (templateResult.TryPickProblems(out var problems, out var template))
            {
                return problems.Prepend("Failed to load template");
            }

            var sourceCode = await template.RenderAsync(scriptObject);
            if (sourceCode is null)
            {
                return new ResultProblem("Failed to write source generated asset file");
            }

            await File.WriteAllTextAsync(outputPath.Path, sourceCode, ct);

            logger.LogDebug("Processed template '{TemplatePath}' to '{Destination}'", templatePath, outputPath);

            return Result.Success();
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Failed to write template '{0}' to path '{1}' with message: {2}", templatePath, outputPath, e.Message);
        }
    }

    public async Task<Result<Template>> LoadTemplateAsync(IPath templatePath, CancellationToken ct = default)
    {
        if (!File.Exists(templatePath.Path))
        {
            return new ResultProblem("Template file '{0}' does not exist", templatePath.Path);
        }

        var templateFile = await File.ReadAllTextAsync(templatePath.Path, ct);

        return Template.Parse(templateFile);
    }
}