# Visual Studio setup and Web project recovery

## Required Visual Studio components

Use Visual Studio 2026 Community, Professional or Enterprise with:

- ASP.NET and web development workload
- .NET Framework 4.8 SDK
- .NET Framework 4.8 targeting pack
- IIS Express

The current Express product line does not provide the complete classic ASP.NET Web Application workload. If the installed product is an Express edition, install Visual Studio Community 2026 or later.

## Open the solution

1. Close every Visual Studio window that has the old solution open.
2. Open `BC.FixedAsset.sln` directly. Do not use **Open Folder**.
3. In Solution Explorer, right-click `BC.FixedAsset.Web` and choose **Set as Startup Project**.
4. Select IIS Express and press **F5**.

## If `BC.FixedAsset.Web` is unloaded

1. Open **Visual Studio Installer** and choose **Modify**.
2. Install the components listed above, then restart Visual Studio.
3. Reopen this corrected solution and right-click the Web project > **Reload Project**.
4. If the project remains unloaded, right-click it and choose **Edit Project File** or inspect the Output window. The project now reports a clear build error when the Web Application targets are missing.

The solution project entry uses the C# project type GUID. The Web Application flavor remains in `BC.FixedAsset.Web.csproj`, which is the format expected by modern Visual Studio versions for a .NET Framework 4.8 Web Forms project.

## Database and first sign-in

1. In SQL Server Management Studio, enable **Query > SQLCMD Mode**.
2. Run `database\00_Install_All.sql` from its own folder.
3. Run `database\04_Verify_Installation.sql` and confirm `admin` has credentials and both application roles.
4. Start the Web project and sign in with:
   - Username: `admin`
   - Temporary password: `BC-Admin@2026!`
5. The application requires a password change after first sign-in.

Re-running `03_Seed_Data.sql` resets the `admin` credential to this temporary password so an environment with the earlier unusable credential can be recovered.

Use `tools\Set-InitialAdminPassword.ps1` to replace the temporary password before handing the environment to another person.
