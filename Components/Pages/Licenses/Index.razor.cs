using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace GlpiNg.Modules.Management.Components.Pages.Licenses;

public partial class Index : ComponentBase
{
    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private List<SoftwareLicense> _items = [];
    private List<SoftwareLicense> _filtered = [];
    private readonly HashSet<int> _selectedIds = [];
    private string _searchTerm = string.Empty;

    private SoftwareLicense _newItem = NewBlank();

    private bool AllSelected => _filtered.Count > 0 && _selectedIds.Count == _filtered.Count;

    private bool _expiringOnly;

    private void OnExpiringOnlyChanged(bool value)
    {
        _expiringOnly = value;
        ApplyFilter();
    }

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        _items = await db.Set<SoftwareLicense>()
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

        IEnumerable<SoftwareLicense> query = _items;

        if (term.Length > 0)
        {
            query = query.Where(item =>
                item.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (item.SoftwareName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.Type?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            );
        }

        if (_expiringOnly)
        {
            query = query.Where(item =>
                ExpirationStatus.IsExpired(item.ExpirationDate) || ExpirationStatus.IsExpiringSoon(item.ExpirationDate));
        }

        _filtered = [.. query];
        _selectedIds.IntersectWith(_filtered.Select(item => item.Id));
    }

    private void ToggleSelectAll(bool selectAll)
    {
        _selectedIds.Clear();
        if (selectAll)
        {
            foreach (SoftwareLicense item in _filtered)
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

        List<SoftwareLicense> toDelete = await db.Set<SoftwareLicense>()
            .Where(item => _selectedIds.Contains(item.Id))
            .ToListAsync();

        // L'historique est polymorphe, donc sans clé étrangère : il part à la main avec les fiches.
        List<ManagementHistoryEntry> history = await db.Set<ManagementHistoryEntry>()
            .Where(entry => entry.ItemType == ItemTypes.SoftwareLicense && _selectedIds.Contains(entry.ItemId))
            .ToListAsync();

        db.Set<ManagementHistoryEntry>().RemoveRange(history);
        db.Set<SoftwareLicense>().RemoveRange(toDelete);
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

        db.Set<SoftwareLicense>().Add(_newItem);
        await db.SaveChangesAsync();

        _newItem = NewBlank();

        await JS.InvokeVoidAsync("glping.hideModal", "newSoftwareLicenseModal");
        await LoadAsync();
    }

    private static SoftwareLicense NewBlank() => new() { Name = string.Empty };
}
