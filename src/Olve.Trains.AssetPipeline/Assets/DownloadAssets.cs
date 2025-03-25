using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;

namespace Olve.Trains.AssetPipeline.Assets;

/// <summary>
///    Downloads assets from an S3 bucket to the temp directory
/// </summary>
/// <param name="logger"></param>
public class DownloadAssets(ILogger<DownloadAssets> logger) : IAsyncOperation<DownloadAssets.Request, DownloadAssets.Response>
{
    private const string S3Url = "S3_URL";
    private const string S3Bucket = "S3_BUCKET";
    private const string S3Key = "S3_KEY";
    private const string S3Secret = "S3_SECRET";

    public record Request;
    public record Response(IReadOnlyList<FileInfo> Files);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Getting S3 configuration from environment variables");

        var environmentVariableResult = ReadS3EnvironmentVariables();
        if (environmentVariableResult.TryPickProblems(out var problems, out var envVariables))
        {
            return problems.Prepend("Could not get S3 configuration");
        }

        var (url, bucket, key, secret) = envVariables;

        logger.LogInformation("Got configuration - Url: {Url}, Bucket: {Bucket}", url, bucket);

        var retrievalResult = await RetrieveS3BucketAsync(url, bucket, key, secret, ct);
        if (retrievalResult.TryPickProblems(out var retrievalProblems, out var files))
        {
            return retrievalProblems.Prepend("Failed to retrieve S3 bucket");
        }

        var itemCount = Directory.GetFiles(Paths.TempFolder, "*", SearchOption.AllDirectories).Length;

        logger.LogInformation("Retrieved {ItemCount} items from S3 bucket", itemCount);

        return new Response(files);
    }

    private Result<(string Url, string Bucket, string Key, string Secret)> ReadS3EnvironmentVariables()
    {

        return Result.Concat(
            () => EnvHelper.ReadEnvVariable(S3Url),
            () => EnvHelper.ReadEnvVariable(S3Bucket),
            () => EnvHelper.ReadEnvVariable(S3Key),
            () => EnvHelper.ReadEnvVariable(S3Secret)
        );
    }

    private async Task<Result<List<FileInfo>>> RetrieveS3BucketAsync(string url, string bucket, string key, string secret, CancellationToken ct)
    {
        try
        {
            var config = new AmazonS3Config { ServiceURL = url, ForcePathStyle = true };
            using var s3Client = new AmazonS3Client(key, secret, config);
            var listRequest = new ListObjectsV2Request { BucketName = bucket };
            var listResponse = await s3Client.ListObjectsV2Async(listRequest, ct);

            if (listResponse.S3Objects.Count == 0)
            {
                return new ResultProblem("No objects found in the S3 bucket '{0}' at '{1}'", bucket, url);
            }

            Directory.CreateDirectory(Paths.TempFolder);

            List<FileInfo> files = new();

            foreach (var s3Object in listResponse.S3Objects)
            {
                logger.LogDebug("Retrieving object '{0}' from S3 bucket '{1}' at '{2}'", s3Object.Key, bucket, url);

                var destFilePath = Path.Combine(Paths.TempFolder, s3Object.Key);
                var destDirectory = Path.GetDirectoryName(destFilePath);

                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory!);
                }

                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var timeoutCt = timeoutCts.Token;

                var combinedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCt).Token;

                var getRequest = new GetObjectRequest { BucketName = bucket, Key = s3Object.Key };
                using var getResponse = await s3Client.GetObjectAsync(getRequest, combinedCt);
                if (getResponse.HttpStatusCode > (HttpStatusCode)399)
                {
                    return new ResultProblem("Got status code '{0}' while retrieving object '{1}' from s3 bucket '{2}' at '{3}'", getResponse.HttpStatusCode, s3Object.Key, bucket, url);
                }

                await using var responseStream = getResponse.ResponseStream;
                await using var fileStream = File.Create(destFilePath);

                await responseStream.CopyToAsync(fileStream, ct);

                files.Add(new FileInfo(destFilePath));

                logger.LogDebug("Retrieved object '{0}' from S3 bucket '{1}' at '{2}'", s3Object.Key, bucket, url);
            }

            return files;
        }
        catch (Exception ex)
        {
            return new ResultProblem(ex, "Failed to retrieve S3 bucket '{0}' at '{1}'", bucket, url);
        }
    }
}

