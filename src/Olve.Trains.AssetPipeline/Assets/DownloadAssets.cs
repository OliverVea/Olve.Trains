using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;
using Envs = (string Url, string Bucket, string Key, string Secret, string CloudflareId, string CloudflareSecret);

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
    private const string CloudflareId = "CF_CLIENT_ID";
    private const string CloudflareSecret = "CF_CLIENT_SECRET";

    private const string CfClientIdHeader = "CF-Access-Client-Id";
    private const string CfClientSecretHeader = "CF-Access-Client-Secret";

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

        logger.LogInformation("Got configuration - Url: {Url}, Bucket: {Bucket}", envVariables.Url, envVariables.Bucket);

        var retrievalResult = await RetrieveS3BucketAsync(envVariables, ct);
        if (retrievalResult.TryPickProblems(out var retrievalProblems, out var files))
        {
            return retrievalProblems.Prepend("Failed to retrieve S3 bucket");
        }

        var itemCount = Directory.GetFiles(Paths.TempFolder, "*", SearchOption.AllDirectories).Length;

        logger.LogInformation("Retrieved {ItemCount} items from S3 bucket", itemCount);

        return new Response(files);
    }

    private Result<(string Url, string Bucket, string Key, string Secret, string CloudflareId, string CloudflareSecret)> ReadS3EnvironmentVariables()
    {

        return Result.Concat(
            () => EnvHelper.ReadEnvVariable(S3Url),
            () => EnvHelper.ReadEnvVariable(S3Bucket),
            () => EnvHelper.ReadEnvVariable(S3Key),
            () => EnvHelper.ReadEnvVariable(S3Secret),
            () => EnvHelper.ReadEnvVariable(CloudflareId),
            () => EnvHelper.ReadEnvVariable(CloudflareSecret)
        );
    }

    private async Task<Result<string>> GetCloudflareCookieAsync(Envs envs, CancellationToken ct)
    {
        var httpClient = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"{envs.Url}/minio/health/live");
        request.Headers.Add(CfClientIdHeader, envs.CloudflareId);
        request.Headers.Add(CfClientSecretHeader, envs.CloudflareSecret);

        var response = await httpClient.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return new ResultProblem("Failed to get Cloudflare cookie");
        }

        var cookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        if (cookie is null)
        {
            return new ResultProblem("Failed to get Cloudflare cookie");
        }

        logger.LogDebug("Received Cloudflare cookie: {Cookie}", cookie);

        return cookie;
    }

    private async Task<Result<List<FileInfo>>> RetrieveS3BucketAsync(Envs envs, CancellationToken ct)
    {
        var cookieResult = await GetCloudflareCookieAsync(envs, ct);
        if (cookieResult.TryPickProblems(out var cookieProblems, out var cookie))
        {
            return cookieProblems.Prepend("Failed to retrieve Cloudflare cookie");
        }

        try
        {
            var config = new AmazonS3Config { ServiceURL = envs.Url, ForcePathStyle = true };
            using var s3Client = new AmazonS3Client(envs.Key, envs.Secret, config);

            s3Client.BeforeRequestEvent += (_, args) =>
            {
                if (args is WebServiceRequestEventArgs { Headers: not null } wsArgs)
                {
                    wsArgs.Headers.Add("Cookie", cookie);

                    logger.LogDebug("Received web service request header: {Header}", wsArgs.Headers);
                }
                else if (args is HeadersRequestEventArgs { Headers: not null } headersArgs)
                {
                    headersArgs.Headers.Add("Cookie", cookie);

                    logger.LogDebug("Received headers request header: {Header}", headersArgs.Headers);
                }
            };
            
            var listRequest = new ListObjectsV2Request { BucketName = envs.Bucket };
            var listResponse = await s3Client.ListObjectsV2Async(listRequest, ct);

            if (listResponse.S3Objects.Count == 0)
            {
                return new ResultProblem("No objects found in the S3 bucket '{0}' at '{1}'", envs.Bucket, envs.Url);
            }

            Directory.CreateDirectory(Paths.TempFolder);

            List<FileInfo> files = new();

            foreach (var s3Object in listResponse.S3Objects)
            {
                logger.LogDebug("Retrieving object '{0}' from S3 bucket '{1}' at '{2}'", s3Object.Key, envs.Bucket, envs.Url);

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
                    return new ResultProblem("Got status code '{0}' while retrieving object '{1}' from s3 bucket '{2}' at '{3}'", getResponse.HttpStatusCode, s3Object.Key, envs.Bucket, envs.Url);
                }

                await using var responseStream = getResponse.ResponseStream;
                await using var fileStream = File.Create(destFilePath);

                await responseStream.CopyToAsync(fileStream, ct);

                files.Add(new FileInfo(destFilePath));

                logger.LogDebug("Retrieved object '{0}' from S3 bucket '{1}' at '{2}'", s3Object.Key, envs.Bucket, envs.Url);
            }

            return files;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogInformation("Amazon Id: {AmazonId}, Cloudfront Id: {CloudfrontId}, Response body: {ResponseBody}", ex.AmazonId2, ex.AmazonCloudFrontId ,ex.ResponseBody);

            return new ResultProblem(ex, "Failed to retrieve S3 bucket '{0}' at '{1}'", envs.Bucket, envs.Url);
        }
        catch (Exception ex)
        {
            return new ResultProblem(ex, "Failed to retrieve S3 bucket '{0}' at '{1}'", envs.Bucket, envs.Url);
        }
    }
}

