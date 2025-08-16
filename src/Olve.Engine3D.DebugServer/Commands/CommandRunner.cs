using Olve.Logging;
using Olve.MinimalApi;
using Olve.Validation.Validators;

namespace Olve.Engine3D.DebugServer.Commands;

internal class CommandRunner(IEnumerable<ICommandHandler> commandHandlers, ILoggingManager loggingManager) : ICommandRunner, IHandler<RunCommandRequest>
{
    private readonly IReadOnlyDictionary<string, ICommandHandler> _commandHandlers = commandHandlers.ToDictionary(h => h.Verb);
    
    private static readonly StringValidator CommandValidator = new StringValidator().CannotBeNullOrWhiteSpace().WithMessage("command cannot be empty");

    public Task<Result> RunAsync(RunCommandRequest request, CancellationToken cancellationToken)
    {
        var result = Run(request);
        if (result.TryPickProblems(out var problems))
        {
            problems = problems.Prepend("Failed to run command '{0}'", request.Command);
            loggingManager.Log(problems);
        }
        else
        {
            loggingManager.Log(LogLevel.Debug, "Ran '" + request.Command + "' successfully");
        }
        
        return Task.FromResult(result);
    }
    
    public Result Run(RunCommandRequest request)
    {
        if (CommandValidator.Validate(request.Command).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to validate command '{0}'", request.Command);
        }

        if (CommandLineParser.ParseVerbAndArgs(request.Command).TryPickProblems(out problems, out var verbAndArgs))
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
                return new ResultProblem("Command '{0}' does requires the argument '{1}'", verb, handlerArg.Key);
            }
        }

        for (var i = 0; i < request.Times; i++)
        {
            var result = handler.Handle(new CommandContext(args));
            if (result.TryPickProblems(out problems)) return problems;
        }

        return Result.Success();
    }
}