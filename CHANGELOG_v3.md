# BC Fixed Asset v3 update

## Asset Survey

- Added secured uploads for Actual, Serial and Other images (5 MB each), browser previews and mobile camera hints.
- Added reopening of Draft/Returned records, owner/admin row-level access and deletion before submission.
- Added custodian selection, creator name, named locations, dimensions, weight, quantity/UOM and status columns.
- Added detail popup, Excel-compatible export, loading overlay and printable QR tag.

## Workflow

- Renamed Department Manager to Asset Manager.
- Added Submit, Asset Manager Approve/Return and Finance Register/Return actions.
- Return requires a reason and is recorded in the survey, approval task and audit log.
- Finance registration generates the configured Fixed Asset number transactionally.

## Database

- `05_Upgrade_v3.sql` adds the new survey fields, attachment uniqueness and Asset Manager role without recreating the database.
- `00_Install_All.sql` includes the v3 upgrade automatically for clean installation.
- `04_Verify_Installation.sql` verifies the v3 columns, role and attachment index.

## Validation note

Project/config XML and package references are validated in this workspace. Compile and IIS runtime validation must be completed on Windows with the Visual Studio ASP.NET workload and .NET Framework 4.8 targeting pack.
