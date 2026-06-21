using System.Text.Json;
using Microsoft.Build.Locator;

MSBuildLocator.RegisterDefaults();
return await Runner.Analyze();

static class Runner
{
    static readonly string Repo = Environment.GetEnvironmentVariable("METRICS_REPO") ?? Directory.GetCurrentDirectory();
    static readonly string[] Projects =
    {
        "src/Olve.Engine3D/Olve.Engine3D.csproj",
        "src/Olve.Trains/Olve.Trains.csproj",
        "src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj",
    };

    public static async Task<int> Analyze()
    {
        using var ws = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
        ws.WorkspaceFailed += (_, e) =>
        {
            if (e.Diagnostic.Kind == Microsoft.CodeAnalysis.WorkspaceDiagnosticKind.Failure)
                Console.Error.WriteLine("  [ws] " + e.Diagnostic.Message);
        };

        var asm = new List<object>();
        var nss = new List<object>();
        var types = new List<TypeRow>();

        foreach (var rel in Projects)
        {
            var path = Path.Combine(Repo, rel);
            Console.Error.WriteLine($"== loading {rel}");
            var project = await ws.OpenProjectAsync(path);
            var comp = await project.GetCompilationAsync();
            if (comp is null) { Console.Error.WriteLine("  no compilation"); continue; }

            var data = await Microsoft.CodeAnalysis.CodeMetrics.CodeAnalysisMetricData.ComputeAsync(
                comp.Assembly,
                new Microsoft.CodeAnalysis.CodeMetrics.CodeMetricsAnalysisContext(comp, default));

            string asmName = comp.AssemblyName ?? rel;
            asm.Add(new
            {
                name = asmName,
                mi = data.MaintainabilityIndex,
                cc = data.CyclomaticComplexity,
                coupling = data.CoupledNamedTypes.Count,
                dit = data.DepthOfInheritance ?? 0,
                sourceLines = data.SourceLines,
                execLines = data.ExecutableLines,
            });

            int before = types.Count;
            Walk(data, asmName, "", nss, types);
            Console.Error.WriteLine($"   types: {types.Count - before}");
        }

        var files = types.Where(t => t.file != null)
            .GroupBy(t => t.file!)
            .Select(g => new
            {
                file = g.Key,
                types = g.Count(),
                mi = (int)Math.Round(g.Average(t => t.mi)),
                cc = g.Sum(t => t.cc),
                ditMax = g.Max(t => t.dit),
                couplingMax = g.Max(t => t.coupling),
                sourceLines = g.Sum(t => t.sourceLines),
                execLines = g.Sum(t => t.execLines),
            })
            .OrderBy(f => f.mi).ToList();

        var outObj = new
        {
            definitions = new
            {
                source = "Microsoft.CodeAnalysis.Metrics 4.14.0 CodeAnalysisMetricData.ComputeAsync (same engine as VS Calculate Code Metrics, run cross-platform via the public API)",
                mi = "Maintainability Index 0-100 (>=20 good, 10-19 moderate, <10 low)",
                cc = "Cyclomatic complexity (IOperation-based, Microsoft definition)",
                coupling = "Class coupling = distinct named types referenced",
                dit = "Depth of inheritance",
                lines = "sourceLines physical; execLines executable (logical instructions)",
                rollup = "assembly/namespace/type values from the metrics engine; file = aggregate of its types (mi avg, cc sum, dit/coupling max)",
            },
            assemblies = asm,
            namespaces = nss,
            types = types.Select(t => new { t.assembly, t.ns, t.name, t.file, t.mi, t.cc, t.dit, t.coupling, t.sourceLines, t.execLines, t.members }),
            files,
        };

        var outPath = Environment.GetEnvironmentVariable("METRICS_OUT") ?? Path.Combine(Repo, "tools/code-metrics/roslyn-metrics.json");
        File.WriteAllText(outPath,
            JsonSerializer.Serialize(outObj, new JsonSerializerOptions { WriteIndented = false }));
        Console.Error.WriteLine($"\nDONE: {asm.Count} assemblies, {nss.Count} namespaces, {types.Count} types, {files.Count} files");
        return 0;
    }

    static string Rel(string p) =>
        p != null && p.StartsWith(Repo) ? p.Substring(Repo.Length + 1) : p;

    static bool Generated(string file) =>
        file == null || file.Contains("/obj/") || file.Contains("/bin/") || file.Contains("/assets/");

    static void Walk(Microsoft.CodeAnalysis.CodeMetrics.CodeAnalysisMetricData node, string asm, string ns,
        List<object> nss, List<TypeRow> types)
    {
        var sym = node.Symbol;
        if (sym is Microsoft.CodeAnalysis.INamedTypeSymbol t)
        {
            string file = Rel(sym.Locations.FirstOrDefault(l => l.IsInSource)?.SourceTree?.FilePath);
            if (!Generated(file))
            {
                types.Add(new TypeRow
                {
                    assembly = asm, ns = ns, name = t.Name, file = file,
                    mi = node.MaintainabilityIndex, cc = node.CyclomaticComplexity,
                    dit = node.DepthOfInheritance ?? 0, coupling = node.CoupledNamedTypes.Count,
                    sourceLines = node.SourceLines, execLines = node.ExecutableLines,
                    members = node.Children.Count(),
                });
            }
            foreach (var c in node.Children)
                if (c.Symbol is Microsoft.CodeAnalysis.INamedTypeSymbol)
                    Walk(c, asm, ns, nss, types);
            return;
        }

        // assembly or namespace node: recurse into children
        string nsName = ns;
        if (sym is Microsoft.CodeAnalysis.INamespaceSymbol nsSym)
        {
            if (!nsSym.IsGlobalNamespace) nsName = nsSym.ToDisplayString();
            bool hasTypes = node.Children.Any(c => c.Symbol is Microsoft.CodeAnalysis.INamedTypeSymbol);
            if (hasTypes)
                nss.Add(new
                {
                    assembly = asm, ns = nsName == "" ? "<global>" : nsName,
                    types = node.Children.Count(c => c.Symbol is Microsoft.CodeAnalysis.INamedTypeSymbol),
                    mi = node.MaintainabilityIndex, cc = node.CyclomaticComplexity,
                    coupling = node.CoupledNamedTypes.Count, dit = node.DepthOfInheritance ?? 0,
                    sourceLines = node.SourceLines,
                });
        }
        foreach (var c in node.Children) Walk(c, asm, nsName, nss, types);
    }
}

class TypeRow
{
    public string assembly { get; set; }
    public string ns { get; set; }
    public string name { get; set; }
    public string file { get; set; }
    public int mi { get; set; }
    public int cc { get; set; }
    public int dit { get; set; }
    public int coupling { get; set; }
    public long sourceLines { get; set; }
    public long execLines { get; set; }
    public int members { get; set; }
}
