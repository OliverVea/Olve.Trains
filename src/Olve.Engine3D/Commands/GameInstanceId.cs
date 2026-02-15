namespace Olve.Engine3D.Commands;

public class GameInstanceId
{
    public string Id { get; }

    public GameInstanceId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var random = Guid.NewGuid().ToString("N")[..6];
        Id = $"{timestamp}-{random}";
    }

    public GameInstanceId(string id)
    {
        Id = id;
    }

    public string GetPipeName() => $"olve-trains-{Id}";

    public static string GetDefaultPipeName() => "olve-trains-default";
}
