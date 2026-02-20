using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olve.MsdfAtlasGen;
using Olve.MsdfAtlasGen.Models;
using Olve.Operations;
using Olve.Trains.AssetPipeline.Assets;
using Olve.Trains.AssetPipeline.Options;
using Scriban.Runtime;

namespace Olve.Trains.AssetPipeline.Fonts;

public class ProcessFonts(
    ILogger<ProcessFonts> logger,
    TemplateWriter templateWriter,
    NamespaceProvider namespaceProvider,
    PathProvider pathProvider,
    IOptions<FontOptions> fontOptions) : IAsyncOperation<ProcessFonts.Request, ProcessFonts.Response>
{
    private static readonly string TemplateFileName = "FontClass.scriban";

    public record FontAtlasInfo(IPath Atlas, IPath Config);

    public record Request(IReadOnlyList<FileInfo> AssetFiles);
    public record Response(IReadOnlyList<IPath> GeneratedFiles, IReadOnlyList<FontAtlasInfo> FontAtlases);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing font files");

        // Filter for .ttf files from asset files
        var ttfFiles = request.AssetFiles
            .Where(f => f.Extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (ttfFiles.Count == 0)
        {
            logger.LogWarning("No .ttf font files found in asset files");
            return new Response([], []);
        }

        pathProvider.FontsOutputFolder.EnsurePathExists();
        pathProvider.BuildGeneratedFontsPath.EnsurePathExists();

        var templatePath = pathProvider.TemplatesSourceFolder / TemplateFileName;

        List<IPath> generatedFiles = [];
        List<FontAtlasInfo> fontAtlases = [];

        foreach (var ttfFile in ttfFiles)
        {
            logger.LogDebug("Processing font: {FontFile}", ttfFile.Name);

            var fontName = System.IO.Path.GetFileNameWithoutExtension(ttfFile.Name).Replace("-", "");

            // Generate atlas to build/generated/fonts directory
            var atlasImagePath = pathProvider.BuildGeneratedFontsPath / $"{fontName}-atlas.png";
            var atlasJsonPath = pathProvider.BuildGeneratedFontsPath / $"{fontName}-atlas.json";

            AtlasResult atlasResult;
            try
            {
                atlasResult = AtlasGenerator.Generate(new AtlasConfig
                {
                    FontPath = ttfFile.FullName,
                    Type = AtlasType.MSDF,
                    Dimensions = (fontOptions.Value.AtlasWidth, fontOptions.Value.AtlasHeight),
                    GlyphSize = fontOptions.Value.GlyphSize,
                    PixelRange = fontOptions.Value.PixelRange,
                    OutputImagePath = atlasImagePath.Absolute.Path,
                    OutputJsonPath = atlasJsonPath.Absolute.Path
                });

                logger.LogDebug("Generated atlas for {FontName}: {GlyphCount} glyphs, {KerningCount} kerning pairs",
                    fontName, atlasResult.Glyphs.Count, atlasResult.Kerning.Count);
            }
            catch (Exception e)
            {
                return new ResultProblem(e,
                    "Failed to generate atlas for font '{0}' with message: {1}",
                    ttfFile.Name,
                    e.Message);
            }

            // Track atlas for texture processing pipeline
            fontAtlases.Add(new FontAtlasInfo(atlasImagePath, atlasJsonPath));

            // Build script object for template
            var glyphScriptObjects = atlasResult.Glyphs
                .Select(glyph => CreateGlyphScriptObject(glyph))
                .ToArray();

            var scriptObject = new ScriptObject
            {
                { "Namespace", namespaceProvider.FontNamespace },
                {
                    "Font", new ScriptObject
                    {
                        { "Name", fontName },
                        {
                            "Atlas", new ScriptObject
                            {
                                { "DistanceRange", atlasResult.DistanceRange },
                                { "DistanceRangeMiddle", atlasResult.DistanceRange / 2 },
                                { "GlyphSize", fontOptions.Value.GlyphSize },
                                {
                                    "Path", new ScriptObject
                                    {
                                        { "Name", $"{fontName}-atlas" },
                                        { "Path", $"fonts/{fontName}-atlas.texture" }
                                    }
                                },
                                {
                                    "Size", new ScriptObject
                                    {
                                        { "X", atlasResult.Width },
                                        { "Y", atlasResult.Height }
                                    }
                                },
                                { "Type", "MSDF" }
                            }
                        },
                        { "Glyphs", glyphScriptObjects },
                        {
                            "Metrics", new ScriptObject
                            {
                                { "Ascender", atlasResult.Metrics.Ascender },
                                { "Descender", atlasResult.Metrics.Descender },
                                { "UnderlineY", atlasResult.Metrics.UnderlineY },
                                { "UnderlineThickness", atlasResult.Metrics.UnderlineThickness },
                                { "YPointsDown", false }, // MSDF typically uses Y-up
                                { "EmSize", atlasResult.Metrics.EmSize },
                                { "LineHeight", atlasResult.Metrics.LineHeight }
                            }
                        }
                    }
                }
            };

            var destinationPath = pathProvider.FontsOutputFolder / (fontName + ".cs");

            var writeResult = await templateWriter.WriteTemplateAsync(templatePath, scriptObject, destinationPath, ct);
            if (writeResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to write font source file for '{0}'", ttfFile.Name);
            }

            logger.LogDebug("Generated font class: {Destination}", destinationPath);
            generatedFiles.Add(destinationPath);
        }

        logger.LogInformation("Generated {Count} font classes successfully.", generatedFiles.Count);
        return new Response(generatedFiles, fontAtlases);
    }

    private static ScriptObject CreateGlyphScriptObject(GlyphInfo glyph)
    {
        var glyphObject = new ScriptObject
        {
            { "Rune", (int)new Rune(glyph.Character).Value },
            { "Index", glyph.Unicode },
            { "Advance", glyph.Advance }
        };

        // Only add bounds if they exist (some glyphs like space may not have bounds)
        if (glyph.PlaneBounds != default && glyph.AtlasBounds != default)
        {
            glyphObject.Add("Bounds", new ScriptObject
            {
                {
                    "Atlas", new ScriptObject
                    {
                        {
                            "Origin", new ScriptObject
                            {
                                { "X", glyph.AtlasBounds.Left },
                                { "Y", glyph.AtlasBounds.Bottom }
                            }
                        },
                        {
                            "Size", new ScriptObject
                            {
                                { "X", glyph.AtlasBounds.Right - glyph.AtlasBounds.Left },
                                { "Y", glyph.AtlasBounds.Top - glyph.AtlasBounds.Bottom }
                            }
                        }
                    }
                },
                {
                    "Plane", new ScriptObject
                    {
                        {
                            "Origin", new ScriptObject
                            {
                                { "X", glyph.PlaneBounds.Left },
                                { "Y", glyph.PlaneBounds.Bottom }
                            }
                        },
                        {
                            "Size", new ScriptObject
                            {
                                { "X", glyph.PlaneBounds.Right - glyph.PlaneBounds.Left },
                                { "Y", glyph.PlaneBounds.Top - glyph.PlaneBounds.Bottom }
                            }
                        }
                    }
                }
            });
        }

        return glyphObject;
    }
}
