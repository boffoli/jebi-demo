namespace Jebi.Web.Shared;

internal static class JebiCompletenessDisplay
{
    public static string GetBadgeLabel(object? completeness) => GetStateName(completeness) switch
    {
        "Complete" => "OK - Complete",
        "NeedsAttention" => "WARN - Needs attention",
        _ => "ERROR - Incomplete"
    };

    public static string GetBadgeClass(object? completeness) =>
        "jebi-binding-state-badge jebi-completeness-badge";

    public static int GetSortRank(object? completeness) => GetStateName(completeness) switch
    {
        "Complete" => 0,
        "NeedsAttention" => 1,
        _ => 2
    };

    public static string GetItemClass(object? item) => GetStateName(item) switch
    {
        "Pass" => "jebi-completeness-item",
        "Warn" => "jebi-completeness-item",
        _ => "jebi-completeness-item"
    };

    public static string GetItemPrefix(object? item) => GetStateName(item) switch
    {
        "Pass" => "OK",
        "Warn" => "WARN",
        _ => "ERROR"
    };

    public static string GetItemPrefixClass(object? item) => GetStateName(item) switch
    {
        "Pass" => "jebi-completeness-item__prefix jebi-completeness-item__prefix-ok",
        "Warn" => "jebi-completeness-item__prefix jebi-completeness-item__prefix-warn",
        _ => "jebi-completeness-item__prefix jebi-completeness-item__prefix-error"
    };

    public static string GetStateName(object? owner) =>
        owner?.GetType().GetProperty("State")?.GetValue(owner)?.ToString() ?? string.Empty;

    public static string GetLabel(object? owner) =>
        owner?.GetType().GetProperty("Label")?.GetValue(owner)?.ToString() ?? "Check";

    public static string GetMessage(object? owner) =>
        owner?.GetType().GetProperty("Message")?.GetValue(owner)?.ToString() ?? "No details.";
}
