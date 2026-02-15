using Microsoft.Extensions.Logging;

namespace Olve.Engine3D.Commands;

public class ScreenshotCommandHandler(ScreenshotManager screenshotManager, ILogger<ScreenshotCommandHandler> logger) : ICommandHandler
{
    private const string PathKey = "path";

    public string Verb => "screenshot";
    public string HelpString => "Captures a screenshot and saves it to the specified path";
    public IReadOnlyList<CommandArgument> Arguments { get; } = [
        new(PathKey, "output file path for the screenshot", true)
    ];

    public Result<CommandOutput> Handle(CommandContext context)
    {
        if (!context.Arguments.TryGetValue(PathKey, out var path) || string.IsNullOrWhiteSpace(path))
        {
            return new ResultProblem("path argument is required");
        }

        screenshotManager.RequestScreenshot(path);
        logger.LogInformation("Screenshot requested: {Path}", path);

        return new CommandOutput($"Screenshot queued: {path}");
    }
}
