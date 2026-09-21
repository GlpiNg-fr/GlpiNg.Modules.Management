using GlpiNg.Modules.Abstractions.Entities;

namespace GlpiNg.Modules.Management.Models;

/// <summary>Périodicité d'un contrat ou de sa facturation, en mois — reprise des choix de GLPI.</summary>
public enum ContractPeriodicity
{
    None = 0,
    Monthly = 1,
    Quarterly = 3,
    Biannual = 6,
    Annual = 12,
    Biennial = 24,
    Triennial = 36,
}

/// <summary>Conduite à tenir à l'échéance (<c>renewal</c> côté GLPI).</summary>
public enum ContractRenewal
{
    Never,
    Tacit,
    Express,
}

/// <summary>
/// Engagement contractuel auprès d'un ou plusieurs tiers (<c>glpi_contracts</c> côté GLPI,
/// « Gestion &gt; Contrats ») : maintenance, location, abonnement, garantie étendue.
///
/// La date de fin n'est pas stockée mais calculée à partir de la date de début et de la durée
/// (<see cref="EndDate"/>) : deux colonnes qui disent la même chose finissent par se contredire
/// dès qu'on corrige l'une sans l'autre.
/// </summary>
public class Contract : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Numéro du contrat chez le tiers, tel qu'il figure sur les factures.</summary>
    public string? Number { get; set; }

    /// <summary>Nature du contrat (« Maintenance », « Location »...) — champ texte, voir <see cref="Supplier.Type"/>.</summary>
    public string? Type { get; set; }

    public DateTime? StartDate { get; set; }

    /// <summary>Durée en mois. Zéro (ou absente) signifie « sans terme », d'où une <see cref="EndDate"/> nulle.</summary>
    public int? DurationMonths { get; set; }

    /// <summary>Préavis de dénonciation, en mois : c'est lui qui donne la date limite de résiliation.</summary>
    public int? NoticeMonths { get; set; }

    public ContractPeriodicity Periodicity { get; set; } = ContractPeriodicity.None;

    public ContractPeriodicity BillingPeriodicity { get; set; } = ContractPeriodicity.None;

    public ContractRenewal Renewal { get; set; } = ContractRenewal.Never;

    public string? AccountNumber { get; set; }

    public string? Comment { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ContractSupplier> Suppliers { get; set; } = [];
    public List<ContractCost> Costs { get; set; } = [];

    /// <summary>Échéance calculée, ou <c>null</c> pour un contrat sans date de début ou sans durée.</summary>
    public DateTime? EndDate =>
        StartDate is { } start && DurationMonths is > 0 ? start.AddMonths(DurationMonths.Value) : null;

    /// <summary>
    /// Date limite pour dénoncer le contrat : l'échéance moins le préavis. C'est la date qui
    /// compte pour ne pas se retrouver reconduit sans l'avoir voulu, et elle ne se lit nulle part
    /// ailleurs sur la fiche.
    /// </summary>
    public DateTime? NoticeDeadline =>
        EndDate is { } end && NoticeMonths is > 0 ? end.AddMonths(-NoticeMonths.Value) : null;
}

/// <summary>
/// Rattachement d'un <see cref="Supplier"/> à un <see cref="Contract"/>
/// (<c>glpi_contracts_suppliers</c>) : un contrat peut engager plusieurs tiers (un éditeur et son
/// intégrateur, par exemple).
/// </summary>
public class ContractSupplier
{
    public int Id { get; set; }

    public int ContractId { get; set; }
    public Contract? Contract { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
}

/// <summary>
/// Coût imputé à un contrat (<c>glpi_contractcosts</c>), éventuellement sur un
/// <see cref="Budget"/>. C'est par ces lignes qu'un budget se remplit : sans elles, un budget ne
/// serait qu'un montant sans emploi.
/// </summary>
public class ContractCost
{
    public int Id { get; set; }

    public int ContractId { get; set; }
    public Contract? Contract { get; set; }

    public required string Name { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Montant imputé. <c>decimal</c> et non <c>double</c> : c'est de la monnaie, pas une mesure.</summary>
    public decimal Amount { get; set; }

    public int? BudgetId { get; set; }
    public Budget? Budget { get; set; }
}

