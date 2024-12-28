using Soenneker.GitHub.Repositories.Releases.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.GitHub.Repositories.Releases.Tests;

[Collection("Collection")]
public class GitHubRepositoriesReleasesUtilTests : FixturedUnitTest
{
    private readonly IGitHubRepositoriesReleasesUtil _util;

    public GitHubRepositoriesReleasesUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IGitHubRepositoriesReleasesUtil>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
