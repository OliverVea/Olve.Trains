using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olve.Operations;
using Olve.Paths.Glob;
using Olve.Trains.AssetPipeline.Options;
namespace Olve.Trains.AssetPipeline.Assets;

/// <summary>
///    Downloads assets from an S3 bucket to the temp directory
/// </summary>
/// <param name="logger"></param>
/// <param name="s3Options"></param>
public class DownloadAssets(ILogger<DownloadAssets> logger, IOptions<S3Options> s3Options, PathProvider pathProvider) : IAsyncOperation<DownloadAssets.Request, DownloadAssets.Response>
{
    public record Request(TimeSpan InitialTimeout, bool AllowFailure);
    public record Response(IReadOnlyList<FileInfo> Files);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Getting S3 configuration");

        if (string.IsNullOrWhiteSpace(s3Options.Value.Bucket) || string.IsNullOrWhiteSpace(s3Options.Value.Prefix) || string.IsNullOrWhiteSpace(s3Options.Value.Key) || string.IsNullOrWhiteSpace(s3Options.Value.Secret))
        {
            var problem = new ResultProblem("S3 configuration is incomplete. Ensure Bucket, Key, and Secret are configured.");
            if (!request.AllowFailure)
            {
                return problem;
            }

            logger.LogWarning("Could not get S3 configuration: {Problems}", problem);
            return new Response([]);

        }

        logger.LogInformation("Got configuration - Bucket: {Bucket}", s3Options.Value.Bucket);

        var retrievalResult = await RetrieveS3BucketAsync(s3Options.Value.Bucket, s3Options.Value.Prefix, s3Options.Value.Key, s3Options.Value.Secret, request.InitialTimeout, ct);
        if (retrievalResult.TryPickProblems(out var retrievalProblems, out var files))
        {
            if (!request.AllowFailure)
            {
                return retrievalProblems.Prepend("Failed to retrieve S3 bucket");
            }

            logger.LogWarning("Failed to retrieve S3 bucket: {Problems}", retrievalProblems);
            return new Response([]);

        }

        var itemCount = pathProvider.BuildS3CachePath.TryGlob("**/*", out var hits) ? hits?.Count(x => x.ElementType == ElementType.File) ?? 0 : 0;

        logger.LogInformation("Retrieved {ItemCount} items from S3 bucket", itemCount);

        return new Response(files);
    }

    private async Task<Result<List<FileInfo>>> RetrieveS3BucketAsync(string bucket, string prefix, string key, string secret, TimeSpan initialTimeout, CancellationToken ct)
    {
        try
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.APSoutheast2
            };
            using var s3Client = new AmazonS3Client(key, secret, config);

            var listRequest = new ListObjectsV2Request { BucketName = bucket, Prefix = prefix};

            using var initialTimeoutCts = new CancellationTokenSource(initialTimeout);
            using var combinedInitialCts = CancellationTokenSource.CreateLinkedTokenSource(ct, initialTimeoutCts.Token);

            ListObjectsV2Response listResponse;
            try
            {
                listResponse = await s3Client.ListObjectsV2Async(listRequest, combinedInitialCts.Token);
            }
            catch (OperationCanceledException) when (initialTimeoutCts.IsCancellationRequested)
            {
                return new ResultProblem("Timed out after {0}ms when listing objects in bucket '{1}'", initialTimeout.TotalMilliseconds, bucket);
            }

            if ((listResponse.S3Objects?.Count ?? 0) == 0)
            {
                return new ResultProblem("No objects found in the S3 bucket '{0}'", bucket);
            }

            pathProvider.BuildS3CachePath.EnsurePathExists();

            List<FileInfo> files = [];

            foreach (var s3Object in listResponse.S3Objects ?? [])
            {
                var relativePath = s3Object.Key[prefix.Length..];
                if (string.IsNullOrEmpty(relativePath) || relativePath.EndsWith('/'))
                {
                    logger.LogDebug("Skipping folder marker '{0}'", s3Object.Key);
                    continue;
                }

                logger.LogDebug("Retrieving object '{0}' from S3 bucket '{1}'", s3Object.Key, bucket);

                var destPath = pathProvider.BuildS3CachePath / relativePath;

                destPath.Parent.EnsurePathExists();

                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var timeoutCt = timeoutCts.Token;

                var combinedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCt).Token;

                var getRequest = new GetObjectRequest { BucketName = bucket, Key = s3Object.Key };
                using var getResponse = await s3Client.GetObjectAsync(getRequest, combinedCt);
                if (getResponse.HttpStatusCode > (HttpStatusCode)399)
                {
                    return new ResultProblem("Got status code '{0}' while retrieving object '{1}' from s3 bucket '{2}'", getResponse.HttpStatusCode, s3Object.Key, bucket);
                }

                await using var responseStream = getResponse.ResponseStream;
                await using var fileStream = File.Create(destPath.Path);

                await responseStream.CopyToAsync(fileStream, CancellationToken.None);

                files.Add(new FileInfo(destPath.Path));

                logger.LogDebug("Retrieved object '{0}' from S3 bucket '{1}'", s3Object.Key, bucket);
            }

            return files;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogDebug(ex, "Amazon Id: {AmazonId}, Cloudfront Id: {CloudfrontId}, Response body: {ResponseBody}, Message: {Message}", ex.AmazonId2, ex.AmazonCloudFrontId ,ex.ResponseBody, ex.Message);

            return new ResultProblem(ex, "Failed to retrieve S3 bucket '{0}'", bucket);
        }
        catch (Exception ex)
        {
            return new ResultProblem(ex, "Failed to retrieve S3 bucket '{0}', Message: {1}", bucket,  ex.Message);
        }
    }
}

