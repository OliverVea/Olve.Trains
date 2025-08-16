using Olve.Logging;
using Olve.MinimalApi;

namespace Olve.Engine3D.DebugServer;

public class LoggingHandler(ILoggingManager loggingManager) : IHandler<GetLogsRequest, GetLogsResponse>
{
    public Task<Result<GetLogsResponse>> HandleAsync(GetLogsRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(loggingManager.GetLogs(request));
    }
}