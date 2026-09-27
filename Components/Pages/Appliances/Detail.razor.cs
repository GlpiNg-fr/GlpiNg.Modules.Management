using BlazorBootstrap;
using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using GlpiNg.Modules.Abstractions.Localization;

namespace GlpiNg.Modules.Management.Components.Pages.Appliances;

public partial class Detail : ComponentBase
{
    [Parameter]
    public int ApplianceId { get; set; }

    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ToastService ToastService { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    private Appliance? _item;
    private List<HistoryRow> _history = [];
    private List<Supplier> _suppliers = [];
    private List<Contract> _contracts = [];
    private int _supplierId;
    private int _contractId;

    private string _activeTab = "fiche";
    private int _documentCount;
    private int _noteCount;
    private bool _isSaving;
    private string? _error;
    private string _currentUserName = "?";

    private IEnumerable<(string Key, string Label, string Icon, int? Count)> Tabs
    {
        get
        {
            yield return ("fiche", "Fiche", "ti-apps", null);
            yield return ("documents", "Documents", "ti-file", _documentCount);
            yield return ("notes", "Notes", "ti-notes", _noteCount);
            yield return ("historique", "Historique", "ti-history", _history.Count);
        }
    }

    /// <summary>Nom du tiers, pour que l'historique dise « Dell » plutôt que « 4 ».</summary>
    private string? SupplierName(int? id) =>
        id is { } value ? _suppliers.FirstOrDefault(supplier => supplier.Id == value)?.Name : null;

    private string? ContractName(int? id) =>
        id is { } value ? _contracts.FirstOrDefault(contract => contract.Id == value)?.Name : null;

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

        _item = await db.Set<Appliance>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == ApplianceId);

        if (_item is null)
        {
            return;
        }

        _suppliers = await db.Set<Supplier>()
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .ToListAsync();

        _contracts = await db.Set<Contract>()
            .AsNoTracking()
            .OrderBy(contract => contract.Name)
            .ToListAsync();

        _supplierId = _item.SupplierId ?? 0;
        _contractId = _item.ContractId ?? 0;

        _history = await db.Set<ManagementHistoryEntry>()
            .AsNoTracking()
            .Where(entry => entry.ItemType == ItemTypes.Appliance && entry.ItemId == ApplianceId)
            .OrderByDescending(entry => entry.OccurredAt)
            .Select(entry => new HistoryRow(entry.OccurredAt, entry.User, entry.Field, entry.Description))
            .ToListAsync();
    }

    private async Task SaveAsync()
    {
        if (_item is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_item.Name))
        {
            _error = Tr.T("Le nom est obligatoire.");
            return;
        }

        _error = null;
        _isSaving = true;

        try
        {
            await using DbContext db = await DbFactory.CreateDbContextAsync();

            Appliance? stored = await db.Set<Appliance>().FirstOrDefaultAsync(item => item.Id == ApplianceId);
            if (stored is null)
            {
                return;
            }

            ManagementHistoryRecorder history = new(ItemTypes.Appliance, ApplianceId, _currentUserName);

            history.Track("Nom", stored.Name, _item.Name);
            history.Track("Type", stored.Type, _item.Type);
            history.Track("Environnement", stored.Environment, _item.Environment);
            history.Track("Version", stored.Version, _item.Version);
            history.Track("Adresse (URL)", stored.Url, _item.Url);
            history.Track("Responsable", stored.OwnerName, _item.OwnerName);
            history.Track("Fournisseur", SupplierName(stored.SupplierId), SupplierName(_supplierId == 0 ? null : _supplierId));
            history.Track("Contrat", ContractName(stored.ContractId), ContractName(_contractId == 0 ? null : _contractId));
            history.Track("Actif", stored.IsActive, _item.IsActive);
            history.Track("Commentaires", stored.Comment, _item.Comment);

            stored.Name = _item.Name.Trim();
            stored.Type = _item.Type;
            stored.Environment = _item.Environment;
            stored.Version = _item.Version;
            stored.Url = _item.Url;
            stored.OwnerName = _item.OwnerName;
            stored.SupplierId = _supplierId == 0 ? null : _supplierId;
            stored.ContractId = _contractId == 0 ? null : _contractId;
            stored.IsActive = _item.IsActive;
            stored.Comment = _item.Comment;

            if (history.HasChanges)
            {
                stored.UpdatedAt = DateTime.UtcNow;
                db.Set<ManagementHistoryEntry>().AddRange(history.Entries);
            }

            await db.SaveChangesAsync();

            ToastService.Notify(new ToastMessage(ToastType.Success, Tr.T("Élément enregistré.")));
            await LoadAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task DeleteAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        Appliance? stored = await db.Set<Appliance>().FirstOrDefaultAsync(item => item.Id == ApplianceId);
        if (stored is null)
        {
            return;
        }

        // L'historique suit la fiche : sans clé étrangère (table polymorphe), c'est ici qu'il part.
        List<ManagementHistoryEntry> history = await db.Set<ManagementHistoryEntry>()
            .Where(entry => entry.ItemType == ItemTypes.Appliance && entry.ItemId == ApplianceId)
            .ToListAsync();

        db.Set<ManagementHistoryEntry>().RemoveRange(history);
        db.Set<Appliance>().Remove(stored);
        await db.SaveChangesAsync();

        Navigation.NavigateTo("/management/appliances");
    }
}
