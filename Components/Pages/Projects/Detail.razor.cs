using BlazorBootstrap;
using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using GlpiNg.Modules.Abstractions.Localization;

namespace GlpiNg.Modules.Management.Components.Pages.Projects;

public partial class Detail : ComponentBase
{
    [Parameter]
    public int ProjectId { get; set; }

    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ToastService ToastService { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    private Project? _item;
    private List<HistoryRow> _history = [];

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
            yield return ("fiche", "Fiche", "ti-clipboard-list", null);
            yield return ("taches", "Tâches de projet", "ti-checklist", null);
            yield return ("equipe", "Équipe", "ti-users", null);
            yield return ("kanban", "Kanban", "ti-layout-kanban", null);
            yield return ("couts", "Coûts", "ti-coin", null);
            yield return ("elements", "Éléments", "ti-device-desktop", null);
            yield return ("contrats", "Contrats", "ti-file-text", null);
            yield return ("baseconnaissances", "Base de connaissances", "ti-book", null);
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

        _item = await db.Set<Project>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == ProjectId);

        if (_item is null)
        {
            return;
        }

        _history = await db.Set<ManagementHistoryEntry>()
            .AsNoTracking()
            .Where(entry => entry.ItemType == ItemTypes.Project && entry.ItemId == ProjectId)
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

            Project? stored = await db.Set<Project>().FirstOrDefaultAsync(item => item.Id == ProjectId);
            if (stored is null)
            {
                return;
            }

            ManagementHistoryRecorder history = new(ItemTypes.Project, ProjectId, _currentUserName);

            history.Track("Projet", stored.Name, _item.Name);
            history.Track("Code", stored.Code, _item.Code);
            history.Track("Statut", stored.Status, _item.Status);
            history.Track("Avancement", (int?)stored.Percent, (int?)_item.Percent);
            history.Track("Début prévu", stored.PlannedStart, _item.PlannedStart);
            history.Track("Fin prévue", stored.PlannedEnd, _item.PlannedEnd);
            history.Track("Commentaires", stored.Comment, _item.Comment);

            stored.Name = _item.Name.Trim();
            stored.Code = _item.Code;
            stored.Status = _item.Status;
            stored.Percent = Math.Clamp(_item.Percent, 0, 100);
            stored.PlannedStart = _item.PlannedStart;
            stored.PlannedEnd = _item.PlannedEnd;
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

        Project? stored = await db.Set<Project>().FirstOrDefaultAsync(item => item.Id == ProjectId);
        if (stored is null)
        {
            return;
        }

        // L'historique suit la fiche : sans clé étrangère (table polymorphe), c'est ici qu'il part.
        List<ManagementHistoryEntry> history = await db.Set<ManagementHistoryEntry>()
            .Where(entry => entry.ItemType == ItemTypes.Project && entry.ItemId == ProjectId)
            .ToListAsync();

        db.Set<ManagementHistoryEntry>().RemoveRange(history);
        db.Set<Project>().Remove(stored);
        await db.SaveChangesAsync();

        Navigation.NavigateTo("/management/projects");
    }
}
