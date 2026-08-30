using Collectibles.Web.Middleware;

namespace Collectibles.Application.Tests.Features.Logging;

/// <summary>
/// Share tokens are bearer credentials that used to be written verbatim into the request log
/// table, because <c>/share/</c> and <c>/api/public/</c> were not excluded from logging. These pin
/// the redaction so a token cannot return to durable storage.
/// </summary>
public class SensitiveRequestDataRedactorTests
{
    private const string Token = "Zm9vYmFyLXNlY3JldC10b2tlbi12YWx1ZQ";

    [Fact]
    public void RedactPathShouldRemoveTheTokenFromAShareLink()
    {
        var redacted = SensitiveRequestDataRedactor.RedactPath($"/share/{Token}");

        redacted.Should().NotContain(Token);
        redacted.Should().Be($"/share/{SensitiveRequestDataRedactor.Marker}");
    }

    [Fact]
    public void RedactPathShouldRemoveTheTokenFromAPublicPreviewRoute()
    {
        var redacted = SensitiveRequestDataRedactor.RedactPath(
            $"/api/public/attachments/abc123/preview/{Token}");

        redacted.Should().NotContain(Token);
        redacted.Should().Be($"/api/public/attachments/abc123/preview/{SensitiveRequestDataRedactor.Marker}");
    }

    [Fact]
    public void RedactPathShouldKeepTheRouteIdentifiable()
    {
        var redacted = SensitiveRequestDataRedactor.RedactPath(
            $"/api/public/attachments/abc123/thumbnail/{Token}");

        // The hash and action survive, so the log still says what was accessed.
        redacted.Should().StartWith("/api/public/attachments/abc123/thumbnail/");
    }

    [Fact]
    public void RedactPathShouldBeCaseInsensitiveAboutTheRoutePrefix()
    {
        SensitiveRequestDataRedactor.RedactPath($"/Share/{Token}")
            .Should().NotContain(Token);
    }

    [Theory]
    [InlineData("/api/attachments/abc123/preview")]
    [InlineData("/showcases/42")]
    [InlineData("/")]
    [InlineData("")]
    public void RedactPathShouldLeaveOrdinaryPathsAlone(string path)
    {
        SensitiveRequestDataRedactor.RedactPath(path).Should().Be(path);
    }

    [Fact]
    public void RedactQueryStringShouldRemoveTokenValues()
    {
        var redacted = SensitiveRequestDataRedactor.RedactQueryString($"?token={Token}&page=2");

        redacted.Should().NotContain(Token);
        redacted.Should().Be($"?token={SensitiveRequestDataRedactor.Marker}&page=2");
    }

    [Theory]
    [InlineData("access_token")]
    [InlineData("apikey")]
    [InlineData("api_key")]
    [InlineData("secret")]
    [InlineData("password")]
    [InlineData("code")]
    [InlineData("KEY")]
    public void RedactQueryStringShouldRemoveEveryCredentialBearingParameter(string name)
    {
        SensitiveRequestDataRedactor.RedactQueryString($"?{name}={Token}")
            .Should().NotContain(Token);
    }

    [Fact]
    public void RedactQueryStringShouldPreserveNonSensitiveParameters()
    {
        const string query = "?page=2&sort=name&filter=active";

        SensitiveRequestDataRedactor.RedactQueryString(query).Should().Be(query);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?")]
    public void RedactQueryStringShouldHandleAnAbsentQuery(string query)
    {
        SensitiveRequestDataRedactor.RedactQueryString(query).Should().Be(query);
    }

    [Fact]
    public void RedactQueryStringShouldLeaveValuelessParametersIntact()
    {
        SensitiveRequestDataRedactor.RedactQueryString("?flag&page=1")
            .Should().Be("?flag&page=1");
    }
}
