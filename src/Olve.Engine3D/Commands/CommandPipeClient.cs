using System.IO.Pipes;
using MemoryPack;

namespace Olve.Engine3D.Commands;

public static class CommandPipeClient
{
    public static async Task<Result<CommandResponse>> SendCommand(string pipeName, string command, TimeSpan timeout)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            using var cts = new CancellationTokenSource(timeout);

            await pipe.ConnectAsync(cts.Token);

            var request = new CommandRequest(command);
            var requestBytes = MemoryPackSerializer.Serialize(request);

            var lengthBytes = BitConverter.GetBytes(requestBytes.Length);
            await pipe.WriteAsync(lengthBytes, cts.Token);
            await pipe.WriteAsync(requestBytes, cts.Token);
            await pipe.FlushAsync(cts.Token);

            var responseLengthBytes = new byte[4];
            await pipe.ReadExactlyAsync(responseLengthBytes, cts.Token);
            var responseLength = BitConverter.ToInt32(responseLengthBytes);

            var responseBytes = new byte[responseLength];
            await pipe.ReadExactlyAsync(responseBytes, cts.Token);

            var response = MemoryPackSerializer.Deserialize<CommandResponse>(responseBytes);
            return response ?? new CommandResponse(false, string.Empty, ["Failed to deserialize response"]);
        }
        catch (TimeoutException)
        {
            return new ResultProblem("Timeout connecting to game instance on pipe '{0}'", pipeName);
        }
        catch (OperationCanceledException)
        {
            return new ResultProblem("Timeout connecting to game instance on pipe '{0}'", pipeName);
        }
        catch (IOException ex)
        {
            return new ResultProblem("Failed to connect to game instance: {0}", ex.Message);
        }
    }
}
