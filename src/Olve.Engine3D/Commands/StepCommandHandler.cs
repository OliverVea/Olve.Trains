using Microsoft.Extensions.Logging;
using Olve.Engine3D.TimeStepping;

namespace Olve.Engine3D.Commands;

public class StepCommandHandler(ManualStepper manualStepper, ILogger<StepCommandHandler> logger) : ICommandHandler
{
    private const string FramesKey = "frames";

    public string Verb => "step";
    public string HelpString => "Advances the simulation by the specified number of frames (default 1)";
    public IReadOnlyList<CommandArgument> Arguments { get; } =
    [
        new(FramesKey, "number of frames to step (default 1)"),
    ];

    public Result<CommandOutput> Handle(CommandContext commandContext)
    {
        ulong frames = 1;

        if (commandContext.Arguments.TryGetValue(FramesKey, out var framesStr))
        {
            if (!ulong.TryParse(framesStr, out frames) || frames == 0)
            {
                return new ResultProblem("Invalid frames value '{0}', must be a positive integer", framesStr);
            }
        }

        manualStepper.Step(frames);
        logger.LogInformation("Stepped {Frames} frame(s)", frames);

        return new CommandOutput($"Stepped {frames} frame(s)");
    }
}
