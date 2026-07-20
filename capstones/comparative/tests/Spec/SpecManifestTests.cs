using ComparativeKv.Tests.Support;

namespace ComparativeKv.Tests.Spec;

public sealed class SpecManifestTests
{
    [Fact]
    public void CopiedFrozenSpecMatchesItsManifest()
    {
        SpecManifestVerifier.Verify();
    }
}
