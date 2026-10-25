namespace GlpiNg.Modules.Management.Models;

/// <summary>Enregistrement DNS d'un <see cref="Domain"/> (<c>glpi_domainrecords</c>) : « www », type « A », cible « 192.0.2.10 ».</summary>
public class DomainRecord
{
    public int Id { get; set; }

    public int DomainId { get; set; }
    public Domain? Domain { get; set; }

    /// <summary>Nom de l'enregistrement, relatif au domaine (« www », « @ »).</summary>
    public required string Name { get; set; }

    /// <summary>Type DNS (A, AAAA, CNAME, MX, TXT, NS...). Texte libre : la liste des types ne se ferme jamais vraiment.</summary>
    public string Type { get; set; } = "A";

    /// <summary>Valeur de l'enregistrement (adresse, nom canonique, texte...).</summary>
    public string? Target { get; set; }

    public int? Ttl { get; set; }
}

/// <summary>Salle serveur d'un <see cref="Datacenter"/> (<c>glpi_dcrooms</c>).</summary>
public class ServerRoom
{
    public int Id { get; set; }

    public int DatacenterId { get; set; }
    public Datacenter? Datacenter { get; set; }

    public required string Name { get; set; }

    public string? Comment { get; set; }
}
