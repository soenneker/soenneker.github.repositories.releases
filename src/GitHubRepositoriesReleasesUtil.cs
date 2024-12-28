using Microsoft.Extensions.Logging;
using Octokit;
using Soenneker.GitHub.Repositories.Releases.Abstract;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using Soenneker.GitHub.Client.Abstract;
using System.Threading;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.Extensions.Task;
using Soenneker.GitHub.Repositories.Tags.Abstract;
using System.Collections.Generic;

namespace Soenneker.GitHub.Repositories.Releases;

/// <inheritdoc cref="IGitHubRepositoriesReleasesUtil"/>
public class GitHubRepositoriesReleasesUtil : IGitHubRepositoriesReleasesUtil
{
    private readonly IGitHubClientUtil _gitHubClientUtil;
    private readonly IGitHubRepositoriesTagsUtil _tagsUtil;
    private readonly ILogger<GitHubRepositoriesReleasesUtil> _logger;

    public GitHubRepositoriesReleasesUtil(ILogger<GitHubRepositoriesReleasesUtil> logger, IGitHubClientUtil gitHubClientUtil, IGitHubRepositoriesTagsUtil tagsUtil)
    {
        _logger = logger;
        _gitHubClientUtil = gitHubClientUtil;
        _tagsUtil = tagsUtil;
    }

    public async ValueTask Create(string owner, string repo, string tagName, string releaseName, string releaseBody, string filePath, bool isDraft = false,
        bool isPrerelease = false, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if the tag already exists
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

            // Create the release
            var newRelease = new NewRelease(tagName)
            {
                Name = releaseName,
                Body = releaseBody,
                Draft = isDraft,
                Prerelease = isPrerelease
            };

            GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

            Release? release = await client.Repository.Release.Create(owner, repo, newRelease).NoSync();
            _logger.LogInformation("Release '{ReleaseName}' created successfully.", release.Name);

            // Upload the executable as a release asset
            await UploadAsset(release, filePath, cancellationToken).NoSync();
            _logger.LogInformation("Executable '{FileName}' uploaded successfully.", Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
            throw;
        }
    }

    public async ValueTask<ReleaseAsset?> UploadAsset(Release release, string filePath, CancellationToken cancellationToken = default)
    {
        GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        var assetUpload = new ReleaseAssetUpload
        {
            FileName = Path.GetFileName(filePath),
            ContentType = GetMimeType(filePath),
            RawData = File.OpenRead(filePath)
        };

        ReleaseAsset? asset = await client.Repository.Release.UploadAsset(release, assetUpload, cancellationToken).NoSync();

        return asset;
    }

    public async ValueTask Delete(string owner, string repo, string tagName, bool deleteTag = true, CancellationToken cancellationToken = default)
    {
        try
        {
            GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

            Release? release = await Get(owner, repo, tagName, cancellationToken).NoSync();

            if (release == null)
                return;

            await client.Repository.Release.Delete(owner, repo, release.Id).NoSync();
            _logger.LogInformation("Release with tag '{TagName}' deleted successfully.", tagName);

            if (deleteTag)
            {
                // Optional: Delete the tag associated with the release
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
            GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

            // Fetch all releases from the repository
            IReadOnlyList<Release>? releases = await client.Repository.Release.GetAll(owner, repo).NoSync();

            _logger.LogInformation("Successfully retrieved {Count} releases for repository '{Repo}'.", releases.Count, repo);

            return releases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving releases for repository '{Repo}': {Message}", repo, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Determines the MIME type based on the file extension.
    /// </summary>
    private static string GetMimeType(string filePath)
    {
        // will need more
        string extension = Path.GetExtension(filePath).ToLowerInvariantFast();
        return extension switch
        {
            ".exe" => "application/vnd.microsoft.portable-executable",
            ".zip" => "application/zip",
            ".tar" => "application/x-tar",
            ".gz" => "application/gzip",
            _ => "application/octet-stream",
        };
    }
}