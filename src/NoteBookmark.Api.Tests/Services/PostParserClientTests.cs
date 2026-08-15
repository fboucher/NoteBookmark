using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.Protected;

namespace NoteBookmark.Api.Tests.Services;

public class PostParserClientTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<HttpMessageHandler> _mockHandler;

    public PostParserClientTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // Default config setups
        _mockConfig.Setup(c => c["Parser:BaseUrl"]).Returns((string?)null);
        _mockConfig.Setup(c => c["Parser:ApiKey"]).Returns((string?)null);
    }

    private PostParserClient CreateSut(HttpClient httpClient) =>
        new(httpClient, _mockConfig.Object, NullLogger<PostParserClient>.Instance);

    [Fact]
    public async Task ExtractContentAsync_WithDefaults_CallsDefaultUrlWithoutApiKey()
    {
        // Arrange
        var expectedUrl = "https://azpostlight-parser.azurewebsites.net/api/parser";
        var sourceUrl = "https://example.com/blog-post";

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == expectedUrl &&
                    !req.Headers.Contains("x-functions-key")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"content\":\"extracted blog content\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(_mockHandler.Object);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ExtractContentAsync(sourceUrl);

        // Assert
        result.Should().Be("extracted blog content");
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExtractContentAsync_WithApiKey_SendsXFunctionsKeyHeader()
    {
        // Arrange
        var expectedUrl = "https://azpostlight-parser.azurewebsites.net/api/parser";
        var sourceUrl = "https://example.com/blog-post";
        var apiKey = "test-api-key-123";

        _mockConfig.Setup(c => c["Parser:ApiKey"]).Returns(apiKey);

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == expectedUrl &&
                    req.Headers.Contains("x-functions-key") &&
                    string.Join("", req.Headers.GetValues("x-functions-key")) == apiKey),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"content\":\"content with auth\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(_mockHandler.Object);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ExtractContentAsync(sourceUrl);

        // Assert
        result.Should().Be("content with auth");
    }

    [Fact]
    public async Task ExtractContentAsync_WithCustomUrl_CallsCustomUrl()
    {
        // Arrange
        var customUrl = "https://my-custom-parser.com/api/parser";
        var sourceUrl = "https://example.com/blog-post";

        _mockConfig.Setup(c => c["Parser:BaseUrl"]).Returns(customUrl);

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == customUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"content\":\"custom url content\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(_mockHandler.Object);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ExtractContentAsync(sourceUrl);

        // Assert
        result.Should().Be("custom url content");
    }

    [Fact]
    public async Task ExtractContentAsync_ParserReturnsErrorCode_ReturnsNull()
    {
        // Arrange
        var sourceUrl = "https://example.com/blog-post";

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var httpClient = new HttpClient(_mockHandler.Object);
        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.ExtractContentAsync(sourceUrl);

        // Assert
        result.Should().BeNull();
    }
}
