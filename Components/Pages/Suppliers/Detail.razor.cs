using BlazorBootstrap;
using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using GlpiNg.Modules.Abstractions.Localization;

namespace GlpiNg.Modules.Management.Components.Pages.Suppliers;

public partial class Detail : ComponentBase
{
    [Parameter]
    public int SupplierId { get; set; }

    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ToastService ToastService { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    private Supplier? _supplier;
    private List<Contact> _contacts = [];
    private List<Contact> _contactCandidates = [];
    private List<Contract> _contracts = [];
    private List<HistoryRow> _history = [];

    private string _activeTab = "fiche";
    private int _contactToAdd;
    private int _documentCount;
    private int _noteCount;
    private bool _isSaving;
    private string? _error;
    private string _currentUserName = "?";

    private IEnumerable<(string Key, string Label, string Icon, int? Count)> Tabs
    {
        get
        {
            yield return ("fiche", "Fiche", "ti-truck", null);
            yield return ("contacts", "Contacts", "ti-address-book", _contacts.Count);
            yield return ("contrats", "Contrats", "ti-file-text", _contracts.Count);
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

        _supplier = await db.Set<Supplier>()
            .AsNoTracking()
            .FirstOrDefaultAsync(supplier => supplier.Id == SupplierId);

        if (_supplier is null)
        {
            return;
        }

        _contacts = await db.Set<SupplierContact>()
            .AsNoTracking()
            .Where(link => link.SupplierId == SupplierId)
            .Select(link => link.Contact!)
            .OrderBy(contact => contact.Name)
            .ToListAsync();

        // Seuls les contacts pas encore rattachés sont proposés : reproposer les autres inviterait
        // à créer un doublon que l'index unique refuserait de toute façon.
        List<int> attached = [.. _contacts.Select(contact => contact.Id)];

        _contactCandidates = await db.Set<Contact>()
            .AsNoTracking()
            .Where(contact => !attached.Contains(contact.Id))
            .OrderBy(contact => contact.Name)
            .ToListAsync();

        _contracts = await db.Set<ContractSupplier>()
            .AsNoTracking()
            .Where(link => link.SupplierId == SupplierId)
            .Select(link => link.Contract!)
            .OrderBy(contract => contract.Name)
            .ToListAsync();

        _history = await db.Set<ManagementHistoryEntry>()
            .AsNoTracking()
            .Where(entry => entry.ItemType == ItemTypes.Supplier && entry.ItemId == SupplierId)
            .OrderByDescending(entry => entry.OccurredAt)
            .Select(entry => new HistoryRow(entry.OccurredAt, entry.User, entry.Field, entry.Description))
            .ToListAsync();
    }

    private async Task SaveAsync()
    {
        if (_supplier is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_supplier.Name))
        {
            _error = Tr.T("Le nom est obligatoire.");
            return;
        }

        _error = null;
        _isSaving = true;

        try
        {
            await using DbContext db = await DbFactory.CreateDbContextAsync();

            Supplier? stored = await db.Set<Supplier>().FirstOrDefaultAsync(supplier => supplier.Id == SupplierId);
            if (stored is null)
            {
                return;
            }

            ManagementHistoryRecorder history = new(ItemTypes.Supplier, SupplierId, _currentUserName);

            void Track(string field, string? before, string? after) => history.Track(field, before, after);

            Track("Nom", stored.Name, _supplier.Name);
            Track("Type", stored.Type, _supplier.Type);
            Track("Numéro d'immatriculation", stored.RegistrationNumber, _supplier.RegistrationNumber);
            Track("Adresse", stored.Address, _supplier.Address);
            Track("Code postal", stored.PostCode, _supplier.PostCode);
            Track("Ville", stored.Town, _supplier.Town);
            Track("Pays", stored.Country, _supplier.Country);
            Track("Téléphone", stored.Phone, _supplier.Phone);
            Track("Fax", stored.Fax, _supplier.Fax);
            Track("Courriel", stored.Email, _supplier.Email);
            Track("Site web", stored.Website, _supplier.Website);
            Track("Commentaires", stored.Comment, _supplier.Comment);
            Track("Actif", stored.IsActive ? "Oui" : "Non", _supplier.IsActive ? "Oui" : "Non");

            stored.Name = _supplier.Name.Trim();
            stored.Type = _supplier.Type;
            stored.RegistrationNumber = _supplier.RegistrationNumber;
            stored.Address = _supplier.Address;
            stored.PostCode = _supplier.PostCode;
            stored.Town = _supplier.Town;
            stored.Country = _supplier.Country;
            stored.Phone = _supplier.Phone;
            stored.Fax = _supplier.Fax;
            stored.Email = _supplier.Email;
            stored.Website = _supplier.Website;
            stored.Comment = _supplier.Comment;
            stored.IsActive = _supplier.IsActive;

            if (history.HasChanges)
            {
                stored.UpdatedAt = DateTime.UtcNow;
                db.Set<ManagementHistoryEntry>().AddRange(history.Entries);
            }

            await db.SaveChangesAsync();

            ToastService.Notify(new ToastMessage(ToastType.Success, Tr.T("Fournisseur enregistré.")));
            await LoadAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task AttachContactAsync()
    {
        if (_contactToAdd == 0)
        {
            return;
        }

        await using DbContext db = await DbFactory.CreateDbContextAsync();

        db.Set<SupplierContact>().Add(new SupplierContact
        {
            SupplierId = SupplierId,
            ContactId = _contactToAdd,
        });

        await db.SaveChangesAsync();

        _contactToAdd = 0;
        await LoadAsync();
    }

    private async Task DetachContactAsync(int contactId)
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        SupplierContact? link = await db.Set<SupplierContact>()
            .FirstOrDefaultAsync(entry => entry.SupplierId == SupplierId && entry.ContactId == contactId);

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

        if (await db.Set<ContractSupplier>().AnyAsync(link => link.SupplierId == SupplierId))
        {
            _error = Tr.T("Ce fournisseur figure encore sur un contrat : détachez-le du contrat avant de le supprimer.");
            _activeTab = "fiche";
            return;
        }

        Supplier? stored = await db.Set<Supplier>().FirstOrDefaultAsync(supplier => supplier.Id == SupplierId);
        if (stored is null)
        {
            return;
        }

        // L'historique suit l'objet : sans clé étrangère (table polymorphe), c'est ici qu'il part.
        List<ManagementHistoryEntry> history = await db.Set<ManagementHistoryEntry>()
            .Where(entry => entry.ItemType == ItemTypes.Supplier && entry.ItemId == SupplierId)
            .ToListAsync();

        db.Set<ManagementHistoryEntry>().RemoveRange(history);
        db.Set<Supplier>().Remove(stored);
        await db.SaveChangesAsync();

        Navigation.NavigateTo("/management/suppliers");
    }
}
