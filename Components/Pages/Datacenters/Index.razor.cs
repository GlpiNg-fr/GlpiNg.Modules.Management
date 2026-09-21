using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace GlpiNg.Modules.Management.Components.Pages.Datacenters;

public partial class Index : ComponentBase
{
    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private List<Datacenter> _items = [];
    private List<Datacenter> _filtered = [];
    private readonly HashSet<int> _selectedIds = [];
    private string _searchTerm = string.Empty;

    private Datacenter _newItem = NewBlank();

    private bool AllSelected => _filtered.Count > 0 && _selectedIds.Count == _filtered.Count;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        _items = await db.Set<Datacenter>()
            .AsNoTracking()
            .Include(item => item.Supplier)
            .OrderBy(item => item.Name)
            .ToListAsync();

        _selectedIds.Clear();
        ApplyFilter();
    }

    private void OnSearchChanged(KeyboardEventArgs args) => ApplyFilter();

    private void ApplyFilter()
    {
        string term = _searchTerm.Trim();

        IEnumerable<Datacenter> query = _items;

        if (term.Length > 0)
        {
            query = query.Where(item =>
                item.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (item.Location?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.Town?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.Country?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            );
        }

        _filtered = [.. query];
        _selectedIds.IntersectWith(_filtered.Select(item => item.Id));
    }

    private void ToggleSelectAll(bool selectAll)
    {
        _selectedIds.Clear();
        if (selectAll)
        {
            foreach (Datacenter item in _filtered)
            {
                _selectedIds.Add(item.Id);
            }
        }
    }

    private void ToggleSelect(int id, bool selected)
    {
        if (selected)
        {
            _selectedIds.Add(id);
        }
        else
        {
            _selectedIds.Remove(id);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (_selectedIds.Count == 0)
        {
            return;
        }

        await using DbContext db = await DbFactory.CreateDbContextAsync();

        List<Datacenter> toDelete = await db.Set<Datacenter>()
            .Where(item => _selectedIds.Contains(item.Id))
            .ToListAsync();

        // L'historique est polymorphe, donc sans clé étrangère : il part à la main avec les fiches.
        List<ManagementHistoryEntry> history = await db.Set<ManagementHistoryEntry>()
            .Where(entry => entry.ItemType == ItemTypes.Datacenter && _selectedIds.Contains(entry.ItemId))
            .ToListAsync();

        db.Set<ManagementHistoryEntry>().RemoveRange(history);
        db.Set<Datacenter>().RemoveRange(toDelete);
        await db.SaveChangesAsync();

        await LoadAsync();
    }

    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(_newItem.Name))
        {
            return;
        }

        await using DbContext db = await DbFactory.CreateDbContextAsync();

        db.Set<Datacenter>().Add(_newItem);
        await db.SaveChangesAsync();

        _newItem = NewBlank();

        await JS.InvokeVoidAsync("glpiNg.hideModal", "newDatacenterModal");
        await LoadAsync();
    }

    private static Datacenter NewBlank() => new() { Name = string.Empty };
}
