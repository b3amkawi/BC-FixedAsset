/* Remove password rules from existing installations without deleting credentials or history. */
USE [BCFixedAsset];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO
BEGIN TRAN;
UPDATE sec.PasswordPolicies SET IsActive=0,ModifiedUtc=SYSUTCDATETIME() WHERE IsActive=1;
UPDATE sec.Users SET MustChangePassword=0,ModifiedUtc=SYSUTCDATETIME() WHERE MustChangePassword=1;
UPDATE sec.UserCredentials SET PasswordExpiresUtc=NULL WHERE PasswordExpiresUtc IS NOT NULL;
COMMIT;
GO
