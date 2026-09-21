using GlpiNg.Modules.Abstractions.Entities;

namespace GlpiNg.Modules.Management.Models;

/// <summary>
/// Tiers auprès duquel le parc est acheté, loué ou maintenu (<c>glpi_suppliers</c> côté GLPI,
/// « Gestion &gt; Fournisseurs »).
///
/// Écart assumé avec GLPI : la catégorie (<c>SupplierType</c>) est un champ texte et non un
/// intitulé. C'est déjà le cas du « Type » des actifs de ce parc (voir <c>Printer.Type</c>), et
/// ouvrir une catégorie d'intitulés par type de tiers coûterait plus d'écrans de configuration
/// qu'elle ne rendrait de service tant que rien ne s'appuie dessus.
/// </summary>
public class Supplier : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Type { get; set; }

    /// <summary>Numéro d'immatriculation (SIRET, numéro de TVA...), tel que le tiers le communique.</summary>
    public string? RegistrationNumber { get; set; }

    public string? Address { get; set; }
    public string? PostCode { get; set; }
    public string? Town { get; set; }
    public string? Country { get; set; }

    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public string? Comment { get; set; }

    /// <summary>Un tiers inactif reste rattaché à ses contrats passés mais n'est plus proposé à la saisie.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<SupplierContact> Contacts { get; set; } = [];
    public List<ContractSupplier> Contracts { get; set; } = [];
}

/// <summary>
/// Rattachement d'un <see cref="Contact"/> à un <see cref="Supplier"/>
/// (<c>glpi_contacts_suppliers</c>). Une personne peut représenter plusieurs tiers, et un tiers
/// compter plusieurs interlocuteurs : d'où une table de liaison plutôt qu'une colonne de part et
/// d'autre.
/// </summary>
public class SupplierContact
{
    public int Id { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int ContactId { get; set; }
    public Contact? Contact { get; set; }
}

