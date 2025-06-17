using Soenneker.GitHub.OpenApiClient.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GitHub.Repositories.Releases.Abstract;

/// <summary>
/// Provides utilities for managing GitHub repository releases, including creation, retrieval, deletion, and asset uploads.
/// </summary>
public interface IGitHubRepositoriesReleasesUtil
{
    /// <summary>
    /// Creates a new release in the specified GitHub repository. If the tag does not exist, it will be created.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The tag to associate with the release.</param>
    /// <param name="releaseName">The name of the release.</param>
    /// <param name="releaseBody">The body/description of the release.</param>
    /// <param name="filePath">The file path of the release asset to upload.</param>
    /// <param name="isDraft">Whether the release should be marked as a draft.</param>
    /// <param name="isPrerelease">Whether the release should be marked as a prerelease.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask Create(string owner, string repo, string tagName, string releaseName, string releaseBody, string filePath, bool isDraft = false, bool isPrerelease = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an asset file to an existing release.
    /// </summary>
    /// <param name="releaseId"></param>
    /// <param name="filePath">The local file path of the asset.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="owner"></param>
    /// <param name="repo"></param>
    /// <returns>The uploaded <see cref="ReleaseAsset"/> if successful; otherwise, <c>null</c>.</returns>
    ValueTask UploadAsset(string owner, string repo, long releaseId, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a release from a repository and optionally deletes the associated tag.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The tag associated with the release.</param>
    /// <param name="deleteTag">Whether to delete the associated tag.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask Delete(string owner, string repo, string tagName, bool deleteTag = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a release associated with the specified tag.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The tag of the release.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="Release"/> if found; otherwise, <c>null</c>.</returns>
    ValueTask<Release?> Get(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all releases for a given repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A read-only list of <see cref="Release"/> objects.</returns>
    ValueTask<IReadOnlyList<Release>> GetAll(string owner, string repo, CancellationToken cancellationToken = default);
}