using System.Net;

using Collectibles.Domain.Common;

using FluentAssertions;

namespace Collectibles.Domain.Tests.Common;

/// <summary>
/// The link capturer fetches these URLs server-side from a privileged network position,
/// so the policy is the boundary that keeps internal targets unreachable.
/// </summary>
public class ExternalUrlPolicyTests
{
    [Theory]
    [InlineData("https://example.com/page")]
    [InlineData("http://example.com:8080/page?q=1")]
    [InlineData("https://93.184.216.34/")]
    public void TryValidateAllowsPublicHttpUrls(string url)
    {
        var allowed = ExternalUrlPolicy.TryValidate(url, out var uri, out var error);

        allowed.Should().BeTrue(error);
        uri.Should().NotBeNull();
    }

    [Theory]
    [InlineData("file:///c:/windows/win.ini")]
    [InlineData("ftp://example.com/file")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative/path")]
    [InlineData("")]
    [InlineData(null)]
    public void TryValidateRejectsNonHttpUrls(string? url)
    {
        ExternalUrlPolicy.TryValidate(url, out _, out var error).Should().BeFalse();
        error.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")] // cloud metadata
    [InlineData("http://localhost:5111/admin")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://10.0.0.5/")]
    [InlineData("http://192.168.1.1/")]
    [InlineData("http://172.16.4.4/")]
    [InlineData("http://[::1]/")]
    [InlineData("http://intranet.local/")]
    public void TryValidateRejectsInternalTargets(string url)
    {
        ExternalUrlPolicy.TryValidate(url, out _, out var error).Should().BeFalse();
        error.Should().NotBeEmpty();
    }

    [Fact]
    public void TryValidateRejectsUrlsCarryingCredentials()
    {
        ExternalUrlPolicy.TryValidate("http://user:pass@example.com/", out _, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("169.254.169.254", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("::ffff:127.0.0.1", false)]
    [InlineData("fd00::1", false)]
    public void IsAllowedAddressClassifiesRanges(string address, bool expected)
    {
        ExternalUrlPolicy.IsAllowedAddress(IPAddress.Parse(address)).Should().Be(expected);
    }
}
