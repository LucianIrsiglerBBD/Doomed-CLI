using System.Text;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DoomedCLI.Utility.HTTP.Tests;

public sealed class HttpHandlerTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly HttpHandler _sut;

    public HttpHandlerTests()
    {
        _server = WireMockServer.Start();
        var client = new HttpClient { BaseAddress = new Uri(_server.Url!) };
        _sut = new HttpHandler(client);
    }

    public void Dispose()
    {
        _server.Dispose();
    }

    [Fact]
    public async Task GetAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/products/1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"id":1,"title":"Test Product","price":99.99}"""));

        // Act
        var result = await _sut.GetAsync("/products/1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var jsonDoc = JsonDocument.Parse(result);
        Assert.Equal(1, jsonDoc.RootElement.GetProperty("id").GetInt32());
        Assert.Equal("Test Product", jsonDoc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsEmpty()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/products/999")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404));

        // Act
        var result = await _sut.GetAsync("/products/999", CancellationToken.None);

        // Assert
        Assert.Equal("",result);
    }

    [Fact]
    public async Task PostAsync_ValidRequest_ReturnsCreatedResponse()
    {
        // Arrange
        var requestJson = """{"title":"New Product","price":49.99}""";

        _server
            .Given(Request.Create()
                .WithPath("/products")
                .UsingPost()
                .WithBody(requestJson)
                .WithHeader("Content-Type", "application/json; charset=utf-8"))
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithBody("""{"id":101,"title":"New Product","price":49.99}"""));

        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        // Act
        var result = await _sut.PostAsync("/products", content);

        // Assert
        Assert.NotNull(result);
        var jsonDoc = JsonDocument.Parse(result);
        Assert.Equal(101, jsonDoc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task PutAsync_ValidRequest_ReturnsUpdatedResponse()
    {
        // Arrange
        var requestJson = """{"id":1,"title":"Updated Product","price":79.99}""";

        _server
            .Given(Request.Create()
                .WithPath("/products/1")
                .UsingPut()
                .WithBody(requestJson))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("""{"id":1,"title":"Updated Product","price":79.99}"""));

        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        // Act
        var result = await _sut.PutAsync("/products/1", content);

        // Assert
        Assert.NotNull(result);
        var jsonDoc = JsonDocument.Parse(result);
        Assert.Equal("Updated Product", jsonDoc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PatchAsync_ValidRequest_ReturnsPatchedResponse()
    {
        // Arrange
        var requestJson = """{"price":29.99}""";

        _server
            .Given(Request.Create()
                .WithPath("/products/1")
                .UsingPatch()
                .WithBody(requestJson))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("""{"id":1,"title":"Original Title","price":29.99}"""));

        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        // Act
        var result = await _sut.PatchAsync("/products/1", content);

        // Assert
        Assert.NotNull(result);
        var jsonDoc = JsonDocument.Parse(result);
        Assert.Equal(29.99, jsonDoc.RootElement.GetProperty("price").GetDouble());
    }

    [Fact]
    public async Task DeleteAsync_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/products/1")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(204));

        // Act
        var result = await _sut.DeleteAsync("/products/1");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // 204 No Content returns empty body
    }

    [Fact]
    public async Task PostAsync_NullContent_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.PostAsync("/products", null!));
    }

    [Fact]
    public async Task PatchAsync_NullContent_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.PatchAsync("/products/1", null!));
    }

    [Fact]
    public async Task PutAsync_NullContent_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.PutAsync("/products/1", null!));
    }
}