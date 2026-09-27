# GlpiNg.Modules.Management

*[Version française](README.md)*

GlpiNg's Management module: third parties, contracts, finance and infrastructure.

> **Disclaimer** — GlpiNg is an independent project. It is not affiliated with, endorsed,
> supported or sponsored by Teclib' or the GLPI project. "GLPI" and "GLPI-Agent" are trademarks
> of their respective owners; they are mentioned here only to describe GlpiNg's compatibility
> with the GLPI-Agent protocol and import from a GLPI database.

## Contents

- Suppliers and contacts
- Contracts and their costs, budgets
- Software licenses, phone lines, certificates, domains
- Datacenters, clusters, appliances, database instances
- Change history

## Usage

This repository is a submodule of [GlpiNg](https://github.com/GlpiNg-fr/GlpiNg), under
`src/GlpiNg.Modules.Management`. It does not build on its own: it references `GlpiNg.Modules.Abstractions` by relative path.

```bash
git clone --recurse-submodules https://github.com/GlpiNg-fr/GlpiNg.git
```

The host registers it with `services.AddManagementModule()` (see `Program.cs`).

## License

[GNU Affero General Public License v3.0](LICENSE).
