using Microsoft.Extensions.Hosting;
using Olve.Logging;

namespace Olve.Engine3D.DebugServer;

internal class StartupTask(ILoggingManager loggingManager, WebSocketManager webSocketManager) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        loggingManager.LogsUpdatedEvent.Subscribe(BroadcastLogsChanged);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        loggingManager.LogsUpdatedEvent.Unsubscribe(BroadcastLogsChanged);
        await webSocketManager.StopAsync();
    }

    private void BroadcastLogsChanged()
    {
        _ = webSocketManager.BroadcastAsync("NEW_LOG");
    }
}