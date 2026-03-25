using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jebi.Web.Services;

/// <summary>
/// Publishes UI-friendly error messages that components can show to users.
/// </summary>
public sealed class ErrorNotifier
{
    private static readonly Regex RestFailurePrefixRegex = new(
        @"^REST\s+.+?\scall failed for '.+?':\s*\d{3}\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex GrpcFailurePrefixRegex = new(
        @"^.+?\sgRPC call failed with status\s+'.+?':\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Raised every time to new error message is available.</summary>
    public event Action<string>? OnError;

    /// <param name="ex">Exception to normalize and display.</param>
    /// <param name="autoDismissMs">
    /// Milliseconds after which the message is cleared (ll means it stays visible).
    /// </param>
    public async Task Error(Exception ex, int? autoDismissMs = null)
    {
        await _gate.WaitAsync();
        try
        {
            OnError?.Invoke(NormalizeToastMessage(ex));

            if (autoDismissMs is > 0)
            {
                _ = Task.Delay(autoDismissMs.Value)
                        .ContinueWith(_ => OnError?.Invoke(string.Empty));
            }
        }
        finally { _gate.Release(); }
    }

    private static string NormalizeToastMessage(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return NormalizeMessage(ex.Message);
    }

    /// <summary>
    /// Normalizes technical messages into to more readable UI format.
    /// </summary>
    public static string NormalizeMessage(string? rawMessage)
    {
        var raw = rawMessage?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return "Unexpected error.";

        if (TryExtractEmbeddedJsonFriendlyMessage(raw, out var extracted))
            return extracted;

        var normalized = raw;
        normalized = RestFailurePrefixRegex.Replace(normalized, string.Empty);
        normalized = GrpcFailurePrefixRegex.Replace(normalized, string.Empty);
        normalized = normalized.Trim().Trim('"');
        normalized = normalized.ReplaceLineEndings(" ");
        while (normalized.Contains("  ", StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);

        if (TryExtractEmbeddedJsonFriendlyMessage(normalized, out extracted))
            return extracted;

        if (LooksLikeGenericEntityPersistFailure(normalized))
        {
            return "Entity persistence failed: target DB rejected the write. " +
                   "Likely missing identity support (pk-pk) or unresolved required relations in structural binding. " +
                   "Check entity rules, pk-pk identity rule, and required target properties. " +
                   "Target FK fields are resolved automatically and must not be mapped manually.";
        }

        if (normalized.Length > 320)
            normalized = normalized[..320].TrimEnd() + "...";

        return string.IsNullOrWhiteSpace(normalized) ? "Unexpected error." : normalized;
    }

    private static bool LooksLikeGenericEntityPersistFailure(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var hasPersistPrefix =
            message.Contains("Persist (entity) failed:", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Entity persistence failed:", StringComparison.OrdinalIgnoreCase);

        return hasPersistPrefix &&
               message.Contains("An error occurred while saving the entity changes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryExtractEmbeddedJsonFriendlyMessage(string message, out string extracted)
    {
        extracted = string.Empty;
        if (string.IsNullOrWhiteSpace(message))
            return false;

        if (TryExtractFriendlyMessageFromJson(message, out extracted))
            return true;

        var jsonStart = message.IndexOf('{');
        if (jsonStart < 0)
            jsonStart = message.IndexOf('[');
        if (jsonStart < 0)
            return false;

        var jsonCandidate = message[jsonStart..].Trim();
        return TryExtractFriendlyMessageFromJson(jsonCandidate, out extracted);
    }

    private static bool TryExtractFriendlyMessageFromJson(string json, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.String)
            {
                var value = root.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    message = value.Trim();
                    return true;
                }
            }

            if (root.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var name in new[] { "message", "error", "detail", "title" })
            {
                if (root.TryGetProperty(name, out var property) &&
                    property.ValueKind == JsonValueKind.String)
                {
                    var value = property.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        message = value.Trim();
                        return true;
                    }
                }
            }

            if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var item in errors.EnumerateObject())
            {
                if (item.Value.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var entry in item.Value.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.String)
                        continue;

                    var value = entry.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        message = value.Trim();
                        return true;
                    }
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }
}
