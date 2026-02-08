using Olve.Logging;

namespace Olve.Engine3D.Commands;

public class CommandRunner(IEnumerable<ICommandHandler> commandHandlers, ILoggingManager loggingManager) : ICommandRunner
{
    private readonly IReadOnlyDictionary<string, ICommandHandler> _commandHandlers = commandHandlers.ToDictionary(h => h.Verb);

    public Result Run(RunCommandRequest request)
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

        if (!_commandHandlers.TryGetValue(verb, out var handler))
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

        for (var i = 0; i < request.Times; i++)
        {
            var result = handler.Handle(new CommandContext(args));
            if (result.TryPickProblems(out problems)) return problems;
        }

        loggingManager.Log(LogLevel.Debug, "Ran '" + request.Command + "' successfully");

        return Result.Success();
    }
}
