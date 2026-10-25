using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.EntityFrameworkCore;

namespace GlpiNg.Modules.Management;

/// <summary>Voir <see cref="IItemContracts"/>.</summary>
public sealed class ItemContractsService(IDbContextFactory<DbContext> dbFactory, IEnumerable<IRelationTargetSource> sources) : IItemContracts
{
    public async Task<IReadOnlyList<ItemContract>> GetForItemAsync(string itemType, int itemId, CancellationToken cancellationToken = default)
    {
        await using DbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // EndDate est calculée (non stockée) : on charge les contrats puis on la lit en mémoire.
        var rows = await db.Set<ContractItem>().AsNoTracking()
            .Where(l => l.ItemType == itemType && l.ItemId == itemId)
            .Select(l => new { l.Id, l.Contract })
            .ToListAsync(cancellationToken);

        return [.. rows.Where(r => r.Contract is not null)
            .Select(r => new ItemContract(r.Id, r.Contract!.Id, r.Contract.Name, r.Contract.Number, r.Contract.StartDate, r.Contract.EndDate))
            .OrderBy(c => c.Name)];
    }

    public async Task<IReadOnlyList<ContractOption>> GetAvailableAsync(string itemType, int itemId, CancellationToken cancellationToken = default)
    {
        await using DbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var linked = db.Set<ContractItem>().Where(l => l.ItemType == itemType && l.ItemId == itemId).Select(l => l.ContractId);
        return [.. (await db.Set<Contract>().AsNoTracking().Where(c => !linked.Contains(c.Id))
            .OrderBy(c => c.Name).Select(c => new { c.Id, c.Name }).ToListAsync(cancellationToken))
            .Select(c => new ContractOption(c.Id, c.Name))];
    }

    public async Task AttachAsync(string itemType, int itemId, int contractId, CancellationToken cancellationToken = default)
    {
        await using DbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        if (await db.Set<ContractItem>().AnyAsync(l => l.ContractId == contractId && l.ItemType == itemType && l.ItemId == itemId, cancellationToken))
        {
            return;
        }

        db.Set<ContractItem>().Add(new ContractItem { ContractId = contractId, ItemType = itemType, ItemId = itemId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DetachAsync(int linkId, CancellationToken cancellationToken = default)
    {
        await using DbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Set<ContractItem>().Where(l => l.Id == linkId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RelationTarget>> GetItemsAsync(int contractId, CancellationToken cancellationToken = default)
    {
        await using DbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var rows = await db.Set<ContractItem>().AsNoTracking().Where(l => l.ContractId == contractId)
            .Select(l => new { l.ItemType, l.ItemId }).ToListAsync(cancellationToken);

        List<RelationTarget> targets = [];
        foreach (var group in rows.GroupBy(r => r.ItemType))
        {
            foreach (IRelationTargetSource source in sources.Where(s => s.SupportedTypes.Contains(group.Key)))
            {
                targets.AddRange(await source.ResolveAsync(group.Key, [.. group.Select(g => g.ItemId)], cancellationToken));
            }
        }

        return [.. targets.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)];
    }
}
