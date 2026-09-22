# BC Fixed Asset Management v3.1 UI Sync

Production foundation for Better Code's fixed-asset survey, approval and registration workflow.

The Web Forms presentation layer in v3.1 is synchronized with the currently deployed BC Fixed Asset Site. See `UI_PARITY_v3.1.md` for the page/component mapping.

## Technology

- ASP.NET Web Forms on .NET Framework 4.8
- C# class-library separation: Core, Data, Services and Web
- SQL Server Express / SQL Server
- IIS with Forms Authentication for the first deployment
- Microsoft Entra ID SSO-ready configuration boundary

## Version 3 scope

- Central sign-in and Application Portal
- Application-level access control
- BC Administration: Application Portal, Users & Roles and Master Data
- User profile and secure local/fallback credentials
- Fixed Asset Dashboard
- Asset Survey with quantity, UOM, dimensions, weight, receiving date, PO and named master-data location
- Three image types (Actual, Serial and Other), instant preview and mobile-camera capture
- Owner-only edit/delete before submission; administrators can inspect all records
- Asset Manager review queue with Approve/Return and mandatory return reason
- Finance review queue
- Detail popup, thumbnail list, Excel-compatible export and printable QR asset tag
- Loading/progress overlay for server operations
- Fixed Asset Register
- Configurable Fixed Asset number scheme and transactional running number
- Reports foundation and Audit Log schema

Not included in this phase: Transfer & Movement, Maintenance, Disposal and Physical Verification.

## Open in Visual Studio

1. Install Visual Studio 2026 Community, Professional or Enterprise with **ASP.NET and web development**, **.NET Framework 4.8 SDK/targeting pack** and **IIS Express**.
2. Open `BC.FixedAsset.sln`.
3. Set `BC.FixedAsset.Web` as the startup project.
4. Restore/build the solution. The solution has no external NuGet dependency in this foundation.

## Create the database

For a new database, enable **Query > SQLCMD Mode** in SQL Server Management Studio, open the `database` folder and run:

1. `database/00_Install_All.sql`

Or run the component scripts in order:

1. `database/01_CreateDatabase.sql`
2. `database/02_Schema.sql`
3. `database/03_Seed_Data.sql`
4. `database/05_Upgrade_v3.sql`
5. `database/06_Upgrade_v4.sql`

For an existing database that only needs the reference data, run `database/08_Default_Data_Setup.sql` directly in SSMS. It can be run repeatedly without duplicating rows or resetting the administrator password. To add or restore the system administrator separately, run `database/07_Add_Admin_User.sql`. Do not re-run `03_Seed_Data.sql` against an existing database unless you intend to reset the `admin` credential.

The seed creates a first-use administrator:

- Username: `admin`
- Temporary password: `BC-Admin@2026!`
- Password change: required at first sign-in

Change the temporary password immediately. You can also reset it from an elevated PowerShell prompt before first use:

```powershell
.\tools\Set-InitialAdminPassword.ps1 -Password "Replace-With-A-Strong-Password"
```

No decryptable password or decryption key is stored. Local passwords use PBKDF2-HMAC-SHA256 with a unique salt, so there is deliberately no decryption key to disclose or manage.

## Configuration

Update `src/BC.FixedAsset.Web/Web.config`:

- `BCFixedAsset`: SQL Server Express connection string
- `EnvironmentName`: Development, UAT or Production
- `AuthenticationMode`: Local during the first deployment; Entra ID integration is the next security increment
- `UploadRoot`: attachment location

For IIS Production, set `requireSSL="true"` for authentication and cookies after HTTPS is configured. Do not commit production secrets or client secrets to `Web.config`.

## Current implementation status

The solution contains the local authentication and central application authorization foundation plus the latest Asset Survey workflow: three secured image attachments, previews/mobile capture, owner/admin row-level access, draft editing/deletion, Asset Manager and Finance approval/return actions, return reasons, automatic registration number generation, detail popup, Excel-compatible export and QR tag printing.

This is a production-oriented implementation package, not a production go-live approval. Before go-live, complete Windows MSBuild/IIS validation, UAT, Microsoft Entra ID integration, full Thai/English resource localization, Master Data CRUD completion, backup/restore testing and security testing.

The project files target standard .NET Framework 4.8 Web Application format and can be opened in current Visual Studio versions. Build and IIS smoke tests must be performed on Windows because this workspace does not include MSBuild for .NET Framework or IIS.

See `docs/DEPLOYMENT_CHECKLIST.md` for the build, IIS, database, smoke-test and go-live gates.
See `docs/VISUAL_STUDIO_SETUP.md` for the exact Web project unload recovery steps.
