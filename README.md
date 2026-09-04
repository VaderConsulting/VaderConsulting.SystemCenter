# VaderConsulting.SystemCenter

C# .NET Framework 3.5 class library that builds and imports System Center Operations Manager 2012 Service Designer management packs from lists of component servers and services. `OperationsManager` connects to a caller-supplied SCOM management group, creates or updates a distributed-application pack (singleton service class based on `Microsoft.SystemCenter.ServiceDesigner.GenericService`, component-group class, GroupPopulator discovery, computer/service relationships, folders, and views), optionally seals it with a PowerShell runspace using `#MPFILENAME#` / `#KEYFILEFILENAME#` / `#KEYCOMPANYNAME#` tokens (default company Department of Finance), and imports the sealed `.mp` or unsealed pack. It also resolves computer and service IDs, reads server health, and emits membership XML for discoveries. Extra files on disk but not in the `.csproj` Compile list are `ServerUtilities` (SCOM plus SCSM connections, four XML templates, enum values) with `ServerUtilities_backup`, plus standalone `ConnectionInfo`, `ServiceInfo`, `ManagementPackCreationOptions`, and `ManagementPackSealOptions` types that duplicate nested classes inside `OperationsManager`; leftover `App.config` Entity Framework 6 LocalDB and `packages.config` AsyncBridge 0.1.1 are unused.

**Source last updated:** 2015-03-14 · **Language:** C# · **Target:** .NET Framework 3.5 · **Output:** class library (`Library`)

## Solution structure

| Project | Language | Type | Purpose |
|---------|----------|------|---------|
| `VaderConsulting.SystemCenter` (`VaderConsulting.SystemCenter.csproj`) | C# | class library (`net35`, SCOM 2012 SDK) | Builds, seals, and imports Service Designer management packs; health/ID lookups. |

## How to open

Open `VaderConsulting.SystemCenter.csproj` in Visual Studio 2013 or later (ToolsVersion 12.0). There is no `.sln` in this folder. The project references sibling `..\VaderConsulting.Helper\VaderConsulting.Helper.csproj`, SCOM 2012 SDK assemblies from System Center 2012 Visual Studio Authoring Extensions (`Microsoft.EnterpriseManagement.Core` / `Microsoft.EnterpriseManagement.OperationsManager` 7.0.5000.0 under `C:\Program Files\System Center 2012 Visual Studio Authoring Extensions\Tools\MPSimulator\OM2012\`), and GAC `System.Management.Automation` 1.0. `ServerUtilities.cs`, `ServerUtilities_backup.cs`, `ConnectionInfo.cs`, `ServiceInfo.cs`, `ManagementPackCreationOptions.cs`, and `ManagementPackSealOptions.cs` are present but not listed in the `.csproj` Compile items.

## Requirements

- Visual Studio 2013 or later, .NET Framework 3.5

## Attribution and provenance

Working copy from Dave Robinson's OneDrive Historical Dev folder `VaderConsulting.SystemCenter`. Assembly title/product `VaderConsulting.OperationsManager`; the Visual Studio template still has company/copyright Microsoft 2015. Namespace `VaderConsulting.SystemCenter`. `packages.config` lists AsyncBridge 0.1.1; `App.config` has leftover Entity Framework 6 LocalDB section. Neither is referenced by the `.csproj`.

## License

MIT © 2026 VaderConsulting. See `LICENSE`.
