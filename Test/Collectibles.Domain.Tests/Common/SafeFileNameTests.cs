using Collectibles.Domain.Common;

using FluentAssertions;

namespace Collectibles.Domain.Tests.Common;

public class SafeFileNameTests
{
    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("photos/holiday.jpg", "holiday.jpg")]
    [InlineData(@"..\..\..\x\evil.zip", "evil.zip")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData(@"C:\Windows\System32\evil.dll", "evil.dll")]
    public void SanitizeKeepsOnlyTheLeafName(string input, string expected)
    {
        SafeFileName.Sanitize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SanitizeFallsBackWhenNothingUsableRemains(string? input)
    {
        SafeFileName.Sanitize(input).Should().Be("file");
    }

    [Fact]
    public void SanitizeReplacesCharactersIllegalInAFileName()
    {
        var result = SafeFileName.Sanitize("in:va|lid?.txt");

        result.Should().NotContain(":").And.NotContain("|").And.NotContain("?");
        result.Should().EndWith(".txt");
    }
}
