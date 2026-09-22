# Release 2.0 - Visual Studio 2026 recovery build

- Corrected the solution project type so `BC.FixedAsset.Web` loads as a C# Web Application.
- Modernized the Web Forms project metadata for Visual Studio 2026 and .NET Framework 4.8.
- Added a clear build-time message when ASP.NET Web Application targets are not installed.
- Completed administrator user create, edit, reset-password, per-application role, deactivate and delete flows.
- Added first-use `admin` credentials and a repeatable credential recovery path.
- Added one-command SQLCMD installation and installation verification scripts for SQL Server Express.
- Added Visual Studio prerequisite checking and Web project recovery documentation.

This package was structurally validated in the Linux workspace. A final compile, IIS Express smoke test and IIS deployment test must be run on Windows with Visual Studio/.NET Framework 4.8 installed.
