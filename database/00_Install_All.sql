/*
  BC Fixed Asset - SQL Server Express installation

  Run this file in SQL Server Management Studio with SQLCMD Mode enabled:
  Query > SQLCMD Mode, then Execute.

  The referenced scripts create BCFixedAsset, all schemas/tables/procedures,
  master data, applications, roles and the first administrator account.
*/
:ON ERROR EXIT
:r .\01_CreateDatabase.sql
:r .\02_Schema.sql
:r .\03_Seed_Data.sql
:r .\05_Upgrade_v3.sql
:r .\06_Upgrade_v4.sql

PRINT N'BCFixedAsset installation completed.';
GO
