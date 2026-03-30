namespace DoomedCLI.Utility.HTTP;

public class HttpHandler
{
    private readonly HttpClient _client;

    public HttpHandler(HttpClient client)
    {
        _client = client;
    }

    public async Task<string> GetAsync(string url, CancellationToken cancellation)
    {
        var response = await _client.GetAsync(url, cancellation);
        return await response.Content.ReadAsStringAsync(cancellation);
    }

    public async Task<string?> PostAsync(string requestUrl, HttpContent content,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        
        try
        {
            using var response = await _client.PostAsync(requestUrl, content, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string?> PatchAsync(string requestUrl, HttpContent content,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            using var response = await _client.PatchAsync(requestUrl, content, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string?> PutAsync(string requestUrl, HttpContent content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            using var response = await _client.PutAsync(requestUrl, content, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string?> DeleteAsync(string requestUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _client.DeleteAsync(requestUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            throw;
        }
    }

}
