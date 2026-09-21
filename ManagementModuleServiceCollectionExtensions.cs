using GlpiNg.Modules.Abstractions.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace GlpiNg.Modules.Management;

/// <summary>
/// Point d'enregistrement du module Gestion dans le conteneur DI de l'hôte, même principe que
/// <c>KnowledgeBaseModuleServiceCollectionExtensions.AddKnowledgeBaseModule</c>.
///
/// Ce module n'expose ni contrôleur ni service : ses pages lisent et écrivent par le
/// <c>DbContext</c> de base, et empruntent à l'hôte les documents et les notes par leurs contrats.
/// Ses pages Razor vivent dans une autre assembly que l'hôte et doivent être déclarées au routeur
/// — voir <c>AdditionalAssemblies</c> dans Routes.razor.
/// </summary>
public static class ManagementModuleServiceCollectionExtensions
{
    public static IServiceCollection AddManagementModule(this IServiceCollection services)
    {
        // Contribution du module au menu latéral de l'hôte (groupe « Gestion »).
        services.AddSingleton<IMenuProvider, ManagementMenuProvider>();

        return services;
    }
}
