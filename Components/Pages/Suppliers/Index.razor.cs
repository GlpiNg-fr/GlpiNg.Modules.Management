using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace GlpiNg.Modules.Management.Components.Pages.Suppliers;

public partial class Index : ComponentBase
{
    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private List<Supplier> _suppliers = [];
    private List<Supplier> _filtered = [];
    private readonly HashSet<int> _selectedIds = [];
    private string _searchTerm = string.Empty;
    private string? _deleteError;

    private Supplier _newSupplier = NewBlank();

    private bool AllSelected => _filtered.Count > 0 && _selectedIds.Count == _filtered.Count;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        _suppliers = await db.Set<Supplier>()
            .AsNoTracking()
            .Include(supplier => supplier.Contacts)
            .Include(supplier => supplier.Contracts)
            .OrderBy(supplier => supplier.Name)
            .ToListAsync();

        _selectedIds.Clear();
        ApplyFilter();
    }

    private void OnSearchChanged(KeyboardEventArgs args) => ApplyFilter();

    private void ApplyFilter()
    {
        string term = _searchTerm.Trim();

        _filtered = term.Length == 0
            ? _suppliers
            : [.. _suppliers.Where(supplier =>
                supplier.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (supplier.Type?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (supplier.Town?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (supplier.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];

        _selectedIds.IntersectWith(_filtered.Select(supplier => supplier.Id));
    }

    private void ToggleSelectAll(bool selectAll)
    {
        _selectedIds.Clear();
        if (selectAll)
        {
            foreach (Supplier supplier in _filtered)
            {
                _selectedIds.Add(supplier.Id);
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

        // Un fournisseur encore engagé par un contrat ne se supprime pas : la ligne de liaison
        // refuse de partir (voir la relation dans GlpiNgDbContext), et un message vaut mieux que
        // l'erreur de base de données qui remonterait sinon.
        List<string> engaged = await db.Set<ContractSupplier>()
            .AsNoTracking()
            .Where(link => _selectedIds.Contains(link.SupplierId))
            .Select(link => link.Supplier!.Name)
            .Distinct()
            .ToListAsync();

        if (engaged.Count > 0)
        {
            _deleteError = $"Impossible de supprimer : {string.Join(", ", engaged)} "
                           + "figure(nt) encore sur un contrat. Détachez-les du contrat d'abord.";
            return;
        }

        _deleteError = null;

        List<Supplier> toDelete = await db.Set<Supplier>()
            .Where(supplier => _selectedIds.Contains(supplier.Id))
            .ToListAsync();

        db.Set<Supplier>().RemoveRange(toDelete);
        await db.SaveChangesAsync();

        await LoadAsync();
    }

    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(_newSupplier.Name))
        {
            return;
        }

        await using DbContext db = await DbFactory.CreateDbContextAsync();

        db.Set<Supplier>().Add(_newSupplier);
        await db.SaveChangesAsync();

        _newSupplier = NewBlank();

        await JS.InvokeVoidAsync("glpiNg.hideModal", "newSupplierModal");
        await LoadAsync();
    }

    private static Supplier NewBlank() => new() { Name = string.Empty };
}
