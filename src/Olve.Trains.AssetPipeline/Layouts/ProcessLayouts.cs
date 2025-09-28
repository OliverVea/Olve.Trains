using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;
using Olve.Trains.AssetPipeline.Assets;

namespace Olve.Trains.AssetPipeline.Layouts;

public readonly record struct Layout(
    string Tag,
    string? Id,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<Layout> Children);

public readonly record struct TemplateOptions();

public class ProcessLayouts(ILogger<ProcessLayouts> logger, TemplateWriter templateWriter, ShaderOptions shaderOptions) : IAsyncOperation<ProcessLayouts.Request, ProcessLayouts.Response>
{
    public record Request;
    public record Response(IReadOnlyList<Layout> Layouts);

    public Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = new CancellationToken())
    {
        throw new NotImplementedException();
    }
}