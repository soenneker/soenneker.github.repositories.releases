using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.GitHub.Repositories.Releases.Abstract;
using Soenneker.GitHub.Repositories.Tags.Registrars;

namespace Soenneker.GitHub.Repositories.Releases.Registrars;

/// <summary>
/// A utility library for GitHub repository release operations
/// </summary>
public static class GitHubRepositoriesReleasesUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IGitHubRepositoriesReleasesUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddGitHubRepositoriesReleasesUtilAsSingleton(this IServiceCollection services)
    {
        services.AddGitHubRepositoriesTagsUtilAsSingleton()
                .TryAddSingleton<IGitHubRepositoriesReleasesUtil, GitHubRepositoriesReleasesUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IGitHubRepositoriesReleasesUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddGitHubRepositoriesReleasesUtilAsScoped(this IServiceCollection services)
    {
        services.AddGitHubRepositoriesTagsUtilAsScoped()
                .TryAddScoped<IGitHubRepositoriesReleasesUtil, GitHubRepositoriesReleasesUtil>();

        return services;
    }
}