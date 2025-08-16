using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Olve.Engine3D.DebugServer;

internal class WebSocketManager(ILogger<WebSocketManager>? logger)
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();

    public async Task AcceptSocketAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var id = Guid.NewGuid();
        _sockets.TryAdd(id, socket);

        await ReceiveAsync(id, socket);
    }

    private async Task ReceiveAsync(Guid id, WebSocket socket)
    {
        var buffer = new byte[1024];
        try
        {
            logger?.LogInformation("WS Connected: {0}", id);
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }
        catch
        {
            // ignore receive errors
        }
        finally
        {
            _sockets.TryRemove(id, out _);
            logger?.LogInformation("WS Disconnected: {0}", id);
        }
    }

    public async Task BroadcastAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var tasks = _sockets.Values
            .Where(s => s.State == WebSocketState.Open)
            .Select(s => s.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None));

        await Task.WhenAll(tasks);
    }

    public async Task StopAsync()
    {
        var tasks = _sockets.Values.Select(s => s.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None));
        
        await Task.WhenAll(tasks);
    }
}