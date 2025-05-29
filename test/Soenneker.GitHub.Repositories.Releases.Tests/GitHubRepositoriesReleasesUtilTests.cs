using Soenneker.GitHub.Repositories.Releases.Abstract;
using Soenneker.Tests.FixturedUnit;
using System;
using System.Threading.Tasks;
using Soenneker.Facts.Local;
using Xunit;

namespace Soenneker.GitHub.Repositories.Releases.Tests;

[Collection("Collection")]
public class GitHubRepositoriesReleasesUtilTests : FixturedUnitTest
{
    private readonly IGitHubRepositoriesReleasesUtil _util;

    private const string _owner = "";
    private const string _repo = "";
    private const string _filePath = @"";

    public GitHubRepositoriesReleasesUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IGitHubRepositoriesReleasesUtil>(true);
    }

    [Fact]
    public void Default()
    {

    }

    [LocalFact]
    public async Task CreateAndUploadAsset_Succeeds()
    {
        var tag = $"v{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var name = $"Integration test release {tag}";
        const string body = "This is a test release";

        await _util.Create(_owner, _repo, tag, name, body, _filePath);
    }
}
