using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace GlpiNg.Modules.Management.Components.Pages.Budgets;

public partial class Index : ComponentBase
{
    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private List<Budget> _budgets = [];
    private List<Budget> _filtered = [];

    /// <summary>Total imputé par budget, calculé en base : additionner en mémoire obligerait à charger tous les coûts.</summary>
    private Dictionary<int, decimal> _spentByBudget = [];

    private readonly HashSet<int> _selectedIds = [];
    private string _searchTerm = string.Empty;

    private Budget _newBudget = NewBlank();

    private bool AllSelected => _filtered.Count > 0 && _selectedIds.Count == _filtered.Count;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        _budgets = await db.Set<Budget>()
            .AsNoTracking()
            .OrderBy(budget => budget.Name)
            .ToListAsync();

        _spentByBudget = await db.Set<ContractCost>()
            .AsNoTracking()
            .Where(cost => cost.BudgetId != null)
            .GroupBy(cost => cost.BudgetId!.Value)
            .Select(group => new { BudgetId = group.Key, Total = group.Sum(cost => cost.Amount) })
            .ToDictionaryAsync(entry => entry.BudgetId, entry => entry.Total);

        _selectedIds.Clear();
        ApplyFilter();
    }

    private void OnSearchChanged(KeyboardEventArgs args) => ApplyFilter();

    private void ApplyFilter()
    {
        string term = _searchTerm.Trim();

        _filtered = term.Length == 0
            ? _budgets
            : [.. _budgets.Where(budget =>
                budget.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (budget.Code?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (budget.Type?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];

        _selectedIds.IntersectWith(_filtered.Select(budget => budget.Id));
    }

    private void ToggleSelectAll(bool selectAll)
    {
        _selectedIds.Clear();
        if (selectAll)
        {
            foreach (Budget budget in _filtered)
            {
                _selectedIds.Add(budget.Id);
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

        List<Budget> toDelete = await db.Set<Budget>()
            .Where(budget => _selectedIds.Contains(budget.Id))
            .ToListAsync();

        // Les coûts imputés survivent, sans budget : la dépense a bien eu lieu, c'est l'enveloppe
        // qui disparaît (relation en SetNull, voir GlpiNgDbContext).
        db.Set<Budget>().RemoveRange(toDelete);
        await db.SaveChangesAsync();

        await LoadAsync();
    }

    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(_newBudget.Name))
        {
            return;
        }

        await using DbContext db = await DbFactory.CreateDbContextAsync();

        db.Set<Budget>().Add(_newBudget);
        await db.SaveChangesAsync();

        _newBudget = NewBlank();

        await JS.InvokeVoidAsync("glping.hideModal", "newBudgetModal");
        await LoadAsync();
    }

    private static Budget NewBlank() => new() { Name = string.Empty };
}
