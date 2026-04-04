namespace ServerEye.API.Helpers;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Helper class for Uri operations to work around .NET 10 Uri constructor overload resolution issues.
/// </summary>
public static class UriHelper
{
    /// <summary>
    /// Tries to create an absolute Uri from a string.
    /// </summary>
    /// <param name="uriString">The URI string to parse.</param>
    /// <param name="result">The resulting Uri if successful.</param>
    /// <returns>True if the Uri was created successfully and is absolute; otherwise, false.</returns>
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings", Justification = "Method purpose is to parse string to Uri")]
    public static bool TryCreateAbsoluteUri(string? uriString, out Uri? result)
    {
        result = null;
        
        if (string.IsNullOrEmpty(uriString))
        {
            return false;
        }

        if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
        {
            return false;
        }

        try
        {
            result = new Uri(uriString);
            return result.IsAbsoluteUri;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
