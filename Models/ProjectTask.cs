namespace GlpiNg.Modules.Management.Models;

/// <summary>Statuts d'une tâche de projet, dans l'ordre des colonnes du kanban.</summary>
public static class ProjectTaskStatuses
{
    public static readonly IReadOnlyList<string> All = ["À faire", "En cours", "Terminé"];
}

/// <summary>Tâche d'un <see cref="Project"/> (<c>glpi_projecttasks</c>).</summary>
public class ProjectTask
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public required string Name { get; set; }

    /// <summary>Un des <see cref="ProjectTaskStatuses.All"/>. Texte plutôt qu'énumération, comme le statut du projet.</summary>
    public string Status { get; set; } = "À faire";

    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }

    public int Percent { get; set; }

    public string? Comment { get; set; }
}

/// <summary>Membre de l'équipe d'un projet (<c>glpi_projectteams</c>) ; le nom est conservé à part, le compte pouvant disparaître.</summary>
public class ProjectMember
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? UserId { get; set; }

    public required string UserName { get; set; }
}
