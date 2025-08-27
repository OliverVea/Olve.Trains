using Microsoft.Extensions.Logging;
using Olve.Results;
using Scriban;
using Scriban.Runtime;

namespace Olve.Trains.AssetPipeline.Assets;

public class TemplateWriter(ILogger<TemplateWriter> logger)
{
    private const string AssetTemplatePath = "Templates/AssetClass.scriban";

    public async Task<Result> WriteTemplateAsync<T>(string className, IEnumerable<Asset<T>> assets, string outputPath,
        CancellationToken ct = default)
    {
        var scriptObject = MapToScriptObject(className, typeof(T).Name, assets);

        return await WriteTemplateAsync(AssetTemplatePath, scriptObject, outputPath, ct);
    }

    private static ScriptObject MapToScriptObject<T>(string className, string assetType, IEnumerable<Asset<T>> assets)
    {
        ScriptObject scriptObject = new();

        List<ScriptObject> assetScriptObjects = [];

        foreach (var asset in assets)
        {
            ScriptObject assetScriptObject = new()
            {
                { "Name", asset.Name },
                { "Source", asset.Source },
                { "Destination", asset.Destination }
            };

            assetScriptObjects.Add(assetScriptObject);
        }

        scriptObject.Add("ClassName", className);
        scriptObject.Add("AssetType", assetType);
        scriptObject.Add("Assets", assetScriptObjects);

        return scriptObject;
    }

    public async Task<Result> WriteTemplateAsync(string templatePath, ScriptObject scriptObject, string outputPath, CancellationToken ct = default)
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

            await File.WriteAllTextAsync(outputPath, sourceCode, ct);

            logger.LogDebug("Processed template '{TemplatePath}' to '{Destination}'", templatePath, outputPath);

            return Result.Success();
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Failed to write template '{0}' to path '{1}' with message: {2}", templatePath, outputPath, e.Message);
        }
    }

    public async Task<Result<Template>> LoadTemplateAsync(string templatePath, CancellationToken ct = default)
    {
        if (!File.Exists(templatePath))
        {
            return new ResultProblem("Template file '{0}' does not exist", templatePath);
        }

        var templateFile = await File.ReadAllTextAsync(templatePath, ct);

        return Template.Parse(templateFile);
    }
}