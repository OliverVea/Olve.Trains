using Olve.Logging;
using Olve.MinimalApi;

namespace Olve.Engine3D.DebugServer;

internal sealed class InMemoryGetLogsHandler(ILoggingManager loggingManager) : IHandler<GetLogsRequest, GetLogsResponse>
{
    public Task<Result<GetLogsResponse>> HandleAsync(GetLogsRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(loggingManager.GetLogs(request));
    }
}
