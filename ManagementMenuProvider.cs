using GlpiNg.Modules.Abstractions.Menu;

namespace GlpiNg.Modules.Management;

/// <summary>
/// Contribue les entrées du groupe « Gestion » que ce module met en service, là où l'hôte les
/// affichait sans lien faute de page derrière — même montage qu'<c>InventoryMenuProvider</c>.
///
/// Le module couvre désormais tout le groupe, à l'exception des Documents, que l'hôte porte
/// lui-même (ils se rattachent à n'importe quel type d'objet, y compris hors de ce module).
/// </summary>
public sealed class ManagementMenuProvider : IMenuProvider
{
    public IReadOnlyList<MenuGroup> GetMenuGroups() =>
    [
        new("gestion", "ti-wallet", "Gestion",
        [
            new("Budgets", "/management/budgets", "ti-report-money"),
            new("Fournisseurs", "/management/suppliers", "ti-truck"),
            new("Contacts", "/management/contacts", "ti-address-book"),
            new("Contrats", "/management/contracts", "ti-file-text"),
            new("Licences", "/management/licenses", "ti-license"),
            new("Lignes téléphoniques", "/management/lines", "ti-phone-call"),
            new("Certificats", "/management/certificates", "ti-certificate"),
            new("Data centers", "/management/datacenters", "ti-building-warehouse"),
            new("Clusters", "/management/clusters", "ti-affiliate"),
            new("Domaines", "/management/domains", "ti-world-www"),
            new("Applicatifs", "/management/appliances", "ti-apps"),
            new("Bases de données", "/management/databases", "ti-database"),
        ]),
    ];
}
