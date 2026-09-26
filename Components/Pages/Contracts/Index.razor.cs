using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace GlpiNg.Modules.Management.Components.Pages.Contracts;

public partial class Index : ComponentBase
{
    /// <summary>Fenêtre du filtre « préavis proche » : un trimestre, le délai courant d'un préavis contractuel.</summary>
    private const int UrgentNoticeDays = 90;

    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private List<Contract> _contracts = [];
    private List<Contract> _filtered = [];
    private readonly HashSet<int> _selectedIds = [];
    private string _searchTerm = string.Empty;
    private bool _expiringOnly;

    private Contract _newContract = NewBlank();

    private bool AllSelected => _filtered.Count > 0 && _selectedIds.Count == _filtered.Count;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        _contracts = await db.Set<Contract>()
            .AsNoTracking()
            .Include(contract => contract.Suppliers).ThenInclude(link => link.Supplier)
            .Include(contract => contract.Costs)
            .OrderBy(contract => contract.Name)
            .ToListAsync();

        _selectedIds.Clear();
        ApplyFilter();
    }

    private void OnSearchChanged(KeyboardEventArgs args) => ApplyFilter();

    private void OnExpiringOnlyChanged(bool value)
    {
        _expiringOnly = value;
        ApplyFilter();
    }

    /// <summary>Un préavis est urgent s'il tombe dans la fenêtre — ou s'il est déjà passé, ce qui l'est encore plus.</summary>
    private static bool IsUrgent(DateTime deadline) =>
        deadline <= DateTime.Today.AddDays(UrgentNoticeDays);

    private void ApplyFilter()
    {
        string term = _searchTerm.Trim();

        IEnumerable<Contract> query = _contracts;

        if (term.Length > 0)
        {
            query = query.Where(contract =>
                contract.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (contract.Number?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (contract.Type?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || contract.Suppliers.Any(link =>
                    link.Supplier!.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (_expiringOnly)
        {
            query = query.Where(contract => contract.NoticeDeadline is { } deadline && IsUrgent(deadline));
        }

        _filtered = [.. query];
        _selectedIds.IntersectWith(_filtered.Select(contract => contract.Id));
    }

    private void ToggleSelectAll(bool selectAll)
    {
        _selectedIds.Clear();
        if (selectAll)
        {
            foreach (Contract contract in _filtered)
            {
                _selectedIds.Add(contract.Id);
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

        List<Contract> toDelete = await db.Set<Contract>()
            .Where(contract => _selectedIds.Contains(contract.Id))
            .ToListAsync();

        // Les coûts et les rattachements aux fournisseurs partent en cascade avec le contrat
        // (voir GlpiNgDbContext) : ils ne décrivent que lui.
        db.Set<Contract>().RemoveRange(toDelete);
        await db.SaveChangesAsync();

        await LoadAsync();
    }

    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(_newContract.Name))
        {
            return;
        }

        await using DbContext db = await DbFactory.CreateDbContextAsync();

        db.Set<Contract>().Add(_newContract);
        await db.SaveChangesAsync();

        _newContract = NewBlank();

        await JS.InvokeVoidAsync("glping.hideModal", "newContractModal");
        await LoadAsync();
    }

    private static Contract NewBlank() => new() { Name = string.Empty };
}
