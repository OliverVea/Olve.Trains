using Olve.Engine3D;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Results;

namespace Olve.Trains.Scenes.Console;

public class ConsoleCommandService
{
    public Result Execute(string command)
    {
        var args = command.Split(' ');
        if (args.Length == 0)
        {
            return new ResultProblem("No command provided");
        }

        var verb = args[0];
        var parameters = args[1..];

        return verb switch
        {
            "echo" => Echo(parameters),
            "set" => Set(parameters),
            _ => new ResultProblem($"Unknown command: {verb}")
        };
    }

    private Result Echo(string[] parameters)
    {
        if (parameters.Length == 0)
        {
            return new ResultProblem("No message provided");
        }

        var message = string.Join(' ', parameters);
        GameManager.LoggingManager.Log(LogLevel.Info, message);

        return Result.Success();
    }

    private Result Set(string[] parameters)
    {
        if (parameters.Length != 2)
        {
            return new ResultProblem("Invalid number of parameters");
        }

        var (unit, amount) = (parameters[0], parameters[1]);
        
        if (!float.TryParse(amount, out var value))
        {
            return new ResultProblem("Amount must be a number");
        }
        
        return unit switch
        {
            "time" => SetTime(value),
            "timescale" => SetTimeScale(value),
            _ => new ResultProblem($"Unknown unit: {unit}")
        };
    }

    private Result SetTimeScale(float result)
    {
        if (result <= 0)
        {
            return new ResultProblem("Timescale must be greater than 0");
        }

        GameManager.DayTimeManager.DayLength = DayTimeManager.DefaultDayLength / result;

        return Result.Success();
    }

    private Result SetTime(float result)
    {
        var hours = (int)result;
        var minutes = (int)((result - hours) * 60);

        GameManager.DayTimeManager.CurrentTime = new DayTime(hours, minutes);

        return Result.Success();
    }
}