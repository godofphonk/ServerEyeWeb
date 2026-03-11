namespace ServerEye.Infrastracture.ExternalServices.GoApi;

using System.Net.Http.Json;
using ServerEye.Core.DTOs.GoApi;

/// <summary>
/// HTTP communication handler for Go API.
/// </summary>
public class GoApiHttpHandler(HttpClient httpClient)
{
    private readonly HttpClient httpClient = httpClient;

    /// <summary>
    /// Performs GET request and returns response content.
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await httpClient.GetAsync(new Uri(url, UriKind.Relative));
    }

    /// <summary>
    /// Performs POST request with JSON content.
    /// </summary>
    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T content)
    {
        return await httpClient.PostAsJsonAsync(url, content);
    }

    /// <summary>
    /// Checks if response is successful and returns content string.
    /// </summary>
    public async Task<string?> GetSuccessfulResponseContentAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Gets error content from failed response.
    /// </summary>
    public async Task<string> GetErrorContentAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Checks if response indicates server not found.
    /// </summary>
    public bool IsNotFound(HttpResponseMessage response)
    {
        return response.StatusCode == System.Net.HttpStatusCode.NotFound;
    }

    /// <summary>
    /// Checks if response indicates service unavailable.
    /// </summary>
    public bool IsServiceUnavailable(HttpResponseMessage response)
    {
        return response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable;
    }
}
