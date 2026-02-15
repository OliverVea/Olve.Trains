namespace Olve.Engine3D.Commands;

public interface ICommandRunner
{
    Result<CommandOutput> Run(RunCommandRequest request);
}
