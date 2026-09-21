using BlazorBootstrap;
using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GlpiNg.Modules.Management.Components.Pages.Contacts;

public partial class Detail : ComponentBase
{
    [Parameter]
    public int ContactId { get; set; }

    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ToastService ToastService { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    private Contact? _contact;
    private List<Supplier> _suppliers = [];
    private List<Supplier> _supplierCandidates = [];
    private List<HistoryRow> _history = [];

    private string _activeTab = "fiche";
    private int _supplierToAdd;
    private int _documentCount;
    private int _noteCount;
    private bool _isSaving;
    private string? _error;
    private string _currentUserName = "?";

    private IEnumerable<(string Key, string Label, string Icon, int? Count)> Tabs
    {
        get
        {
            yield return ("fiche", "Fiche", "ti-address-book", null);
            yield return ("fournisseurs", "Fournisseurs", "ti-truck", _suppliers.Count);
            yield return ("documents", "Documents", "ti-file", _documentCount);
            yield return ("notes", "Notes", "ti-notes", _noteCount);
            yield return ("historique", "Historique", "ti-history", _history.Count);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (AuthStateTask is not null)
        {
            AuthenticationState authState = await AuthStateTask;
            _currentUserName = authState.User.Identity?.Name ?? "?";
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        _contact = await db.Set<Contact>()
            .AsNoTracking()
            .FirstOrDefaultAsync(contact => contact.Id == ContactId);

        if (_contact is null)
        {
            return;
        }

        _suppliers = await db.Set<SupplierContact>()
            .AsNoTracking()
            .Where(link => link.ContactId == ContactId)
            .Select(link => link.Supplier!)
            .OrderBy(supplier => supplier.Name)
            .ToListAsync();

        List<int> attached = [.. _suppliers.Select(supplier => supplier.Id)];

        _supplierCandidates = await db.Set<Supplier>()
            .AsNoTracking()
            .Where(supplier => !attached.Contains(supplier.Id))
            .OrderBy(supplier => supplier.Name)
            .ToListAsync();

        _history = await db.Set<ManagementHistoryEntry>()
            .AsNoTracking()
            .Where(entry => entry.ItemType == ItemTypes.Contact && entry.ItemId == ContactId)
            .OrderByDescending(entry => entry.OccurredAt)
            .Select(entry => new HistoryRow(entry.OccurredAt, entry.User, entry.Field, entry.Description))
            .ToListAsync();
    }

    private async Task SaveAsync()
    {
        if (_contact is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_contact.Name))
        {
            _error = "Le nom est obligatoire.";
            return;
        }

        _error = null;
        _isSaving = true;

        try
        {
            await using DbContext db = await DbFactory.CreateDbContextAsync();

            Contact? stored = await db.Set<Contact>().FirstOrDefaultAsync(contact => contact.Id == ContactId);
            if (stored is null)
            {
                return;
            }

            ManagementHistoryRecorder history = new(ItemTypes.Contact, ContactId, _currentUserName);

            void Track(string field, string? before, string? after) => history.Track(field, before, after);

            Track("Nom", stored.Name, _contact.Name);
            Track("Prénom", stored.FirstName, _contact.FirstName);
            Track("Fonction", stored.Type, _contact.Type);
            Track("Titre", stored.Title, _contact.Title);
            Track("Adresse", stored.Address, _contact.Address);
            Track("Code postal", stored.PostCode, _contact.PostCode);
            Track("Ville", stored.Town, _contact.Town);
            Track("Pays", stored.Country, _contact.Country);
            Track("Téléphone", stored.Phone, _contact.Phone);
            Track("Téléphone 2", stored.Phone2, _contact.Phone2);
            Track("Mobile", stored.Mobile, _contact.Mobile);
            Track("Fax", stored.Fax, _contact.Fax);
            Track("Courriel", stored.Email, _contact.Email);
            Track("Commentaires", stored.Comment, _contact.Comment);
            Track("Actif", stored.IsActive ? "Oui" : "Non", _contact.IsActive ? "Oui" : "Non");

            stored.Name = _contact.Name.Trim();
            stored.FirstName = _contact.FirstName;
            stored.Type = _contact.Type;
            stored.Title = _contact.Title;
            stored.Address = _contact.Address;
            stored.PostCode = _contact.PostCode;
            stored.Town = _contact.Town;
            stored.Country = _contact.Country;
            stored.Phone = _contact.Phone;
            stored.Phone2 = _contact.Phone2;
            stored.Mobile = _contact.Mobile;
            stored.Fax = _contact.Fax;
            stored.Email = _contact.Email;
            stored.Comment = _contact.Comment;
            stored.IsActive = _contact.IsActive;

            if (history.HasChanges)
            {
                stored.UpdatedAt = DateTime.UtcNow;
                db.Set<ManagementHistoryEntry>().AddRange(history.Entries);
            }

            await db.SaveChangesAsync();

            ToastService.Notify(new ToastMessage(ToastType.Success, "Contact enregistré."));
            await LoadAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task AttachSupplierAsync()
    {
        if (_supplierToAdd == 0)
        {
            return;
        }

        await using DbContext db = await DbFactory.CreateDbContextAsync();

        db.Set<SupplierContact>().Add(new SupplierContact
        {
            SupplierId = _supplierToAdd,
            ContactId = ContactId,
        });

        await db.SaveChangesAsync();

        _supplierToAdd = 0;
        await LoadAsync();
    }

    private async Task DetachSupplierAsync(int supplierId)
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        SupplierContact? link = await db.Set<SupplierContact>()
            .FirstOrDefaultAsync(entry => entry.SupplierId == supplierId && entry.ContactId == ContactId);

        if (link is null)
        {
            return;
        }

        db.Set<SupplierContact>().Remove(link);
        await db.SaveChangesAsync();

        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        // Les rattachements partent avec la personne — voir la même règle dans la liste.
        List<SupplierContact> links = await db.Set<SupplierContact>()
            .Where(link => link.ContactId == ContactId)
            .ToListAsync();

        db.Set<SupplierContact>().RemoveRange(links);

        Contact? stored = await db.Set<Contact>().FirstOrDefaultAsync(contact => contact.Id == ContactId);
        if (stored is null)
        {
            return;
        }

        // L'historique suit l'objet : sans clé étrangère (table polymorphe), c'est ici qu'il part.
        List<ManagementHistoryEntry> history = await db.Set<ManagementHistoryEntry>()
            .Where(entry => entry.ItemType == ItemTypes.Contact && entry.ItemId == ContactId)
            .ToListAsync();

        db.Set<ManagementHistoryEntry>().RemoveRange(history);
        db.Set<Contact>().Remove(stored);
        await db.SaveChangesAsync();

        Navigation.NavigateTo("/management/contacts");
    }
}
