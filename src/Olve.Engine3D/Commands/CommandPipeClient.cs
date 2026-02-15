using System.IO.Pipes;
using MemoryPack;

namespace Olve.Engine3D.Commands;

public static class CommandPipeClient
{
    public static async ValueTask<Result<CommandResponse>> SendCommandAsync(string pipeName, string command, CancellationToken cancellationToken)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);

            await pipe.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var request = new CommandRequest(command);
            var requestBytes = MemoryPackSerializer.Serialize(request);

            var lengthBytes = BitConverter.GetBytes(requestBytes.Length);
            await pipe.WriteAsync(lengthBytes, cancellationToken);
            await pipe.WriteAsync(requestBytes, cancellationToken);
            await pipe.FlushAsync(cancellationToken);

            var responseLengthBytes = new byte[4];
            await pipe.ReadExactlyAsync(responseLengthBytes, cancellationToken);
            var responseLength = BitConverter.ToInt32(responseLengthBytes);

            var responseBytes = new byte[responseLength];
            await pipe.ReadExactlyAsync(responseBytes, cancellationToken);

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
