namespace Olve.Engine3D.Commands;

public interface ICommandRunner
{
    Result Run(RunCommandRequest request);
}
