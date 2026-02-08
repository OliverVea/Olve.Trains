namespace Olve.Engine3D.Commands;

public readonly record struct RunCommandRequest(string Command, int Times = 1);
