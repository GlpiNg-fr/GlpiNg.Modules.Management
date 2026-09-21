using BlazorBootstrap;
using GlpiNg.Modules.Abstractions.Items;
using GlpiNg.Modules.Management.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GlpiNg.Modules.Management.Components.Pages.Databases;

public partial class Detail : ComponentBase
{
    [Parameter]
    public int DatabaseId { get; set; }

    [Inject]
    private IDbContextFactory<DbContext> DbFactory { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ToastService ToastService { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    private DatabaseInstance? _item;
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
            yield return ("fiche", "Fiche", "ti-database", null);
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

        _item = await db.Set<DatabaseInstance>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == DatabaseId);

        if (_item is null)
        {
            return;
        }

        _history = await db.Set<ManagementHistoryEntry>()
            .AsNoTracking()
            .Where(entry => entry.ItemType == ItemTypes.DatabaseInstance && entry.ItemId == DatabaseId)
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
            _error = "Le nom est obligatoire.";
            return;
        }

        _error = null;
        _isSaving = true;

        try
        {
            await using DbContext db = await DbFactory.CreateDbContextAsync();

            DatabaseInstance? stored = await db.Set<DatabaseInstance>().FirstOrDefaultAsync(item => item.Id == DatabaseId);
            if (stored is null)
            {
                return;
            }

            ManagementHistoryRecorder history = new(ItemTypes.DatabaseInstance, DatabaseId, _currentUserName);

            history.Track("Nom", stored.Name, _item.Name);
            history.Track("Moteur", stored.Engine, _item.Engine);
            history.Track("Version", stored.Version, _item.Version);
            history.Track("Serveur hôte", stored.HostName, _item.HostName);
            history.Track("Port", (int?)stored.Port, (int?)_item.Port);
            history.Track("Chemin des données", stored.Path, _item.Path);
            history.Track("Sauvegardée", stored.IsBackedUp, _item.IsBackedUp);
            history.Track("Actif", stored.IsActive, _item.IsActive);
            history.Track("Commentaires", stored.Comment, _item.Comment);

            stored.Name = _item.Name.Trim();
            stored.Engine = _item.Engine;
            stored.Version = _item.Version;
            stored.HostName = _item.HostName;
            stored.Port = _item.Port;
            stored.Path = _item.Path;
            stored.IsBackedUp = _item.IsBackedUp;
            stored.IsActive = _item.IsActive;
            stored.Comment = _item.Comment;

            if (history.HasChanges)
            {
                stored.UpdatedAt = DateTime.UtcNow;
                db.Set<ManagementHistoryEntry>().AddRange(history.Entries);
            }

            await db.SaveChangesAsync();

            ToastService.Notify(new ToastMessage(ToastType.Success, "Élément enregistré."));
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

        DatabaseInstance? stored = await db.Set<DatabaseInstance>().FirstOrDefaultAsync(item => item.Id == DatabaseId);
        if (stored is null)
        {
            return;
        }

        // L'historique suit la fiche : sans clé étrangère (table polymorphe), c'est ici qu'il part.
        List<ManagementHistoryEntry> history = await db.Set<ManagementHistoryEntry>()
            .Where(entry => entry.ItemType == ItemTypes.DatabaseInstance && entry.ItemId == DatabaseId)
            .ToListAsync();

        db.Set<ManagementHistoryEntry>().RemoveRange(history);
        db.Set<DatabaseInstance>().Remove(stored);
        await db.SaveChangesAsync();

        Navigation.NavigateTo("/management/databases");
    }
}
