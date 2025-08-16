namespace Olve.Engine3D.DebugServer.Commands;

internal interface ICommandRunner
{
    public Result Run(RunCommandRequest request);
}