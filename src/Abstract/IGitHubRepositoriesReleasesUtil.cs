using Soenneker.GitHub.OpenApiClient.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GitHub.Repositories.Releases.Abstract;

/// <summary>
/// A utility for managing GitHub releases and their assets using the GitHub REST API via OpenAPI client.
/// </summary>
public interface IGitHubRepositoriesReleasesUtil
{
    /// <summary>
    /// Creates a GitHub release with the specified tag, name, and body. 
    /// Automatically creates the tag if it does not already exist, and uploads a single asset to the release.
    /// </summary>
    /// <param name="owner">The owner of the GitHub repository (user or organization).</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The tag name to associate with the release.</param>
    /// <param name="releaseName">The name/title of the release.</param>
    /// <param name="releaseBody">The description/body content of the release.</param>
    /// <param name="filePath">The full file path to the asset to be uploaded.</param>
    /// <param name="isDraft">Whether the release should be marked as a draft.</param>
    /// <param name="isPrerelease">Whether the release should be marked as a prerelease.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    ValueTask Create(string owner, string repo, string tagName, string releaseName, string releaseBody, string filePath, bool isDraft = false, bool isPrerelease = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a single asset to an existing GitHub release.
    /// </summary>
    /// <param name="owner">The owner of the GitHub repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="releaseId">The ID of the release to which the asset will be uploaded.</param>
    /// <param name="filePath">The full path to the file to upload.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    ValueTask UploadAsset(string owner, string repo, long releaseId, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a release and optionally its associated tag.
    /// </summary>
    /// <param name="owner">The owner of the GitHub repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The tag name of the release to delete.</param>
    /// <param name="deleteTag">If true, the Git tag will also be deleted after removing the release.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    ValueTask Delete(string owner, string repo, string tagName, bool deleteTag = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads all assets from the latest non-draft release of the specified repository.
    /// </summary>
    /// <param name="owner">The owner of the GitHub repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="downloadDirectory">The local directory where downloaded files will be saved.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of file paths where the assets were saved.</returns>
    ValueTask<List<string>> DownloadAllLatestReleaseAssets(string owner, string repo, string downloadDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the first release asset from the latest non-draft release whose name contains all provided substrings (case-insensitive).
    /// </summary>
    /// <param name="owner">The owner of the GitHub repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="downloadDirectory">The local directory where the file will be saved.</param>
    /// <param name="nameContains">A collection of substrings that must all be contained in the asset name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The full file path of the downloaded asset, or null if no match was found.</returns>
    ValueTask<string?> DownloadReleaseAssetByNamePattern(string owner, string repo, string downloadDirectory, IEnumerable<string> nameContains, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific release by its tag name.
    /// </summary>
    /// <param name="owner">The owner of the GitHub repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The tag name of the release to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The release associated with the specified tag, or null if not found.</returns>
    ValueTask<Release?> Get(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all releases (drafts and published) from the specified repository.
    /// </summary>
    /// <param name="owner">The owner of the GitHub repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of all releases in the repository.</returns>
    ValueTask<IReadOnlyList<Release>> GetAll(string owner, string repo, CancellationToken cancellationToken = default);
}