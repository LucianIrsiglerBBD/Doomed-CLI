namespace DoomedCLI.Utility.HTTP;

public class HttpHandler
{
    private readonly HttpClient _client;

    public HttpHandler(HttpClient client)
    {
        _client = client;
    }

    private void AttachSession()
    {
        if (File.Exists(".token"))
        {
            var token = File.ReadAllText(".token").Trim();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<string> GetAsync(string url, CancellationToken cancellation)
    {
        AttachSession();
        
        using var response = await _client.GetAsync(url, cancellation).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}",
                null,
                response.StatusCode);
        }

        return responseBody;
    }

    public async Task<string?> PostAsync(string requestUrl, HttpContent content,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        AttachSession();

        using var response = await _client.PostAsync(requestUrl, content, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}",
                null,
                response.StatusCode);
        }

        return responseBody;
    }

    public async Task<string?> PatchAsync(string requestUrl, HttpContent content,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        AttachSession();

        using var response = await _client.PatchAsync(requestUrl, content, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}",
                null,
                response.StatusCode);
        }

        return responseBody;
    }

    public async Task<string?> PutAsync(string requestUrl, HttpContent content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        AttachSession();

        using var response = await _client.PutAsync(requestUrl, content, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}",
                null,
                response.StatusCode);
        }

        return responseBody;
    }

    public async Task<string?> DeleteAsync(string requestUrl,
        CancellationToken cancellationToken = default)
    {
        AttachSession();

        using var response = await _client.DeleteAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}",
                null,
                response.StatusCode);
        }

        return responseBody;
    }

}
