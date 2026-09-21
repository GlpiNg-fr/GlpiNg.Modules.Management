using GlpiNg.Modules.Abstractions.Entities;

namespace GlpiNg.Modules.Management.Models;

// Quatre fiches qui décrivent l'infrastructure plutôt qu'un achat : où le matériel est hébergé, ce
// qu'il forme ensemble, et ce qui tourne dessus. Même forme que les précédentes, d'où le fichier
// commun.
//
// Écart assumé et commun aux quatre : GLPI rattache à ces fiches les actifs concernés (les baies
// d'un data center, les nœuds d'un cluster, les serveurs d'un applicatif). Ce rattachement
// demanderait une colonne dans le module Inventory, qui ne connaît pas celui-ci ; il reste à faire
// et le README le dit. Ces fiches décrivent donc l'infrastructure sans encore la relier au parc.

/// <summary>
/// Centre de données (<c>glpi_datacenters</c> côté GLPI, « Gestion &gt; Data centers ») : le site
/// qui héberge les baies.
/// </summary>
public class Datacenter : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Lieu, en clair : le parc a bien une catégorie d'intitulés « Lieux », mais elle vit dans le module Inventory.</summary>
    public string? Location { get; set; }

    public string? Address { get; set; }
    public string? Town { get; set; }
    public string? Country { get; set; }

    public int? SupplierId { get; set; }

    /// <summary>Hébergeur, quand le site n'est pas le sien.</summary>
    public Supplier? Supplier { get; set; }

    public int? ContractId { get; set; }
    public Contract? Contract { get; set; }

    public string? Comment { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Grappe de serveurs (<c>glpi_clusters</c> côté GLPI, « Gestion &gt; Clusters ») : plusieurs
/// machines vues comme une seule ressource.
/// </summary>
public class Cluster : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Nature de la grappe (« Hyper-V », « vSphere », « Kubernetes », « Proxmox »...).</summary>
    public string? Type { get; set; }

    public string? Version { get; set; }

    /// <summary>Nombre de nœuds, tenu à la main tant que le rattachement au parc n'existe pas.</summary>
    public int? NodeCount { get; set; }

    public string? Comment { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Applicatif (<c>glpi_appliances</c> côté GLPI, « Gestion &gt; Applicatifs ») : une application
/// métier, vue comme un tout, par-dessus les serveurs qui la font tourner.
/// </summary>
public class Appliance : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Nature de l'applicatif (« Interne », « SaaS », « Progiciel »...).</summary>
    public string? Type { get; set; }

    /// <summary>Environnement (« Production », « Recette », « Développement »).</summary>
    public string? Environment { get; set; }

    public string? Version { get; set; }

    /// <summary>Adresse d'accès, quand l'applicatif en a une.</summary>
    public string? Url { get; set; }

    /// <summary>Responsable métier ou technique, en clair.</summary>
    public string? OwnerName { get; set; }

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
/// Instance de base de données (<c>glpi_databaseinstances</c> côté GLPI, « Gestion &gt; Bases de
/// données ») : le service, sur le serveur qui l'héberge.
/// </summary>
public class DatabaseInstance : IEntityScoped
{
    /// <inheritdoc />
    public int? EntityId { get; set; }

    /// <inheritdoc />
    public bool IsRecursive { get; set; }

    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Moteur (« SQL Server », « PostgreSQL », « MySQL », « Oracle »...).</summary>
    public string? Engine { get; set; }

    public string? Version { get; set; }

    /// <summary>Serveur hôte, en clair : le rattachement à l'ordinateur du parc reste à faire.</summary>
    public string? HostName { get; set; }

    public int? Port { get; set; }

    /// <summary>Chemin des fichiers de données, quand il vaut la peine d'être noté.</summary>
    public string? Path { get; set; }

    /// <summary>Sauvegardée ou non : la question qu'on se pose toujours trop tard.</summary>
    public bool IsBackedUp { get; set; }

    public string? Comment { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
