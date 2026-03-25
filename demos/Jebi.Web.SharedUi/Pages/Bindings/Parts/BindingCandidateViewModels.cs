using Jebi.Common.Features.Dtos.Model.Source;
using Jebi.Common.Features.Dtos.Model.Target;

namespace Jebi.Web.Pages.Bindings.Parts;

internal sealed class BindingSourceEntityCandidateVm
{
    public Guid EntityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<BindingSourceFieldCandidateVm> Fields { get; init; } = [];
}

internal sealed class BindingSourceFieldCandidateVm
{
    public Guid PropertyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayPath { get; init; } = string.Empty;
    public bool IsKey { get; init; }
    public bool IsReference { get; init; }
}

internal sealed class BindingTargetClassCandidateVm
{
    public Guid ClassId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsOwnedEntity { get; init; }
    public IReadOnlyList<BindingTargetFieldCandidateVm> Fields { get; init; } = [];
}

internal sealed class BindingTargetFieldCandidateVm
{
    public Guid PropertyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayPath { get; init; } = string.Empty;
    public bool IsKey { get; init; }
    public bool IsRequired { get; init; }
}

internal static class BindingCandidateProjection
{
    public static IReadOnlyList<BindingSourceEntityCandidateVm> CreateSourceCandidates(SchemaDetailDto? schema)
    {
        if (schema?.JsonEntities is null || schema.JsonEntities.Count == 0)
            return [];

        return EnumerateSourceEntities(schema.JsonEntities)
            .Where(static entity => entity.Kind == JsonEntityKindDto.Definition)
            .Select(entity => new BindingSourceEntityCandidateVm
            {
                EntityId = entity.Id,
                Name = entity.Name,
                Fields = entity.JsonProperties
                    .OrderByDescending(static property => property.IsKey)
                    .ThenBy(static property => property.IsReference)
                    .ThenBy(static property => property.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(property => new BindingSourceFieldCandidateVm
                    {
                        PropertyId = property.Id,
                        Name = property.Name,
                        DisplayPath = BuildDisplayPath(entity.Name, property.Name, property.FullName),
                        IsKey = property.IsKey,
                        IsReference = property.IsReference
                    })
                    .ToList()
            })
            .Where(static entity => entity.Fields.Count > 0)
            .OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<BindingTargetClassCandidateVm> CreateTargetCandidates(TargetContextDetailDto? context)
    {
        if (context?.ClassInfos is null || context.ClassInfos.Count == 0)
            return [];

        return EnumerateTargetClasses(context.ClassInfos)
            .Select(@class => new BindingTargetClassCandidateVm
            {
                ClassId = @class.Id,
                Name = @class.Name,
                IsOwnedEntity = @class.IsOwnedEntity,
                Fields = @class.PropertyInfos
                    .Where(static property => !property.IsForeignKey)
                    .OrderByDescending(static property => property.IsKey)
                    .ThenByDescending(static property => property.IsRequired)
                    .ThenBy(static property => property.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(property => new BindingTargetFieldCandidateVm
                    {
                        PropertyId = property.Id,
                        Name = property.Name,
                        DisplayPath = BuildDisplayPath(@class.Name, property.Name, property.FullName),
                        IsKey = property.IsKey,
                        IsRequired = property.IsRequired
                    })
                    .ToList()
            })
            .Where(static @class => @class.Fields.Count > 0)
            .OrderBy(static @class => @class.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<JsonEntityClientDto> EnumerateSourceEntities(IEnumerable<JsonEntityClientDto>? entities)
    {
        if (entities is null)
            yield break;

        var seen = new HashSet<Guid>();

        foreach (var entity in EnumerateSourceEntities(entities, seen))
            yield return entity;
    }

    private static IEnumerable<JsonEntityClientDto> EnumerateSourceEntities(
        IEnumerable<JsonEntityClientDto>? entities,
        ISet<Guid> seen)
    {
        if (entities is null)
            yield break;

        foreach (var entity in entities)
        {
            if (!seen.Add(entity.Id))
                continue;

            yield return entity;

            foreach (var child in EnumerateSourceEntities(entity.JsonEntities, seen))
                yield return child;
        }
    }

    private static IEnumerable<TargetClassInfoDto> EnumerateTargetClasses(IEnumerable<TargetClassInfoDto>? classes)
    {
        if (classes is null)
            yield break;

        var seen = new HashSet<Guid>();

        foreach (var @class in EnumerateTargetClasses(classes, seen))
            yield return @class;
    }

    private static IEnumerable<TargetClassInfoDto> EnumerateTargetClasses(
        IEnumerable<TargetClassInfoDto>? classes,
        ISet<Guid> seen)
    {
        if (classes is null)
            yield break;

        foreach (var @class in classes)
        {
            if (!seen.Add(@class.Id))
                continue;

            yield return @class;

            foreach (var child in EnumerateTargetClasses(@class.ClassInfos, seen))
                yield return child;
        }
    }

    private static string BuildDisplayPath(string parentName, string propertyName, string? fullName) =>
        string.IsNullOrWhiteSpace(fullName)
            ? $"{parentName}.{propertyName}"
            : fullName;
}
