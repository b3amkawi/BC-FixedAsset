# Deployment Checklist

## Build server prerequisites

- Windows Server with IIS and ASP.NET 4.x features enabled
- .NET Framework 4.8 runtime
- SQL Server Express or SQL Server
- Visual Studio/MSBuild with the .NET Framework 4.8 targeting pack for the build step
- HTTPS certificate and DNS name for Production

## Database

1. Take a SQL Server backup before upgrading an existing environment.
2. For a clean installation run `database/00_Install_All.sql` in SQLCMD mode. For an existing v2 database, back up first and run `database/05_Upgrade_v3.sql`.
3. Run `tools/Set-InitialAdminPassword.ps1` with a strong one-time password.
4. Grant the IIS application-pool identity only the database permissions required by the application.
5. Configure and test automated backup and restore.

## Application

1. Open `BC.FixedAsset.sln` and build the Release configuration.
2. Publish `BC.FixedAsset.Web` to a versioned staging directory.
3. Update the Production connection string through IIS/environment configuration; do not commit secrets.
4. Run `tools/Deploy-IIS.ps1` from an elevated PowerShell session.
5. Bind HTTPS, then set `requireSSL="true"` on Forms Authentication and cookies in `Web.config`.
6. Restrict write access to the configured upload directory and enable malware scanning before attachments are enabled.

## Smoke test

- Sign in as the seeded administrator and change the one-time password.
- Confirm that the Portal displays only assigned applications.
- Confirm direct URL access is blocked for applications without permission.
- Create a test user and assign different roles to Fixed Asset and Administration.
- Create and reopen an Asset Survey with quantity/UOM, dimensions, location, received date, PO and all three image types.
- Confirm owner-only visibility/edit/delete, administrator visibility, Asset Manager/Finance Return reasons and final registration.
- Confirm detail popup, Excel export, mobile camera input and QR tag printing.
- Save a numbering pattern and generate numbers concurrently to verify uniqueness.
- Verify account lockout, password expiry and password-history behavior.
- Verify mobile layout, Thai text, English labels, audit logging and error handling.

## Go-live gates

- Windows MSBuild succeeds with zero errors.
- SQL scripts complete on a clean database and the approved upgrade path.
- IIS smoke, UAT, security test, backup/restore test and rollback rehearsal pass.
- Entra ID SSO and full resource-based Thai/English localization are completed if required for the first release.
- Master Data CRUD and operational reports are completed and accepted.
