using System.Collections.Concurrent;

namespace Olve.Engine3D.Commands;

public class CommandQueue
{
    private readonly ConcurrentQueue<PendingCommand> _queue = new();

    public void Enqueue(string command, TaskCompletionSource<CommandResponse> completionSource)
    {
        _queue.Enqueue(new PendingCommand(command, completionSource));
    }

    public bool TryDequeue(out PendingCommand command)
    {
        return _queue.TryDequeue(out command!);
    }
}

public record PendingCommand(string Command, TaskCompletionSource<CommandResponse> CompletionSource);
