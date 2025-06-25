using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Configuration;
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

    public async ValueTask UploadAsset(string owner, string repo, long releaseId, string filePath, CancellationToken cancellationToken = default)
    {
        string uploadUrl =
            $"https://uploads.github.com/repos/{owner}/{repo}/releases/{releaseId}/assets?name={Uri.EscapeDataString(Path.GetFileName(filePath))}";

        if (!File.Exists(filePath))
        {
            _logger.LogError("UploadAsset: File not found at path '{FilePath}'", filePath);
            throw new FileNotFoundException("Upload file not found.", filePath);
        }

        _logger.LogInformation("UploadAsset: Starting upload for file '{FileName}'", Path.GetFileName(filePath));

        await using var fileStream = File.OpenRead(filePath);
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
        {
            Content = content
        };

        string token = _configuration.GetValueStrict<string>("GH:TOKEN");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd(Guid.NewGuid().ToString());

        using var httpClient = new HttpClient();

        HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("UploadAsset: Upload failed. Status: {StatusCode}, Reason: {ReasonPhrase}, Body: {Body}", response.StatusCode,
                response.ReasonPhrase, responseBody);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("UploadAsset: Upload successful for '{FileName}'", Path.GetFileName(filePath));
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
            _logger.LogInformation("Release with tag '{TagName}' deleted.", tagName);

            if (deleteTag)
            {
                bool tagExists = await _tagsUtil.DoesTagExist(owner, repo, tagName, cancellationToken).NoSync();
                if (tagExists)
                {
                    await _tagsUtil.Delete(owner, repo, tagName, cancellationToken).NoSync();
                    _logger.LogInformation("Tag '{TagName}' deleted.", tagName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting release: {Message}", ex.Message);
            throw;
        }
    }

    public async ValueTask<List<string>> DownloadAllLatestReleaseAssets(string owner, string repo, string downloadDirectory,
        CancellationToken cancellationToken = default)
    {
        var downloadedPaths = new List<string>();

        try
        {
            var release = await GetLatestNonDraftRelease(owner, repo, cancellationToken);
            if (release?.Assets == null || release.Assets.Count == 0)
            {
                _logger.LogWarning("Latest release has no assets.");
                return downloadedPaths;
            }

            foreach (var asset in release.Assets)
            {
                string? path = await DownloadAsset(asset, downloadDirectory, cancellationToken);
                if (path != null)
                    downloadedPaths.Add(path);
            }

            return downloadedPaths;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading all release assets.");
            throw;
        }
    }

    public async ValueTask<string?> DownloadReleaseAssetByNamePattern(string owner, string repo, string downloadDirectory, IEnumerable<string> nameContains,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var release = await GetLatestNonDraftRelease(owner, repo, cancellationToken);
            if (release?.Assets == null)
            {
                _logger.LogWarning("No assets found.");
                return null;
            }

            var asset = release.Assets.FirstOrDefault(a => nameContains.All(part => a.Name.Contains(part, StringComparison.OrdinalIgnoreCase)));

            if (asset == null)
            {
                _logger.LogWarning("No matching asset found in release {Tag}", release.TagName);
                return null;
            }

            return await DownloadAsset(asset, downloadDirectory, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadReleaseAssetByNamePattern");
            throw;
        }
    }

    public async ValueTask<Release?> Get(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        var releases = await GetAll(owner, repo, cancellationToken).NoSync();
        var release = releases.FirstOrDefault(r => r.TagName == tagName);

        if (release == null)
        {
            _logger.LogWarning("No release for tag '{TagName}'", tagName);
            return null;
        }

        _logger.LogInformation("Found release '{ReleaseName}' with tag '{TagName}'", release.Name, tagName);
        return release;
    }

    public async ValueTask<IReadOnlyList<Release>> GetAll(string owner, string repo, CancellationToken cancellationToken = default)
    {
        GitHubOpenApiClient client = await _gitHubOpenApiClientUtil.Get(cancellationToken).NoSync();
        List<Release>? releases = await client.Repos[owner][repo].Releases.GetAsync(cancellationToken: cancellationToken).NoSync();

        _logger.LogInformation("Retrieved {Count} releases for '{Repo}'", releases.Count, repo);
        return releases;
    }

    private async ValueTask<Release?> GetLatestNonDraftRelease(string owner, string repo, CancellationToken cancellationToken = default)
    {
        var releases = await GetAll(owner, repo, cancellationToken).NoSync();

        return releases.Where(r => !r.Draft.GetValueOrDefault(false)).OrderByDescending(r => r.CreatedAt).FirstOrDefault();
    }

    private async ValueTask<string?> DownloadAsset(ReleaseAsset asset, string downloadDirectory, CancellationToken cancellationToken = default)
    {
        string filePath = Path.Combine(downloadDirectory, asset.Name);
        string downloadUrl = asset.BrowserDownloadUrl;

        using var httpClient = new HttpClient();
        string token = _configuration.GetValueStrict<string>("GH:TOKEN");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Guid.NewGuid().ToString());

        _logger.LogInformation("Downloading asset '{Name}' from {Url}", asset.Name, downloadUrl);

        using var response = await httpClient.GetAsync(downloadUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Download failed for {Name}. Status: {StatusCode}, Body: {Body}", asset.Name, response.StatusCode, body);
            return null;
        }

        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fs, cancellationToken);

        _logger.LogInformation("Saved asset to {Path}", filePath);
        return filePath;
    }
}