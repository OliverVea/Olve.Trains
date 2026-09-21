namespace Olve.Engine3D;

public static class ProblemSeverityExtensions
{
    public static bool AnyCritical(this ResultProblemCollection problems) =>
        problems.Any(p => p.Severity >= ProblemSeverities.Critical);
}
