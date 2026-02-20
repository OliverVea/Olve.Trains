using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Paths.Glob;
using Olve.Trains.AssetPipeline.Assets;
using Scriban.Runtime;

namespace Olve.Trains.AssetPipeline.Layouts;

public class ProcessLayouts(
    ILogger<ProcessLayouts> logger,
    TemplateWriter templateWriter,
    NamespaceProvider namespaceProvider,
    PathProvider pathProvider) : IAsyncOperation<ProcessLayouts.Request, ProcessLayouts.Response>
{
    private static readonly string TemplateFileName = "LayoutClass.scriban";
    private static readonly MD5 Md5 = MD5.Create();

    public record Request;
    public record Response(IReadOnlyList<IPath> GeneratedFiles);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing layout XML files");

        var layoutsRoot = pathProvider.LayoutsSourceFolder;

        var xmlFiles = layoutsRoot.TryGlob("*.xml", out var files) ? files : [];
        pathProvider.LayoutsOutputFolder.EnsurePathExists();

        var templatePath = pathProvider.TemplatesSourceFolder / TemplateFileName;

        List<IPath> generatedFiles = [];

        foreach (var absoluteXmlPath in xmlFiles)
        {
            logger.LogDebug("Reading layout: {LayoutFile}", absoluteXmlPath);

            XDocument doc;
            try
            {
                // Preserve whitespace so literal values are not altered by parser
                doc = XDocument.Load(absoluteXmlPath.Absolute.Path,
                    LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                return new ResultProblem(e,
                    "Failed to parse layout file '{0}' with message: {1}",
                    absoluteXmlPath,
                    e.Message);
            }

            var root = doc.Root;
            if (root is null)
            {
                return new ResultProblem("Layout file '{0}' has no root element.", absoluteXmlPath);
            }

            var className = string.Join("",
                absoluteXmlPath.Name!
                    .Split(".")
                    .SkipLast(1));

            List<Node> nodes = [];
            AddNodeAndChildren(root, nodes);

            IReadOnlyCollection<ScriptObject> nodeScriptObjects = nodes
                .Select(node => new ScriptObject()
                {
                    { "TypeName", node.TypeName },
                    {
                        "Children", node
                            .Children.Select(x => x.Id)
                            .ToArray()
                    },
                    { "Id", node.Id.Id },
                    { "ElementId", $"{className}/{node.TypeName}/{node.Id.Id}"},
                    { "StyleKey", node.StyleKey },
                    {
                        "Properties", node
                            .Properties.Select(nodeProperty =>
                                new ScriptObject() { { "Name", nodeProperty.Name }, { "Value", nodeProperty.Value }, })
                            .ToArray()
                    }
                })
                .ToList();

            var fixedIdsDefinition = string.Join(", ",
                nodes
                    .Where(x => x.Id.Fixed)
                    .Select(x => $"{x.TypeName} {x.Id.Id}"));

            var fixedIds = string.Join(", ",
                nodes
                    .Where(x => x.Id.Fixed)
                    .Select(x => $"{x.Id.Id}"));

            fixedIdsDefinition = fixedIdsDefinition.Length == 0 ? fixedIdsDefinition : fixedIdsDefinition + ", ";
            fixedIds = fixedIds.Length == 0 ? fixedIds : fixedIds + ", ";

        var elementList = string.Join(", ", nodes.Select(x => x.Id.Id).Reverse());

            var scriptObject = new ScriptObject()
            {
                { "Namespace", namespaceProvider.LayoutNamespace },
                { "ClassName", className },
                { "Nodes", nodeScriptObjects },
                { "FixedIdsDefinition", fixedIdsDefinition },
                { "FixedIds", fixedIds },
                { "ElementList", elementList }
            };

            var destinationPath = pathProvider.LayoutsOutputFolder / (className + ".cs");

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
    private readonly record struct Node(NodeId Id, string TypeName, string? StyleKey, IReadOnlyList<Property> Properties, IReadOnlyList<NodeId> Children);
    private readonly record struct NodeId(string Id, bool Fixed);
    private readonly record struct Property(string Name, string Value);

    private static NodeId AddNodeAndChildren(XElement element, List<Node> nodes)
    {
        // Translate attributes directly to property assignments.
        // The XML values are emitted verbatim; user ensures they are valid C# expressions.
        var properties = element.Attributes()
            .Select(a => new Property(a.Name.LocalName, a.Value))
            .ToList();

        var nodeName = element.Name.LocalName;
        NodeId? nodeId = null;
        string? styleKey = null;

        var idProperty = properties.Find(x => x.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase));
        if (idProperty != default)
        {
            properties.Remove(idProperty);
            nodeId = new NodeId(idProperty.Value, true);
        }

        var styleProperty = properties.Find(x => x.Name.Equals("style", StringComparison.InvariantCultureIgnoreCase));
        if (styleProperty != default)
        {
            properties.Remove(styleProperty);
            styleKey = styleProperty.Value;
        }

        var children = element
            .Elements()
            .Select(childElement => AddNodeAndChildren(childElement, nodes))
            .ToList();

        nodeId ??= new(nodeName + "_" + nodes.Count, false);
        Node node = new(nodeId.Value, nodeName, styleKey, properties, children);

        nodes.Add(node);

        return nodeId.Value;
    }
}
