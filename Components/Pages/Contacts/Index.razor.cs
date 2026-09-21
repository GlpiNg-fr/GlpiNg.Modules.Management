using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace GlpiNg.Modules.Management.Components.Pages.Contacts;

public partial class Index : ComponentBase
{
    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private List<Contact> _contacts = [];
    private List<Contact> _filtered = [];
    private readonly HashSet<int> _selectedIds = [];
    private string _searchTerm = string.Empty;

    private Contact _newContact = NewBlank();

    private bool AllSelected => _filtered.Count > 0 && _selectedIds.Count == _filtered.Count;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        _contacts = await db.Set<Contact>()
            .AsNoTracking()
            .Include(contact => contact.Suppliers)
            .OrderBy(contact => contact.Name)
            .ThenBy(contact => contact.FirstName)
            .ToListAsync();

        _selectedIds.Clear();
        ApplyFilter();
    }

    private void OnSearchChanged(KeyboardEventArgs args) => ApplyFilter();

    private void ApplyFilter()
    {
        string term = _searchTerm.Trim();

        _filtered = term.Length == 0
            ? _contacts
            : [.. _contacts.Where(contact =>
                contact.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (contact.Type?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (contact.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (contact.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];

        _selectedIds.IntersectWith(_filtered.Select(contact => contact.Id));
    }

    private void ToggleSelectAll(bool selectAll)
    {
        _selectedIds.Clear();
        if (selectAll)
        {
            foreach (Contact contact in _filtered)
            {
                _selectedIds.Add(contact.Id);
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

        // Les rattachements aux fournisseurs partent avec le contact : c'est la personne qui
        // disparaît, et la ligne de liaison ne dit plus rien sans elle. La relation côté base est
        // en Restrict (voir GlpiNgDbContext), d'où cette suppression explicite.
        List<SupplierContact> links = await db.Set<SupplierContact>()
            .Where(link => _selectedIds.Contains(link.ContactId))
            .ToListAsync();

        db.Set<SupplierContact>().RemoveRange(links);

        List<Contact> toDelete = await db.Set<Contact>()
            .Where(contact => _selectedIds.Contains(contact.Id))
            .ToListAsync();

        db.Set<Contact>().RemoveRange(toDelete);
        await db.SaveChangesAsync();

        await LoadAsync();
    }

    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(_newContact.Name))
        {
            return;
        }

        await using DbContext db = await DbFactory.CreateDbContextAsync();

        db.Set<Contact>().Add(_newContact);
        await db.SaveChangesAsync();

        _newContact = NewBlank();

        await JS.InvokeVoidAsync("glpiNg.hideModal", "newContactModal");
        await LoadAsync();
    }

    private static Contact NewBlank() => new() { Name = string.Empty };
}
