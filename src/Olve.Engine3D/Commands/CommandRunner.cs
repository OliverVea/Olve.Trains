using Microsoft.Extensions.Logging;
using Olve.Engine3D.Logging;

namespace Olve.Engine3D.Commands;

public class CommandRunner(CommandHandlerServiceCollection commandHandlers, ILogger<CommandRunner> logger) : ICommandRunner
{
    public Result<CommandOutput> Run(RunCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return new ResultProblem("Command cannot be empty");
        }

        if (CommandLineParser.ParseVerbAndArgs(request.Command).TryPickProblems(out var problems, out var verbAndArgs))
        {
            return problems;
        }

        var (verb, args) = verbAndArgs;

        var handler = commandHandlers.FirstOrDefault(h => h.Verb == verb);
        if (handler is null)
        {
            return new ResultProblem("No handler found for verb '{0}'", verb);
        }

        var handlerArgs = handler.Arguments.ToDictionary(x => x.Key);

        foreach (var arg in args.Keys.Where(arg => !handlerArgs.ContainsKey(arg)))
        {
            return new ResultProblem("Command '{0}' does not have an argument '{1}'", verb, arg);
        }

        foreach (var handlerArg in handlerArgs.Values)
        {
            if (handlerArg.Required && !args.ContainsKey(handlerArg.Key))
            {
                return new ResultProblem("Command '{0}' requires the argument '{1}'", verb, handlerArg.Key);
            }
        }

        Result<CommandOutput> lastResult = CommandOutput.Empty;
        for (var i = 0; i < request.Times; i++)
        {
            lastResult = handler.Handle(new CommandContext(args));
            if (lastResult.TryPickProblems(out problems)) return problems;
        }

        logger.LogDebug("Ran '{Command}' successfully", request.Command);

        return lastResult;
    }
}
