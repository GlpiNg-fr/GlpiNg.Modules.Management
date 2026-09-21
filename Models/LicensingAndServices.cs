using GlpiNg.Modules.Abstractions.Entities;

namespace GlpiNg.Modules.Management.Models;

// Quatre fiches de même famille : ce qui s'achète auprès d'un tiers, court sur une période et
// finit par expirer. Elles partagent la même forme — un nom, un type libre, un tiers et un contrat
// facultatifs, une échéance — d'où ce fichier commun plutôt que quatre presque identiques.
//
// L'échéance est le champ qui compte : une licence, un certificat ou un domaine qu'on laisse
// expirer se remarque en production, jamais dans un inventaire. Les listes la mettent donc en
// avant et savent filtrer ce qui expire bientôt.

/// <summary>
/// Licence logicielle (<c>glpi_softwarelicenses</c> côté GLPI, « Gestion &gt; Licences »).
///
/// Écart assumé : GlpiNg n'a pas d'entité Logiciel — les logiciels sont inventoriés poste par poste
/// (<c>ComputerSoftware</c>). Le logiciel couvert est donc nommé ici en clair, et le rapprochement
/// avec l'inventaire reste à faire par l'œil.
/// </summary>
public class SoftwareLicense : IEntityScoped, IExpiringItem
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Logiciel couvert, en clair.</summary>
    public string? SoftwareName { get; set; }

    /// <summary>Nature de la licence (« OEM », « Abonnement », « Perpétuelle »...).</summary>
    public string? Type { get; set; }

    public string? SerialNumber { get; set; }

    /// <summary>Clé ou numéro d'activation, tel que le tiers l'a fourni.</summary>
    public string? LicenseKey { get; set; }

    /// <summary>Nombre de postes couverts. Zéro signifie « illimité », comme dans GLPI.</summary>
    public int Seats { get; set; }

    /// <summary>Postes effectivement déployés, tenus à la main faute de lien avec l'inventaire.</summary>
    public int UsedSeats { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int? ContractId { get; set; }
    public Contract? Contract { get; set; }

    public string? Comment { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Postes restants, négatif en cas de sur-déploiement — ce qu'on veut justement voir.</summary>
    public int RemainingSeats => Seats == 0 ? int.MaxValue : Seats - UsedSeats;

    /// <summary>Vrai quand plus de postes sont déployés que couverts : c'est le risque juridique.</summary>
    public bool IsOverDeployed => Seats > 0 && UsedSeats > Seats;
}

/// <summary>
/// Ligne téléphonique (<c>glpi_lines</c> côté GLPI, « Gestion &gt; Lignes téléphoniques ») :
/// l'abonnement, non l'appareil. La carte SIM et le téléphone qui la portent sont des actifs du
/// parc ; le lien entre les deux demanderait une colonne côté Inventory et reste à faire.
/// </summary>
public class PhoneLine : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Numéro d'appel.</summary>
    public string? Number { get; set; }

    /// <summary>Nature de la ligne (« Mobile », « Fixe », « Données »...).</summary>
    public string? Type { get; set; }

    /// <summary>État de l'abonnement (« En service », « Suspendue », « Résiliée »...).</summary>
    public string? Status { get; set; }

    /// <summary>Personne à qui la ligne est attribuée, en clair.</summary>
    public string? AssignedUser { get; set; }

    public int? SupplierId { get; set; }

    /// <summary>Opérateur. C'est un <see cref="Models.Supplier"/> comme un autre, d'où le même rattachement.</summary>
    public Supplier? Supplier { get; set; }

    public int? ContractId { get; set; }
    public Contract? Contract { get; set; }

    public string? Comment { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Certificat (<c>glpi_certificates</c> côté GLPI, « Gestion &gt; Certificats ») : TLS, signature
/// de code, autorité interne.
/// </summary>
public class Certificate : IEntityScoped, IExpiringItem
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Nature du certificat (« TLS serveur », « Signature de code »...).</summary>
    public string? Type { get; set; }

    /// <summary>Nom DNS couvert, ou le sujet du certificat.</summary>
    public string? DnsName { get; set; }

    public string? SerialNumber { get; set; }

    /// <summary>Autorité émettrice.</summary>
    public string? Issuer { get; set; }

    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int? ContractId { get; set; }
    public Contract? Contract { get; set; }

    public string? Comment { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Nom de domaine (<c>glpi_domains</c> côté GLPI, « Gestion &gt; Domaines »).
/// </summary>
public class Domain : IEntityScoped, IExpiringItem
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }

    /// <summary>Le domaine lui-même (« exemple.fr »).</summary>
    public required string Name { get; set; }

    /// <summary>Nature du domaine (« Principal », « Redirection », « Interne »...).</summary>
    public string? Type { get; set; }

    public DateTime? CreationDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public int? SupplierId { get; set; }

    /// <summary>Bureau d'enregistrement, qui est un tiers comme un autre.</summary>
    public Supplier? Supplier { get; set; }

    public int? ContractId { get; set; }
    public Contract? Contract { get; set; }

    public string? Comment { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Ce qui porte une échéance et mérite d'être signalé avant qu'elle ne tombe — voir
/// <c>ExpirationStatus</c>, qui en tire l'affichage commun aux listes et aux fiches.
/// </summary>
public interface IExpiringItem
{
    DateTime? ExpirationDate { get; }
}

/// <summary>
/// Lecture d'une échéance : expirée, proche, ou lointaine. Une seule règle pour les licences, les
/// certificats et les domaines, sans quoi « bientôt » ne voudrait pas dire la même chose d'une
/// liste à l'autre.
/// </summary>
public static class ExpirationStatus
{
    /// <summary>Fenêtre d'alerte : un mois, le délai usuel pour renouveler sans précipitation.</summary>
    public const int SoonDays = 30;

    public static bool IsExpired(DateTime? expiration) =>
        expiration is { } date && date.Date < DateTime.Today;

    public static bool IsExpiringSoon(DateTime? expiration) =>
        expiration is { } date && date.Date >= DateTime.Today && date.Date <= DateTime.Today.AddDays(SoonDays);

    /// <summary>Classe CSS de l'échéance affichée : rouge si passée, orange si proche.</summary>
    public static string CssClass(DateTime? expiration) =>
        IsExpired(expiration) ? "text-danger fw-semibold"
        : IsExpiringSoon(expiration) ? "text-warning fw-semibold"
        : "text-secondary";

    public static string Label(DateTime? expiration) =>
        expiration is { } date ? date.ToString("dd/MM/yyyy") : "—";
}
