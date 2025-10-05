using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;
using Olve.Trains.AssetPipeline.Assets;
using Scriban.Runtime;

namespace Olve.Trains.AssetPipeline.Layouts;

public class ProcessLayouts(ILogger<ProcessLayouts> logger, TemplateWriter templateWriter, LayoutOptions layoutOptions) : IAsyncOperation<ProcessLayouts.Request, ProcessLayouts.Response>
{
    private static readonly string TemplateFileName = "LayoutClass.scriban";

    public record Request;
    public record Response(IReadOnlyList<string> GeneratedFiles);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing layout XML files");

        var layoutsRoot = string.IsNullOrWhiteSpace(layoutOptions.LayoutsDirectory)
            ? Paths.LayoutsSourceFolder
            : layoutOptions.LayoutsDirectory;

        if (!Directory.Exists(layoutsRoot))
        {
            logger.LogWarning("Layouts directory '{LayoutsRoot}' does not exist. Nothing to process.", layoutsRoot);
            return new Response([]);
        }

        var xmlFiles = Directory.GetFiles(layoutsRoot, "*.xml", SearchOption.AllDirectories);
        Directory.CreateDirectory(Paths.LayoutsOutputFolder);

        // Resolve template path (check container path first, then project-relative fallbacks)
        var templateCandidates = new[]
        {
            Path.Combine(Paths.TemplatesSourceFolder, TemplateFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Olve.Trains.AssetPipeline", "Templates", TemplateFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", TemplateFileName),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "src", "Olve.Trains.AssetPipeline", "Templates", TemplateFileName)
        }.Select(Path.GetFullPath).ToArray();

        var templatePath = templateCandidates.FirstOrDefault(File.Exists);
        if (templatePath == null)
        {
            return new ResultProblem("Template file '{0}' not found. Searched: {1}", TemplateFileName, string.Join("; ", templateCandidates));
        }

        List<string> generatedFiles = [];

        foreach (var absoluteXmlPath in xmlFiles)
        {
            logger.LogDebug("Reading layout: {LayoutFile}", absoluteXmlPath);

            XDocument doc;
            try
            {
                // Preserve whitespace so literal values are not altered by parser
                doc = XDocument.Load(absoluteXmlPath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                return new ResultProblem(e, "Failed to parse layout file '{0}' with message: {1}", absoluteXmlPath, e.Message);
            }

            var root = doc.Root;
            if (root is null)
            {
                return new ResultProblem("Layout file '{0}' has no root element.", absoluteXmlPath);
            }

            var fileName = Path.GetFileNameWithoutExtension(absoluteXmlPath);
            var className = ToPascalCase(fileName);
            var rootType = root.Name.LocalName;
            var rootVarName = ToCamelCase(className);

            var rootNode = BuildNode(root);

            var scriptObject = new ScriptObject
            {
                { "Namespace", string.IsNullOrWhiteSpace(layoutOptions.Namespace) ? "Olve.Trains.resources.layouts" : layoutOptions.Namespace },
                { "ClassName", className },
                { "RootType", rootType },
                { "RootVarName", rootVarName },
                { "RootNode", MapNodeToScript(rootNode) }
            };

            var destinationPath = Path.Combine(Paths.LayoutsOutputFolder, className + ".cs");

            var writeResult = await templateWriter.WriteTemplateAsync(templatePath, scriptObject, destinationPath, ct);
            if (writeResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to write layout source file for '{0}'", absoluteXmlPath);
            }

            logger.LogDebug("Generated layout class: {Destination}", destinationPath);
            generatedFiles.Add(destinationPath);
        }

        logger.LogInformation("Generated {Count} layout classes successfully.", generatedFiles.Count);
        return new Response(generatedFiles);
    }

    // Internal node model used to build ScriptObject for Scriban
    private readonly record struct Node(string TypeName, IReadOnlyList<Property> Properties, IReadOnlyList<Node> Children);
    private readonly record struct Property(string Name, string ValueLiteral);

    private static Node BuildNode(XElement element)
    {
        // Translate attributes directly to property assignments.
        // The XML values are emitted verbatim; user ensures they are valid C# expressions.
        var properties = element.Attributes()
            .Select(a => new Property(a.Name.LocalName, a.Value))
            .ToList();

        var children = element.Elements()
            .Select(BuildNode)
            .ToList();

        return new Node(element.Name.LocalName, properties, children);
    }

    private static ScriptObject MapNodeToScript(Node node)
    {
        ScriptObject scriptNode = new()
        {
            { "TypeName", node.TypeName }
        };

        List<ScriptObject> propObjects = [];
        foreach (var p in node.Properties)
        {
            propObjects.Add(new ScriptObject
            {
                { "Name", p.Name },
                { "ValueLiteral", p.ValueLiteral }
            });
        }
        scriptNode.Add("Properties", propObjects);

        List<ScriptObject> childObjects = [];
        foreach (var c in node.Children)
        {
            childObjects.Add(MapNodeToScript(c));
        }
        scriptNode.Add("Children", childObjects);

        return scriptNode;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var parts = input.Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries);
        var result = string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..] : string.Empty)));
        return result;
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToLowerInvariant(input[0]) + (input.Length > 1 ? input[1..] : string.Empty);
    }
}
