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

        // Remove all control characters (including CR/LF) to prevent log injection.
        // This keeps printable characters intact while ensuring log-safe output.
        var span = input.AsSpan();
        var buffer = new char[span.Length];
        var index = 0;

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (!char.IsControl(c))
            {
                buffer[index++] = c;
            }
        }

        return index == span.Length
            ? input
            : new string(buffer, 0, index);
    }

    /// <summary>
    /// Masks an email address for logging, showing only first few characters before @ symbol.
    /// </summary>
    /// <param name="email">The email address to mask.</param>
    /// <param name="visibleChars">Number of characters to show before masking (default: 3).</param>
    /// <returns>Masked email address safe for logging.</returns>
    public static string MaskEmail(string? email, int visibleChars = 3)
    {
        // If there is no email, log a generic placeholder.
        if (string.IsNullOrEmpty(email))
        {
            return "***";
        }

        // Remove control characters to prevent log injection.
        var sanitized = Sanitize(email);
        if (sanitized == null)
        {
            return "***";
        }

        // Basic structural validation: must contain '@' and at least one character before it.
        var atIndex = sanitized.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 0)
        {
            return "***";
        }

        // Only expose a small, fixed number of characters from the local part.
        if (visibleChars < 1)
        {
            visibleChars = 1;
        }

        var charsToShow = Math.Min(atIndex, visibleChars);

        // Construct a fixed-format, masked value that is safe to log.
        var prefix = sanitized[..charsToShow];
        return $"{prefix}***";
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
