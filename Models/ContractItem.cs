namespace GlpiNg.Modules.Management.Models;

/// <summary>
/// Rattachement d'un <see cref="Contract"/> à un objet quelconque (<c>glpi_contracts_items</c>).
/// <see cref="ItemType"/> et <see cref="ItemId"/> suivent la convention des documents et des notes
/// (noms de GLPI, voir <c>ItemTypes</c>) : pas de clé étrangère vers l'objet, qui vit dans un autre module.
/// </summary>
public class ContractItem
{
    public int Id { get; set; }

    public int ContractId { get; set; }
    public Contract? Contract { get; set; }

    public required string ItemType { get; set; }

    public int ItemId { get; set; }
}
