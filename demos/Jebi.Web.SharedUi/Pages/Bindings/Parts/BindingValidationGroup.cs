using Jebi.Common.Features.Dtos.Model.Binding;

namespace Jebi.Web.Pages.Bindings.Parts;

/// <summary>
/// UI grouping model for binding validation issues.
/// </summary>
public sealed class BindingValidationGroup
{
    /// <summary>
    /// Human-readable label for the group.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Validation issues associatedd with the group.
    /// </summary>
    public IReadOnlyCollection<BindingValidationIssueDto> Issues { get; init; } = [];
}
