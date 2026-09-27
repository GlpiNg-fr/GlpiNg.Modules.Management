# GlpiNg.Modules.Management

*[English version](README.en.md)*

Module Gestion de GlpiNg : tiers, contrats, finances et infrastructure.

> **Avertissement** — GlpiNg est un projet indépendant. Il n'est ni affilié à, ni approuvé,
> soutenu ou sponsorisé par Teclib' ou le projet GLPI. « GLPI » et « GLPI-Agent » sont des
> marques de leurs propriétaires respectifs ; elles ne sont citées ici que pour décrire la
> compatibilité de GlpiNg avec le protocole GLPI-Agent et l'import depuis une base GLPI.

## Contenu

- Fournisseurs et contacts
- Contrats et leurs coûts, budgets
- Licences logicielles, lignes téléphoniques, certificats, domaines
- Datacenters, clusters, appliances, instances de bases de données
- Historique des modifications

## Utilisation

Ce dépôt est un sous-module de [GlpiNg](https://github.com/GlpiNg-fr/GlpiNg), sous
`src/GlpiNg.Modules.Management`. Il ne se compile pas seul : il référence `GlpiNg.Modules.Abstractions` par chemin relatif.

```bash
git clone --recurse-submodules https://github.com/GlpiNg-fr/GlpiNg.git
```

L'hôte l'enregistre par `services.AddManagementModule()` (voir `Program.cs`).

## Licence

[GNU Affero General Public License v3.0](LICENSE).
