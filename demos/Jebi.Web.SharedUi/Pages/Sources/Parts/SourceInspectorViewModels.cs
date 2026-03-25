using Jebi.Common.Features.Dtos.Model.Source;
using Jebi.Web.Shared;

namespace Jebi.Web.Pages.Sources.Parts;

internal sealed class SourceInspectorCardVm
{
    public Guid EntityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public JsonEntityIdentityKindDto IdentityKind { get; init; }
    public IReadOnlyList<SourceInspectorPropertyVm> Properties { get; init; } = [];
    public IReadOnlyList<JebiUiRelationItem> NestedRelations { get; init; } = [];
    public IReadOnlyList<JebiUiRelationItem> HasRelations { get; init; } = [];
}

internal sealed class SourceInspectorPropertyVm
{
    public Guid PropertyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsKey { get; init; }
}

internal static class SourceInspectorProjection
{
    public static IReadOnlyList<SourceInspectorCardVm> CreateCards(SchemaDetailDto? schema)
    {
        if (schema?.JsonEntities is null || schema.JsonEntities.Count == 0)
            return [];

        return schema.JsonEntities
            .Where(static entity => entity.Kind == JsonEntityKindDto.Definition)
            .OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entity =>
            {
                var relations = BuildChildren(entity);

                return new SourceInspectorCardVm
                {
                    EntityId = entity.Id,
                    Name = entity.Name,
                    IdentityKind = entity.IdentityKind,
                    Properties = entity.JsonProperties
                        .OrderBy(static property => property.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(property => new SourceInspectorPropertyVm
                        {
                            PropertyId = property.Id,
                            Name = property.Name,
                            IsKey = property.IsKey
                        })
                        .ToList(),
                    NestedRelations = relations.Nested,
                    HasRelations = relations.Has
                };
            })
            .ToList();
    }

    private static (IReadOnlyList<JebiUiRelationItem> Nested, IReadOnlyList<JebiUiRelationItem> Has) BuildChildren(JsonEntityClientDto entity)
    {
        var primitiveNames = entity.JsonProperties
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var children = entity.JsonEntities
            .Where(child => child.Kind != JsonEntityKindDto.Definition && !primitiveNames.Contains(child.Name))
            .Select(child =>
            {
                var definition = child.JsonEntities.FirstOrDefault(static candidate => candidate.Kind == JsonEntityKindDto.Definition);
                return (
                    Relation: new JebiUiRelationItem(
                        definition?.Name ?? child.Name,
                        child.Kind == JsonEntityKindDto.PropertyArray),
                    IsOwned: definition?.IdentityKind == JsonEntityIdentityKindDto.Owned
                );
            })
            .OrderBy(static child => child.Relation.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (
            children.Where(static child => child.IsOwned).Select(static child => child.Relation).ToList(),
            children.Where(static child => !child.IsOwned).Select(static child => child.Relation).ToList()
        );
    }
}
