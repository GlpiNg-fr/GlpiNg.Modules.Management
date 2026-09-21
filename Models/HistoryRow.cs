namespace GlpiNg.Modules.Management.Models;

/// <summary>
/// Une ligne d'historique telle que la fiche l'affiche, projetée depuis
/// <see cref="ManagementHistoryEntry"/> — voir <c>Components/Shared/ItemHistoryTab.razor</c>.
/// </summary>
public sealed record HistoryRow(DateTime OccurredAt, string? User, string Field, string? Description);

