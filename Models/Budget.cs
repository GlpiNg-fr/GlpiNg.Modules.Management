using GlpiNg.Modules.Abstractions.Entities;

namespace GlpiNg.Modules.Management.Models;

/// <summary>
/// Enveloppe budgétaire (<c>glpi_budgets</c> côté GLPI, « Gestion &gt; Budgets ») : un montant
/// alloué sur une période, sur lequel des dépenses sont imputées.
///
/// Ce que GlpiNg impute dessus aujourd'hui, ce sont les coûts de contrat
/// (<see cref="ContractCost"/>). GLPI y ajoute les informations financières de chaque actif
/// (<c>glpi_infocoms</c> : prix d'achat, amortissement, garantie), que GlpiNg ne modélise pas
/// encore — un budget ne voit donc ici que ses contrats, et le README le dit.
/// </summary>
public class Budget : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Référence comptable de l'enveloppe, telle que la connaît la comptabilité.</summary>
    public string? Code { get; set; }

    /// <summary>Nature du budget (« Investissement », « Fonctionnement »...) — champ texte, voir <see cref="Supplier.Type"/>.</summary>
    public string? Type { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Montant alloué. <c>decimal</c> et non <c>double</c> : c'est de la monnaie, pas une mesure.</summary>
    public decimal Amount { get; set; }

    public string? Comment { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ContractCost> Costs { get; set; } = [];
}

