namespace ServerEye.Core.Helpers;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Provides methods to sanitize user input before logging to prevent log injection attacks.
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes a string by removing newline and carriage return characters to prevent log injection.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>Sanitized string safe for logging.</returns>
    [return: NotNullIfNotNull(nameof(input))]
    public static string? Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Masks an email address for logging, showing only first few characters before @ symbol.
    /// </summary>
    /// <param name="email">The email address to mask.</param>
    /// <param name="visibleChars">Number of characters to show before masking (default: 3).</param>
    /// <returns>Masked email address safe for logging.</returns>
    public static string MaskEmail(string? email, int visibleChars = 3)
    {
        if (string.IsNullOrEmpty(email))
        {
            return "***";
        }

        var sanitized = Sanitize(email);
        if (sanitized == null)
        {
            return "***";
        }

        var atIndex = sanitized.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 0)
        {
            return "***";
        }

        var charsToShow = Math.Min(atIndex, visibleChars);
        return $"{sanitized[..charsToShow]}***";
    }

    /// <summary>
    /// Masks a server key for logging, showing only first few characters.
    /// </summary>
    /// <param name="serverKey">The server key to mask.</param>
    /// <param name="visibleChars">Number of characters to show before masking (default: 8).</param>
    /// <returns>Masked server key safe for logging.</returns>
    public static string MaskServerKey(string? serverKey, int visibleChars = 8)
    {
        if (string.IsNullOrEmpty(serverKey))
        {
            return "***";
        }

        var sanitized = Sanitize(serverKey);
        if (sanitized == null || sanitized.Length <= visibleChars)
        {
            return "***";
        }

        return $"{sanitized[..visibleChars]}***";
    }

    /// <summary>
    /// Masks a token or sensitive string for logging, showing only first few characters.
    /// </summary>
    /// <param name="token">The token to mask.</param>
    /// <param name="visibleChars">Number of characters to show before masking (default: 10).</param>
    /// <returns>Masked token safe for logging.</returns>
    public static string MaskToken(string? token, int visibleChars = 10)
    {
        if (string.IsNullOrEmpty(token))
        {
            return "***";
        }

        var sanitized = Sanitize(token);
        if (sanitized == null || sanitized.Length <= visibleChars)
        {
            return "***";
        }

        return $"{sanitized[..visibleChars]}...";
    }
}
