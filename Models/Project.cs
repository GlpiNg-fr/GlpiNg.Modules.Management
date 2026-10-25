using GlpiNg.Modules.Abstractions.Entities;

namespace GlpiNg.Modules.Management.Models;

/// <summary>
/// Projet (<c>glpi_projects</c> côté GLPI, « Gestion &gt; Projets ») : un chantier suivi dans le temps,
/// auquel se rattachent des actifs, des tickets, des contrats. Version volontairement légère : pas de
/// tâches ni de diagramme, seulement de quoi nommer un projet, dire où il en est et y rattacher des objets.
/// </summary>
public class Project : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Code court du projet, tel qu'il circule dans les mails et les budgets.</summary>
    public string? Code { get; set; }

    /// <summary>Avancement (« À démarrer », « En cours », « Terminé »...) — champ texte, comme <see cref="Supplier.Type"/>.</summary>
    public string? Status { get; set; }

    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }

    /// <summary>Avancement en pourcentage, 0 à 100.</summary>
    public int Percent { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
