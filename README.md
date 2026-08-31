[![](https://img.shields.io/nuget/v/soenneker.github.repositories.releases.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.repositories.releases/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.github.repositories.releases/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.github.repositories.releases/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.github.repositories.releases.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.repositories.releases/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.github.repositories.releases/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.github.repositories.releases/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.GitHub.Repositories.Releases

Create and delete GitHub releases, upload release assets, and download assets from the latest published release.

## Installation

```bash
dotnet add package Soenneker.GitHub.Repositories.Releases
```

## Configuration

```json
{
  "GH": {
    "Token": "github-token"
  }
}
```

The token needs repository contents write access to create tags and releases. It must also be able to read any private release assets being downloaded.

## Registration

```csharp
services.AddGitHubRepositoriesReleasesUtilAsSingleton();
```

Use `AddGitHubRepositoriesReleasesUtilAsScoped()` for a scoped consumer.

## Create a release with an asset

```csharp
await releases.Create(
    owner: "soenneker",
    repo: "example-repository",
    tagName: "v1.2.0",
    releaseName: "v1.2.0",
    releaseBody: "Release notes",
    filePath: @"C:\artifacts\example.zip",
    cancellationToken: cancellationToken);
```

`Create` reuses an existing tag or creates one when missing, creates the release, and uploads the specified file as one release asset. `isDraft` and `isPrerelease` control the GitHub release flags.

## Download release assets

```csharp
List<string> paths = await releases.DownloadAllLatestReleaseAssets(
    "soenneker",
    "example-repository",
    @"C:\downloads",
    cancellationToken);

string? windowsAsset = await releases.DownloadReleaseAssetByNamePattern(
    "soenneker",
    "example-repository",
    @"C:\downloads",
    ["win", "x64"],
    cancellationToken);
```

The latest release is the newest non-draft release by creation time. Name-pattern matching is case-insensitive and requires every supplied fragment to appear in the asset name. Existing local files with the same name are replaced.

## Deletion

`Delete` removes the release for a tag and deletes that Git tag by default. Pass `deleteTag: false` to preserve it. `DeleteAllForRepository` applies the same behavior to every release in the repository and is irreversible through this library.
