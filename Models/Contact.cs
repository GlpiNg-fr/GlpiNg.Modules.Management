using GlpiNg.Modules.Abstractions.Entities;

namespace GlpiNg.Modules.Management.Models;

/// <summary>
/// Interlocuteur chez un tiers (<c>glpi_contacts</c> côté GLPI, « Gestion &gt; Contacts ») :
/// commercial, technicien de maintenance, gestionnaire de compte.
///
/// Distinct d'un <c>GlpiUser</c>, qui est un compte de l'application : un contact ne se connecte
/// pas, il se téléphone.
/// </summary>
public class Contact : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }

    /// <summary>Nom de famille, ou raison sociale si le contact est un service plutôt qu'une personne.</summary>
    public required string Name { get; set; }

    public string? FirstName { get; set; }

    /// <summary>Fonction ou nature du contact (« Commercial », « Support technique »...) — champ texte, voir <see cref="Supplier.Type"/>.</summary>
    public string? Type { get; set; }

    public string? Title { get; set; }

    public string? Phone { get; set; }
    public string? Phone2 { get; set; }
    public string? Mobile { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }

    public string? Address { get; set; }
    public string? PostCode { get; set; }
    public string? Town { get; set; }
    public string? Country { get; set; }

    public string? Comment { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<SupplierContact> Suppliers { get; set; } = [];

    /// <summary>« Prénom Nom », ou le seul nom quand il n'y a pas de prénom — l'affichage courant d'un contact.</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(FirstName) ? Name : $"{FirstName} {Name}";
}

