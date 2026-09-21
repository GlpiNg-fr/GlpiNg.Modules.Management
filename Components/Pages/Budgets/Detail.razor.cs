using System.Globalization;
using BlazorBootstrap;
using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GlpiNg.Modules.Management.Components.Pages.Budgets;

public partial class Detail : ComponentBase
{
    [Parameter]
    public int BudgetId { get; set; }

    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ToastService ToastService { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    private Budget? _budget;
    private List<ContractCost> _costs = [];
    private List<HistoryRow> _history = [];

    private string _activeTab = "fiche";
    private int _documentCount;
    private int _noteCount;
    private bool _isSaving;
    private string? _error;
    private string _currentUserName = "?";

    private decimal Spent => _costs.Sum(cost => cost.Amount);

    private decimal Remaining => (_budget?.Amount ?? 0m) - Spent;

    private IEnumerable<(string Key, string Label, string Icon, int? Count)> Tabs
    {
        get
        {
            yield return ("fiche", "Fiche", "ti-report-money", null);
            yield return ("couts", "Coûts imputés", "ti-coin", _costs.Count);
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

        _budget = await db.Set<Budget>()
            .AsNoTracking()
            .FirstOrDefaultAsync(budget => budget.Id == BudgetId);

        if (_budget is null)
        {
            return;
        }

        _costs = await db.Set<ContractCost>()
            .AsNoTracking()
            .Include(cost => cost.Contract)
            .Where(cost => cost.BudgetId == BudgetId)
            .OrderByDescending(cost => cost.StartDate)
            .ToListAsync();

        _history = await db.Set<ManagementHistoryEntry>()
            .AsNoTracking()
            .Where(entry => entry.ItemType == ItemTypes.Budget && entry.ItemId == BudgetId)
            .OrderByDescending(entry => entry.OccurredAt)
            .Select(entry => new HistoryRow(entry.OccurredAt, entry.User, entry.Field, entry.Description))
            .ToListAsync();
    }

    private async Task SaveAsync()
    {
        if (_budget is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_budget.Name))
        {
            _error = "Le nom est obligatoire.";
            return;
        }

        if (_budget.StartDate is { } start && _budget.EndDate is { } end && end < start)
        {
            _error = "La date de fin précède la date de début.";
            return;
        }

        _error = null;
        _isSaving = true;

        try
        {
            await using DbContext db = await DbFactory.CreateDbContextAsync();

            Budget? stored = await db.Set<Budget>().FirstOrDefaultAsync(budget => budget.Id == BudgetId);
            if (stored is null)
            {
                return;
            }

            ManagementHistoryRecorder history = new(ItemTypes.Budget, BudgetId, _currentUserName);

            void Track(string field, string? before, string? after) => history.Track(field, before, after);

            Track("Nom", stored.Name, _budget.Name);
            Track("Code", stored.Code, _budget.Code);
            Track("Type", stored.Type, _budget.Type);
            Track("Montant alloué", stored.Amount.ToString(CultureInfo.CurrentCulture), _budget.Amount.ToString(CultureInfo.CurrentCulture));
            Track("Début", stored.StartDate?.ToString("dd/MM/yyyy"), _budget.StartDate?.ToString("dd/MM/yyyy"));
            Track("Fin", stored.EndDate?.ToString("dd/MM/yyyy"), _budget.EndDate?.ToString("dd/MM/yyyy"));
            Track("Commentaires", stored.Comment, _budget.Comment);
            Track("Actif", stored.IsActive ? "Oui" : "Non", _budget.IsActive ? "Oui" : "Non");

            stored.Name = _budget.Name.Trim();
            stored.Code = _budget.Code;
            stored.Type = _budget.Type;
            stored.Amount = _budget.Amount;
            stored.StartDate = _budget.StartDate;
            stored.EndDate = _budget.EndDate;
            stored.Comment = _budget.Comment;
            stored.IsActive = _budget.IsActive;

            if (history.HasChanges)
            {
                stored.UpdatedAt = DateTime.UtcNow;
                db.Set<ManagementHistoryEntry>().AddRange(history.Entries);
            }

            await db.SaveChangesAsync();

            ToastService.Notify(new ToastMessage(ToastType.Success, "Budget enregistré."));
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

        Budget? stored = await db.Set<Budget>().FirstOrDefaultAsync(budget => budget.Id == BudgetId);
        if (stored is null)
        {
            return;
        }

        // L'historique suit l'objet : sans clé étrangère (table polymorphe), c'est ici qu'il part.
        List<ManagementHistoryEntry> history = await db.Set<ManagementHistoryEntry>()
            .Where(entry => entry.ItemType == ItemTypes.Budget && entry.ItemId == BudgetId)
            .ToListAsync();

        db.Set<ManagementHistoryEntry>().RemoveRange(history);
        db.Set<Budget>().Remove(stored);
        await db.SaveChangesAsync();

        Navigation.NavigateTo("/management/budgets");
    }
}
