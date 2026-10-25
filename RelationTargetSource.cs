using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.EntityFrameworkCore;
using Types = GlpiNg.Modules.Abstractions.Items.ItemTypes;

namespace GlpiNg.Modules.Management;

/// <summary>Certificats, domaines, applicatifs et projets comme cibles de rattachement — voir <see cref="IRelationTargetSource"/>.</summary>
public sealed class RelationTargetSource(IDbContextFactory<DbContext> dbFactory) : IRelationTargetSource
{
    public IReadOnlyCollection<string> SupportedTypes { get; } =
        [Types.Certificate, Types.Domain,
         Types.Appliance, Types.Project, Types.Cluster, Types.Datacenter, Types.DatabaseInstance];

    public async Task<IReadOnlyList<RelationTarget>> SearchAsync(string itemType, string? term, int limit, CancellationToken cancellationToken = default)
    {
        await using DbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);
        string like = term?.Trim() ?? string.Empty;

        return itemType switch
        {
            Types.Certificate => Map(itemType, "certificates", await db.Set<Certificate>().AsNoTracking()
                .Where(x => x.Name.Contains(like)).OrderBy(x => x.Name).Take(limit).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Domain => Map(itemType, "domains", await db.Set<Domain>().AsNoTracking()
                .Where(x => x.Name.Contains(like)).OrderBy(x => x.Name).Take(limit).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Appliance => Map(itemType, "appliances", await db.Set<Appliance>().AsNoTracking()
                .Where(x => x.Name.Contains(like)).OrderBy(x => x.Name).Take(limit).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Project => Map(itemType, "projects", await db.Set<Project>().AsNoTracking()
                .Where(x => x.Name.Contains(like)).OrderBy(x => x.Name).Take(limit).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Cluster => Map(itemType, "clusters", await db.Set<Cluster>().AsNoTracking()
                .Where(x => x.Name.Contains(like)).OrderBy(x => x.Name).Take(limit).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Datacenter => Map(itemType, "datacenters", await db.Set<Datacenter>().AsNoTracking()
                .Where(x => x.Name.Contains(like)).OrderBy(x => x.Name).Take(limit).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.DatabaseInstance => Map(itemType, "databases", await db.Set<DatabaseInstance>().AsNoTracking()
                .Where(x => x.Name.Contains(like)).OrderBy(x => x.Name).Take(limit).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            _ => [],
        };
    }

    public async Task<IReadOnlyList<RelationTarget>> ResolveAsync(string itemType, IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        await using DbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return itemType switch
        {
            Types.Certificate => Map(itemType, "certificates", await db.Set<Certificate>().AsNoTracking()
                .Where(x => ids.Contains(x.Id)).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Domain => Map(itemType, "domains", await db.Set<Domain>().AsNoTracking()
                .Where(x => ids.Contains(x.Id)).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Appliance => Map(itemType, "appliances", await db.Set<Appliance>().AsNoTracking()
                .Where(x => ids.Contains(x.Id)).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Project => Map(itemType, "projects", await db.Set<Project>().AsNoTracking()
                .Where(x => ids.Contains(x.Id)).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Cluster => Map(itemType, "clusters", await db.Set<Cluster>().AsNoTracking()
                .Where(x => ids.Contains(x.Id)).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.Datacenter => Map(itemType, "datacenters", await db.Set<Datacenter>().AsNoTracking()
                .Where(x => ids.Contains(x.Id)).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            Types.DatabaseInstance => Map(itemType, "databases", await db.Set<DatabaseInstance>().AsNoTracking()
                .Where(x => ids.Contains(x.Id)).Select(x => new Row(x.Id, x.Name)).ToListAsync(cancellationToken)),
            _ => [],
        };
    }

    private sealed record Row(int Id, string Name);

    private static List<RelationTarget> Map(string itemType, string route, List<Row> rows) =>
        [.. rows.Select(r => new RelationTarget(itemType, r.Id, r.Name, $"/management/{route}/{r.Id}"))];
}
