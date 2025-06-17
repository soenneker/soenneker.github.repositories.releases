using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.GitHub.ClientUtil.Abstract;
using Soenneker.GitHub.OpenApiClient;
using Soenneker.GitHub.OpenApiClient.Models;
using Soenneker.GitHub.OpenApiClient.Repos.Item.Item.Releases;
using Soenneker.GitHub.Repositories.Releases.Abstract;
using Soenneker.GitHub.Repositories.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GitHub.Repositories.Releases;

///<inheritdoc cref="IGitHubRepositoriesReleasesUtil"/>
public sealed class GitHubRepositoriesReleasesUtil : IGitHubRepositoriesReleasesUtil
{
    private readonly IGitHubOpenApiClientUtil _gitHubOpenApiClientUtil;
    private readonly IGitHubRepositoriesTagsUtil _tagsUtil;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitHubRepositoriesReleasesUtil> _logger;

    public GitHubRepositoriesReleasesUtil(ILogger<GitHubRepositoriesReleasesUtil> logger, IGitHubOpenApiClientUtil gitHubOpenApiClientUtil,
        IGitHubRepositoriesTagsUtil tagsUtil, IConfiguration configuration)
    {
        _logger = logger;
        _gitHubOpenApiClientUtil = gitHubOpenApiClientUtil;
        _tagsUtil = tagsUtil;
        _configuration = configuration;
    }

    public async ValueTask Create(string owner, string repo, string tagName, string releaseName, string releaseBody, string filePath, bool isDraft = false,
        bool isPrerelease = false, CancellationToken cancellationToken = default)
    {
        try
        {
            bool tagExists = await _tagsUtil.DoesTagExist(owner, repo, tagName, cancellationToken).NoSync();

            if (!tagExists)
            {
                await _tagsUtil.Create(owner, repo, tagName, cancellationToken).NoSync();
                _logger.LogInformation("Tag '{TagName}' created successfully.", tagName);
            }
            else
            {
                _logger.LogInformation("Tag '{TagName}' already exists. Skipping tag creation.", tagName);
            }

            GitHubOpenApiClient client = await _gitHubOpenApiClientUtil.Get(cancellationToken).NoSync();

            var releaseRequest = new ReleasesPostRequestBody
            {
                TagName = tagName,
                Name = releaseName,
                Body = releaseBody,
                Draft = isDraft,
                Prerelease = isPrerelease
            };

            Release? release = await client.Repos[owner][repo].Releases.PostAsync(releaseRequest, cancellationToken: cancellationToken).NoSync();
            _logger.LogInformation("Release '{ReleaseName}' created successfully.", release.Name);

            await UploadAsset(owner, repo, release.Id.Value, filePath, cancellationToken).NoSync();
            _logger.LogInformation("Executable '{FileName}' uploaded successfully.", Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
            throw;
        }
    }

    // Can't use Kiota here because of the base address overloading..
    public async ValueTask UploadAsset(string owner, string repo, long releaseId, string filePath, CancellationToken cancellationToken = default)
    {
        var uploadUrl = $"https://uploads.github.com/repos/{owner}/{repo}/releases/{releaseId}/assets?name={Uri.EscapeDataString(Path.GetFileName(filePath))}";

        if (!File.Exists(filePath))
        {
            _logger.LogError("UploadAsset: File not found at path '{FilePath}'", filePath);
            throw new FileNotFoundException("Upload file not found.", filePath);
        }

        _logger.LogInformation("UploadAsset: Starting upload for file '{FileName}' to release {ReleaseId} in {Owner}/{Repo}", Path.GetFileName(filePath), releaseId, owner, repo);

        await using FileStream fileStream = File.OpenRead(filePath);
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        request.Content = content;

        var token = _configuration.GetValueStrict<string>("GH:TOKEN");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd(Guid.NewGuid().ToString());

        using var httpClient = new HttpClient();

        try
        {
            HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("UploadAsset: Upload failed with status {StatusCode} - {ReasonPhrase}. Response body: {Body}", response.StatusCode, response.ReasonPhrase, responseBody);
                response.EnsureSuccessStatusCode(); // Still throw for stack trace
            }

            _logger.LogInformation("UploadAsset: Upload successful for '{FileName}'", Path.GetFileName(filePath));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "UploadAsset: HttpRequestException while uploading to GitHub. URL: {UploadUrl}", uploadUrl);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadAsset: Unexpected error during upload.");
            throw;
        }
    }

    public async ValueTask Delete(string owner, string repo, string tagName, bool deleteTag = true, CancellationToken cancellationToken = default)
    {
        try
        {
            GitHubOpenApiClient client = await _gitHubOpenApiClientUtil.Get(cancellationToken).NoSync();

            Release? release = await Get(owner, repo, tagName, cancellationToken).NoSync();

            if (release == null)
                return;

            await client.Repos[owner][repo].Releases[release.Id.Value].DeleteAsync(cancellationToken: cancellationToken).NoSync();
            _logger.LogInformation("Release with tag '{TagName}' deleted successfully.", tagName);

            if (deleteTag)
            {
                bool tagExists = await _tagsUtil.DoesTagExist(owner, repo, tagName, cancellationToken).NoSync();

                if (tagExists)
                {
                    await _tagsUtil.Delete(owner, repo, tagName, cancellationToken).NoSync();
                    _logger.LogInformation("Tag '{TagName}' deleted successfully.", tagName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the release: {Message}", ex.Message);
            throw;
        }
    }

    public async ValueTask<Release?> Get(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Release> releases = await GetAll(owner, repo, cancellationToken).NoSync();

        Release? release = releases.FirstOrDefault(r => r.TagName == tagName);

        if (release == null)
        {
            _logger.LogWarning("No release found for tag '{TagName}' in repository '{Repo}'.", tagName, repo);
            return null;
        }

        _logger.LogInformation("Successfully retrieved release '{ReleaseName}' with tag '{TagName}'.", release.Name, tagName);
        return release;
    }

    public async ValueTask<IReadOnlyList<Release>> GetAll(string owner, string repo, CancellationToken cancellationToken = default)
    {
        try
        {
            GitHubOpenApiClient client = await _gitHubOpenApiClientUtil.Get(cancellationToken).NoSync();

            List<Release>? releases = await client.Repos[owner][repo].Releases.GetAsync(cancellationToken: cancellationToken).NoSync();

            _logger.LogInformation("Successfully retrieved {Count} releases for repository '{Repo}'.", releases.Count, repo);

            return releases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving releases for repository '{Repo}': {Message}", repo, ex.Message);
            throw;
        }
    }
}