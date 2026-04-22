using Soenneker.GitHub.Repositories.Releases.Abstract;
using Soenneker.Tests.HostedUnit;
using System;
using System.Threading.Tasks;
using Soenneker.Tests.Attributes.Local;

namespace Soenneker.GitHub.Repositories.Releases.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class GitHubRepositoriesReleasesUtilTests : HostedUnitTest
{
    private readonly IGitHubRepositoriesReleasesUtil _util;

    private const string _owner = "";
    private const string _repo = "";
    private const string _filePath = @"";

    public GitHubRepositoriesReleasesUtilTests(Host host) : base(host)
    {
        _util = Resolve<IGitHubRepositoriesReleasesUtil>(true);
    }

    [Test]
    public void Default()
    {

    }

    [LocalOnly]
    public async Task CreateAndUploadAsset_Succeeds()
    {
        var tag = $"v{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var name = $"Integration test release {tag}";
        const string body = "This is a test release";

        await _util.Create(_owner, _repo, tag, name, body, _filePath);
    }
}
