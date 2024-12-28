using Octokit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Soenneker.GitHub.Repositories.Releases.Abstract;

/// <summary>
/// Provides utility methods for managing GitHub repository releases.
/// </summary>
public interface IGitHubRepositoriesReleasesUtil
{
    /// <summary>
    /// Creates a release in the specified GitHub repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The tag name for the release.</param>
    /// <param name="releaseName">The name of the release.</param>
    /// <param name="releaseBody">The body description of the release.</param>
    /// <param name="filePath">The file path of the asset to upload.</param>
    /// <param name="isDraft">Indicates whether the release is a draft.</param>
    /// <param name="isPrerelease">Indicates whether the release is a prerelease.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask Create(string owner, string repo, string tagName, string releaseName, string releaseBody, string filePath, bool isDraft = false, bool isPrerelease = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an asset to an existing release.
    /// </summary>
    /// <param name="release">The release to upload the asset to.</param>
    /// <param name="filePath">The file path of the asset to upload.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The uploaded release asset.</returns>
    ValueTask<ReleaseAsset?> UploadAsset(Release release, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a release and optionally its associated tag.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The tag name of the release to delete.</param>
    /// <param name="deleteTag">Indicates whether the associated tag should also be deleted.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask Delete(string owner, string repo, string tagName, bool deleteTag = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a release by its tag name.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The tag name of the release.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The release if found, otherwise null.</returns>
    ValueTask<Release?> Get(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all releases from the specified repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of releases.</returns>
    ValueTask<IReadOnlyList<Release>> GetAll(string owner, string repo, CancellationToken cancellationToken = default);
}