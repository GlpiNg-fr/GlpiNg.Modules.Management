using System.Globalization;
using BlazorBootstrap;
using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GlpiNg.Modules.Management.Components.Pages.Contracts;

public partial class Detail : ComponentBase
{
    /// <summary>Même fenêtre que le filtre de la liste : un trimestre.</summary>
    private const int UrgentNoticeDays = 90;

    [Parameter]
    public int ContractId { get; set; }

    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ToastService ToastService { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    private Contract? _contract;
    private List<Supplier> _suppliers = [];
    private List<Supplier> _supplierCandidates = [];
    private List<ContractCost> _costs = [];
    private List<Budget> _budgets = [];
    private List<HistoryRow> _history = [];

    private string _activeTab = "fiche";
    private int _supplierToAdd;
    private ContractCost _newCost = NewBlankCost();
    private int _newCostBudgetId;
    private int _documentCount;
    private int _noteCount;
    private bool _isSaving;
    private string? _error;
    private string _currentUserName = "?";

    private bool NoticeIsUrgent =>
        _contract?.NoticeDeadline is { } deadline && deadline <= DateTime.Today.AddDays(UrgentNoticeDays);

    private IEnumerable<(string Key, string Label, string Icon, int? Count)> Tabs
    {
        get
        {
            yield return ("fiche", "Fiche", "ti-file-text", null);
            yield return ("fournisseurs", "Fournisseurs", "ti-truck", _suppliers.Count);
            yield return ("couts", "Coûts", "ti-coin", _costs.Count);
            yield return ("documents", "Documents", "ti-file", _documentCount);
            yield return ("notes", "Notes", "ti-notes", _noteCount);
            yield return ("historique", "Historique", "ti-history", _history.Count);
        }
    }

    private static string PeriodicityLabel(ContractPeriodicity value) => value switch
    {
        ContractPeriodicity.None => "—",
        ContractPeriodicity.Monthly => "Mensuelle",
        ContractPeriodicity.Quarterly => "Trimestrielle",
        ContractPeriodicity.Biannual => "Semestrielle",
        ContractPeriodicity.Annual => "Annuelle",
        ContractPeriodicity.Biennial => "Tous les 2 ans",
        ContractPeriodicity.Triennial => "Tous les 3 ans",
        _ => value.ToString(),
    };

    private static string RenewalLabel(ContractRenewal value) => value switch
    {
        ContractRenewal.Never => "Aucune",
        ContractRenewal.Tacit => "Tacite",
        ContractRenewal.Express => "Expresse",
        _ => value.ToString(),
    };

    private static ContractPeriodicity ParsePeriodicity(object? value) =>
        Enum.TryParse(value?.ToString(), out ContractPeriodicity parsed) ? parsed : ContractPeriodicity.None;

    private static ContractRenewal ParseRenewal(object? value) =>
        Enum.TryParse(value?.ToString(), out ContractRenewal parsed) ? parsed : ContractRenewal.Never;

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

        _contract = await db.Set<Contract>()
            .AsNoTracking()
            .FirstOrDefaultAsync(contract => contract.Id == ContractId);

        if (_contract is null)
        {
            return;
        }

        _suppliers = await db.Set<ContractSupplier>()
            .AsNoTracking()
            .Where(link => link.ContractId == ContractId)
            .Select(link => link.Supplier!)
            .OrderBy(supplier => supplier.Name)
            .ToListAsync();

        List<int> attached = [.. _suppliers.Select(supplier => supplier.Id)];

        _supplierCandidates = await db.Set<Supplier>()
            .AsNoTracking()
            .Where(supplier => !attached.Contains(supplier.Id))
            .OrderBy(supplier => supplier.Name)
            .ToListAsync();

        _costs = await db.Set<ContractCost>()
            .AsNoTracking()
            .Include(cost => cost.Budget)
            .Where(cost => cost.ContractId == ContractId)
            .OrderByDescending(cost => cost.StartDate)
            .ToListAsync();

        _budgets = await db.Set<Budget>()
            .AsNoTracking()
            .Where(budget => budget.IsActive)
            .OrderBy(budget => budget.Name)
            .ToListAsync();

        _history = await db.Set<ManagementHistoryEntry>()
            .AsNoTracking()
            .Where(entry => entry.ItemType == ItemTypes.Contract && entry.ItemId == ContractId)
            .OrderByDescending(entry => entry.OccurredAt)
            .Select(entry => new HistoryRow(entry.OccurredAt, entry.User, entry.Field, entry.Description))
            .ToListAsync();
    }

    private async Task SaveAsync()
    {
        if (_contract is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_contract.Name))
        {
            _error = "Le nom est obligatoire.";
            return;
        }

        _error = null;
        _isSaving = true;

        try
        {
            await using DbContext db = await DbFactory.CreateDbContextAsync();

            Contract? stored = await db.Set<Contract>().FirstOrDefaultAsync(contract => contract.Id == ContractId);
            if (stored is null)
            {
                return;
            }

            ManagementHistoryRecorder history = new(ItemTypes.Contract, ContractId, _currentUserName);

            void Track(string field, string? before, string? after) => history.Track(field, before, after);

            Track("Nom", stored.Name, _contract.Name);
            Track("Numéro", stored.Number, _contract.Number);
            Track("Type", stored.Type, _contract.Type);
            Track("Numéro de compte", stored.AccountNumber, _contract.AccountNumber);
            Track("Date de début", stored.StartDate?.ToString("dd/MM/yyyy"), _contract.StartDate?.ToString("dd/MM/yyyy"));
            Track("Durée (mois)", stored.DurationMonths?.ToString(CultureInfo.InvariantCulture), _contract.DurationMonths?.ToString(CultureInfo.InvariantCulture));
            Track("Préavis (mois)", stored.NoticeMonths?.ToString(CultureInfo.InvariantCulture), _contract.NoticeMonths?.ToString(CultureInfo.InvariantCulture));
            Track("Périodicité", PeriodicityLabel(stored.Periodicity), PeriodicityLabel(_contract.Periodicity));
            Track("Périodicité de facturation", PeriodicityLabel(stored.BillingPeriodicity), PeriodicityLabel(_contract.BillingPeriodicity));
            Track("Reconduction", RenewalLabel(stored.Renewal), RenewalLabel(_contract.Renewal));
            Track("Commentaires", stored.Comment, _contract.Comment);
            Track("Actif", stored.IsActive ? "Oui" : "Non", _contract.IsActive ? "Oui" : "Non");

            stored.Name = _contract.Name.Trim();
            stored.Number = _contract.Number;
            stored.Type = _contract.Type;
            stored.AccountNumber = _contract.AccountNumber;
            stored.StartDate = _contract.StartDate;
            stored.DurationMonths = _contract.DurationMonths;
            stored.NoticeMonths = _contract.NoticeMonths;
            stored.Periodicity = _contract.Periodicity;
            stored.BillingPeriodicity = _contract.BillingPeriodicity;
            stored.Renewal = _contract.Renewal;
            stored.Comment = _contract.Comment;
            stored.IsActive = _contract.IsActive;

            if (history.HasChanges)
            {
                stored.UpdatedAt = DateTime.UtcNow;
                db.Set<ManagementHistoryEntry>().AddRange(history.Entries);
            }

            await db.SaveChangesAsync();

            ToastService.Notify(new ToastMessage(ToastType.Success, "Contrat enregistré."));
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

        db.Set<ContractSupplier>().Add(new ContractSupplier
        {
            ContractId = ContractId,
            SupplierId = _supplierToAdd,
        });

        await db.SaveChangesAsync();

        _supplierToAdd = 0;
        await LoadAsync();
    }

    private async Task DetachSupplierAsync(int supplierId)
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        ContractSupplier? link = await db.Set<ContractSupplier>()
            .FirstOrDefaultAsync(entry => entry.ContractId == ContractId && entry.SupplierId == supplierId);

        if (link is null)
        {
            return;
        }

        db.Set<ContractSupplier>().Remove(link);
        await db.SaveChangesAsync();

        await LoadAsync();
    }

    private async Task AddCostAsync()
    {
        if (string.IsNullOrWhiteSpace(_newCost.Name))
        {
            return;
        }

        await using DbContext db = await DbFactory.CreateDbContextAsync();

        db.Set<ContractCost>().Add(new ContractCost
        {
            ContractId = ContractId,
            Name = _newCost.Name.Trim(),
            StartDate = _newCost.StartDate,
            EndDate = _newCost.EndDate,
            Amount = _newCost.Amount,
            BudgetId = _newCostBudgetId == 0 ? null : _newCostBudgetId,
        });

        await db.SaveChangesAsync();

        _newCost = NewBlankCost();
        _newCostBudgetId = 0;

        await LoadAsync();
    }

    private async Task DeleteCostAsync(int costId)
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        ContractCost? cost = await db.Set<ContractCost>().FirstOrDefaultAsync(entry => entry.Id == costId);
        if (cost is null)
        {
            return;
        }

        db.Set<ContractCost>().Remove(cost);
        await db.SaveChangesAsync();

        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        await using DbContext db = await DbFactory.CreateDbContextAsync();

        Contract? stored = await db.Set<Contract>().FirstOrDefaultAsync(contract => contract.Id == ContractId);
        if (stored is null)
        {
            return;
        }

        // L'historique suit l'objet : sans clé étrangère (table polymorphe), c'est ici qu'il part.
        List<ManagementHistoryEntry> history = await db.Set<ManagementHistoryEntry>()
            .Where(entry => entry.ItemType == ItemTypes.Contract && entry.ItemId == ContractId)
            .ToListAsync();

        db.Set<ManagementHistoryEntry>().RemoveRange(history);
        db.Set<Contract>().Remove(stored);
        await db.SaveChangesAsync();

        Navigation.NavigateTo("/management/contracts");
    }

    private static ContractCost NewBlankCost() => new() { Name = string.Empty };
}
