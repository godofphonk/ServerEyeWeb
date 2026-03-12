namespace ServerEye.Infrastructure.ExternalServices.GoApi;

using System.Net.Http.Json;
using ServerEye.Core.DTOs.GoApi;

/// <summary>
/// HTTP communication handler for Go API.
/// </summary>
public class GoApiHttpHandler(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    /// <summary>
    /// Checks if response is successful and returns content string.
    /// </summary>
    public static async Task<string?> GetSuccessfulResponseContentAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Gets error content from response.
    /// </summary>
    public static async Task<string> GetErrorContentAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Checks if response indicates server not found.
    /// </summary>
    public static bool IsNotFound(HttpResponseMessage response)
    {
        return response.StatusCode == System.Net.HttpStatusCode.NotFound;
    }

    /// <summary>
    /// Checks if response indicates service unavailable.
    /// </summary>
    public static bool IsServiceUnavailable(HttpResponseMessage response)
    {
        return response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable;
    }

    /// <summary>
    /// Performs GET request and returns response content.
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(Uri url)
    {
        return await _httpClient.GetAsync(url);
    }

    /// <summary>
    /// Performs POST request with JSON content.
    /// </summary>
    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(Uri url, T content)
    {
        return await _httpClient.PostAsJsonAsync(url, content);
    }
}
