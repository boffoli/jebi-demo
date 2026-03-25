using Jebi.Common.Features.Dtos.Model.Target;
using Jebi.Web.Shared;

namespace Jebi.Web.Pages.Targets.Parts;

internal sealed class TargetInspectorCardVm
{
    public Guid ClassId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsOwnedEntity { get; init; }
    public IReadOnlyList<TargetInspectorPropertyVm> Properties { get; init; } = [];
    public IReadOnlyList<JebiUiRelationItem> NestedRelations { get; init; } = [];
    public IReadOnlyList<JebiUiRelationItem> HasRelations { get; init; } = [];
}

internal sealed class TargetInspectorPropertyVm
{
    public Guid PropertyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsKey { get; init; }
    public bool IsRequired { get; init; }
}

internal static class TargetInspectorProjection
{
    public static IReadOnlyList<TargetInspectorCardVm> CreateCards(TargetContextDetailDto? context)
    {
        if (context?.ClassInfos is null || context.ClassInfos.Count == 0)
            return [];

        var classes = EnumerateClasses(context.ClassInfos).ToList();
        var childLookup = classes
            .SelectMany(child => child.PropertyInfos
                .Where(property => property.IsForeignKey && property.ReferencedPrimaryKeyPropertyId.HasValue)
                .Select(property => (
                    PkId: property.ReferencedPrimaryKeyPropertyId!.Value,
                    ChildName: child.Name,
                    IsOwned: child.IsOwnedEntity,
                    IsMany: property.IsToManyRelationship)))
            .ToLookup(static item => item.PkId, static item => (item.ChildName, item.IsOwned, item.IsMany));

        return classes
            .OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entity =>
            {
                var relations = entity.PropertyInfos
                    .Where(static property => property.IsKey)
                    .Select(static property => property.Id)
                    .SelectMany(pkId => childLookup[pkId])
                    .DistinctBy(static relation => relation.ChildName)
                    .OrderBy(static relation => relation.ChildName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new TargetInspectorCardVm
                {
                    ClassId = entity.Id,
                    Name = entity.Name,
                    IsOwnedEntity = entity.IsOwnedEntity,
                    Properties = entity.PropertyInfos
                        .Where(static property => !property.IsForeignKey)
                        .OrderBy(static property => property.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(property => new TargetInspectorPropertyVm
                        {
                            PropertyId = property.Id,
                            Name = property.Name,
                            IsKey = property.IsKey,
                            IsRequired = property.IsRequired
                        })
                        .ToList(),
                    NestedRelations = relations
                        .Where(static relation => relation.IsOwned)
                        .Select(static relation => new JebiUiRelationItem(relation.ChildName, relation.IsMany))
                        .ToList(),
                    HasRelations = relations
                        .Where(static relation => !relation.IsOwned)
                        .Select(static relation => new JebiUiRelationItem(relation.ChildName, relation.IsMany))
                        .ToList()
                };
            })
            .ToList();
    }

    private static IEnumerable<TargetClassInfoDto> EnumerateClasses(IEnumerable<TargetClassInfoDto>? classes)
    {
        if (classes is null)
            yield break;

        var seen = new HashSet<Guid>();

        foreach (var classInfo in EnumerateClasses(classes, seen))
            yield return classInfo;
    }

    private static IEnumerable<TargetClassInfoDto> EnumerateClasses(
        IEnumerable<TargetClassInfoDto>? classes,
        ISet<Guid> seen)
    {
        if (classes is null)
            yield break;

        foreach (var classInfo in classes)
        {
            if (!seen.Add(classInfo.Id))
                continue;

            yield return classInfo;

            foreach (var child in EnumerateClasses(classInfo.ClassInfos, seen))
                yield return child;
        }
    }
}
