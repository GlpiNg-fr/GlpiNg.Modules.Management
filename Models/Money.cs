using System.Globalization;

namespace GlpiNg.Modules.Management.Models;

/// <summary>
/// Affichage des montants du module.
///
/// La culture est fixée au français plutôt que laissée à celle du serveur : l'interface de GlpiNg
/// est en français, et un serveur installé en anglais affichait sinon des livres sterling sur des
/// budgets tenus en euros — constaté à l'écran, « £10,000.00 » pour dix mille euros.
///
/// Un vrai réglage de devise a sa place dans la configuration, le jour où GlpiNg sera traduit ;
/// d'ici là, mieux vaut une devise fausse pour personne qu'une devise fausse pour tout le monde
/// sauf l'installation qui a servi à développer.
/// </summary>
public static class Money
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("fr-FR");

    public static string Format(decimal amount) => amount.ToString("C", Culture);
}
