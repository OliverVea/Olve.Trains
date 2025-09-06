namespace Olve.Trains.AssetPipeline.Tests;

public class BuildTargetsTests
{
    [Test]
    public async Task RequiresS3Resources_Works()
    {
        await Assert.That(BuildTargets.Shaders.RequiresS3Resources()).IsFalse();
        await Assert.That((BuildTargets.Meshes | BuildTargets.Textures).RequiresS3Resources()).IsTrue();
    }

    [Test]
    public async Task ParseArgs_ReturnsExpectedTargets()
    {
        var parsed = BuildTargetParser.Parse(["--shaders", "--textures"]);
        await Assert.That(parsed.HasFlag(BuildTargets.Shaders)).IsTrue();
        await Assert.That(parsed.HasFlag(BuildTargets.Textures)).IsTrue();
        await Assert.That(parsed.HasFlag(BuildTargets.Meshes)).IsFalse();
    }

    [Test]
    public async Task ParseArgs_AllDefaultsToAll()
    {
        var parsed = BuildTargetParser.Parse([]);
        await Assert.That(parsed).IsEqualTo(BuildTargets.All);
    }
}
