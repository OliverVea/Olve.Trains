using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;
using Envs = (string Bucket, string Key, string Secret);

namespace Olve.Trains.AssetPipeline.Assets;

/// <summary>
///    Downloads assets from an S3 bucket to the temp directory
/// </summary>
/// <param name="logger"></param>
public class DownloadAssets(ILogger<DownloadAssets> logger) : IAsyncOperation<DownloadAssets.Request, DownloadAssets.Response>
{
    private const string S3Bucket = "S3_BUCKET";
    private const string S3Key = "S3_KEY";
    private const string S3Secret = "S3_SECRET";

    public record Request(TimeSpan InitialTimeout, bool AllowFailure);
    public record Response(IReadOnlyList<FileInfo> Files);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Getting S3 configuration from environment variables");

        var environmentVariableResult = ReadS3EnvironmentVariables();
        if (environmentVariableResult.TryPickProblems(out var problems, out var envVariables))
        {
            if (request.AllowFailure)
            {
                logger.LogWarning("Could not get S3 configuration: {Problems}", problems);
                return new Response([]);
            }

            return problems.Prepend("Could not get S3 configuration");
        }

        logger.LogInformation("Got configuration - Bucket: {Bucket}", envVariables.Bucket);

        var retrievalResult = await RetrieveS3BucketAsync(envVariables, request.InitialTimeout, ct);
        if (retrievalResult.TryPickProblems(out var retrievalProblems, out var files))
        {
            if (request.AllowFailure)
            {
                logger.LogWarning("Failed to retrieve S3 bucket: {Problems}", retrievalProblems);
                return new Response([]);
            }

            return retrievalProblems.Prepend("Failed to retrieve S3 bucket");
        }

        var itemCount = Directory.GetFiles(Paths.TempFolder, "*", SearchOption.AllDirectories).Length;

        logger.LogInformation("Retrieved {ItemCount} items from S3 bucket", itemCount);

        return new Response(files);
    }

    private Result<Envs> ReadS3EnvironmentVariables()
    {
        return Result.Concat(
            EnvHelper.ReadEnvVariable(S3Bucket),
            EnvHelper.ReadEnvVariable(S3Key),
            EnvHelper.ReadEnvVariable(S3Secret)
        );
    }

    private async Task<Result<List<FileInfo>>> RetrieveS3BucketAsync(Envs envs, TimeSpan initialTimeout, CancellationToken ct)
    {
        try
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.APSoutheast2
            };
            using var s3Client = new AmazonS3Client(envs.Key, envs.Secret, config);
            
            var listRequest = new ListObjectsV2Request { BucketName = envs.Bucket };

            using var initialTimeoutCts = new CancellationTokenSource(initialTimeout);
            using var combinedInitialCts = CancellationTokenSource.CreateLinkedTokenSource(ct, initialTimeoutCts.Token);

            ListObjectsV2Response listResponse;
            try
            {
                listResponse = await s3Client.ListObjectsV2Async(listRequest, combinedInitialCts.Token);
            }
            catch (OperationCanceledException) when (initialTimeoutCts.IsCancellationRequested)
            {
                return new ResultProblem("Timed out after {0}ms when listing objects in bucket '{1}'", initialTimeout.TotalMilliseconds, envs.Bucket);
            }

            if ((listResponse.S3Objects?.Count ?? 0) == 0)
            {
                return new ResultProblem("No objects found in the S3 bucket '{0}'", envs.Bucket);
            }

            Directory.CreateDirectory(Paths.TempFolder);

            List<FileInfo> files = [];

            foreach (var s3Object in listResponse.S3Objects ?? [])
            {
                logger.LogDebug("Retrieving object '{0}' from S3 bucket '{1}'", s3Object.Key, envs.Bucket);

                var destFilePath = Path.Combine(Paths.TempFolder, s3Object.Key);
                var destDirectory = Path.GetDirectoryName(destFilePath);

                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory!);
                }

                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var timeoutCt = timeoutCts.Token;

                var combinedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCt).Token;

                var getRequest = new GetObjectRequest { BucketName = envs.Bucket, Key = s3Object.Key };
                using var getResponse = await s3Client.GetObjectAsync(getRequest, combinedCt);
                if (getResponse.HttpStatusCode > (HttpStatusCode)399)
                {
                    return new ResultProblem("Got status code '{0}' while retrieving object '{1}' from s3 bucket '{2}'", getResponse.HttpStatusCode, s3Object.Key, envs.Bucket);
                }

                await using var responseStream = getResponse.ResponseStream;
                await using var fileStream = File.Create(destFilePath);

                await responseStream.CopyToAsync(fileStream, ct);

                files.Add(new FileInfo(destFilePath));

                logger.LogDebug("Retrieved object '{0}' from S3 bucket '{1}'", s3Object.Key, envs.Bucket);
            }

            return files;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogDebug(ex, "Amazon Id: {AmazonId}, Cloudfront Id: {CloudfrontId}, Response body: {ResponseBody}, Message: {Message}", ex.AmazonId2, ex.AmazonCloudFrontId ,ex.ResponseBody, ex.Message);

            return new ResultProblem(ex, "Failed to retrieve S3 bucket '{0}'", envs.Bucket);
        }
        catch (Exception ex)
        {
            return new ResultProblem(ex, "Failed to retrieve S3 bucket '{0}', Message: {1}", envs.Bucket,  ex.Message);
        }
    }
}

