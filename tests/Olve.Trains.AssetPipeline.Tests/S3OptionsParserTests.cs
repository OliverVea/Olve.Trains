namespace Olve.Trains.AssetPipeline.Tests;

public class S3OptionsParserTests
{
    [Test]
    public async Task Parse_NoArgs_ReturnsDefaults()
    {
        var (timeout, allowFailure, remaining) = S3OptionsParser.Parse([]);
        await Assert.That(timeout).IsEqualTo(TimeSpan.FromMilliseconds(200));
        await Assert.That(allowFailure).IsFalse();
        await Assert.That(remaining).HasCount().EqualTo(0);
    }

    [Test]
    public async Task Parse_WithTimeoutAndFlag_ReturnsParsedValues()
    {
        var (timeout, allowFailure, remaining) = S3OptionsParser.Parse(["--s3-timeout", "600", "--allow-s3-failure", "--shaders"]);
        await Assert.That(timeout).IsEqualTo(TimeSpan.FromMilliseconds(600));
        await Assert.That(allowFailure).IsTrue();
        await Assert.That(remaining).HasCount().EqualTo(1);
        await Assert.That(remaining[0]).IsEqualTo("--shaders");
    }
}
