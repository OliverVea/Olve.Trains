using Microsoft.Extensions.Logging;
using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Scenes;

namespace Olve.Engine3D.Commands;

public class CommandProcessingService(
    CommandQueue commandQueue,
    CommandRunner commandRunner,
    ILogger<CommandProcessingService> logger) : ISceneService
{
    public int Priority => -1000;

    public Result Update()
    {
        while (commandQueue.TryDequeue(out var pendingCommand))
        {
            try
            {
                var result = commandRunner.Run(new RunCommandRequest(pendingCommand.Command));

                if (EngineMetrics.IsEnabled) EngineMetrics.CommandsProcessed.Add(1);

                if (result.TryPickProblems(out var problems, out var output))
                {
                    var errors = problems.Select(p => p.ToDebugString()).ToArray();
                    logger.LogWarning("Command '{Command}' failed: {Errors}", pendingCommand.Command, string.Join("; ", errors));
                    pendingCommand.CompletionSource.SetResult(new CommandResponse(false, string.Empty, errors));
                }
                else
                {
                    pendingCommand.CompletionSource.SetResult(new CommandResponse(true, output.Text, []));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception executing command '{Command}'", pendingCommand.Command);
                pendingCommand.CompletionSource.SetResult(new CommandResponse(false, string.Empty, [ex.Message]));
            }
        }

        return Result.Success();
    }
}
