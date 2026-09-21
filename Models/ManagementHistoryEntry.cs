using GlpiNg.Modules.Abstractions.Items;

namespace GlpiNg.Modules.Management.Models;

/// <summary>
/// Modification d'une fiche du module Gestion, une ligne par champ touché.
///
/// Une seule table pour les douze types du module, désignés par <see cref="ItemType"/> (voir
/// <see cref="ItemTypes"/>) — comme le fait GLPI avec <c>glpi_logs</c>, et comme le font déjà les
/// notes et les documents de l'hôte. Une table par type aurait multiplié par douze le même schéma
/// pour un contenu identique, et autant de code pour le lire.
///
/// Pas de clé étrangère vers l'objet, conséquence de la même polymorphie : c'est la page qui
/// supprime l'historique avec l'objet, comme le fait l'hôte pour les notes.
/// </summary>
public class ManagementHistoryEntry
{
    public int Id { get; set; }

    /// <summary>Type d'objet au sens GLPI (« Supplier », « Contract », ...).</summary>
    public required string ItemType { get; set; }

    public int ItemId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public string? User { get; set; }

    /// <summary>Libellé du champ modifié, tel qu'il s'affiche sur la fiche.</summary>
    public required string Field { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// Compare un avant/après et n'ajoute une ligne d'historique que si la valeur a bougé : c'est la
/// même mécanique sur les douze fiches, et la laisser à chacune d'elles garantissait qu'une
/// finirait par tracer une modification qui n'en est pas une.
/// </summary>
public sealed class ManagementHistoryRecorder(string itemType, int itemId, string? user)
{
    private readonly List<ManagementHistoryEntry> _entries = [];

    public IReadOnlyList<ManagementHistoryEntry> Entries => _entries;

    public bool HasChanges => _entries.Count > 0;

    public void Track(string field, string? before, string? after)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            return;
        }

        _entries.Add(new ManagementHistoryEntry
        {
            ItemType = itemType,
            ItemId = itemId,
            User = user,
            Field = field,
            Description = $"« {Displayable(before)} » → « {Displayable(after)} »",
        });
    }

    public void Track(string field, bool before, bool after) =>
        Track(field, before ? "Oui" : "Non", after ? "Oui" : "Non");

    public void Track(string field, DateTime? before, DateTime? after) =>
        Track(field, before?.ToString("dd/MM/yyyy"), after?.ToString("dd/MM/yyyy"));

    public void Track(string field, decimal before, decimal after) =>
        Track(field, before.ToString("0.00"), after.ToString("0.00"));

    public void Track(string field, int? before, int? after) =>
        Track(field, before?.ToString(), after?.ToString());

    private static string Displayable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(vide)" : value;
}
