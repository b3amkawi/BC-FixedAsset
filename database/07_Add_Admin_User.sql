/*
  Add or repair a BC Fixed Asset system administrator.

  Edit the values below before executing this script in SSMS.
  The temporary password for a newly created user is: BC-Admin@2026!
  Existing credentials are preserved unless the user has no credential row.
*/
USE [BCFixedAsset];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @UserName nvarchar(100)=N'admin';
DECLARE @Email nvarchar(256)=N'admin@bettercode.co.th';
DECLARE @FirstName nvarchar(100)=N'System';
DECLARE @LastName nvarchar(100)=N'Administrator';
DECLARE @Position nvarchar(150)=N'System Administrator';

BEGIN TRY
    BEGIN TRAN;

    DECLARE @AdminRoleId int=(SELECT RoleId FROM sec.Roles WHERE RoleCode=N'SYSTEM_ADMIN' AND IsActive=1);
    IF @AdminRoleId IS NULL
        THROW 50001,N'SYSTEM_ADMIN role was not found. Run the database seed/upgrade scripts first.',1;

    DECLARE @UserId int=(SELECT UserId FROM sec.Users WHERE UserName=@UserName);

    IF @UserId IS NULL
    BEGIN
        INSERT sec.Users
        (
            UserName,Email,FirstName,LastName,Position,DepartmentId,
            ProfileImagePath,AccountType,MustChangePassword,IsActive
        )
        VALUES
        (
            @UserName,@Email,@FirstName,@LastName,@Position,
            (SELECT TOP(1) DepartmentId FROM mst.Departments WHERE DepartmentCode=N'IT'),
            N'~/Assets/default-profile.svg',0,1,1
        );

        SET @UserId=CONVERT(int,SCOPE_IDENTITY());
    END
    ELSE
    BEGIN
        UPDATE sec.Users
        SET IsActive=1,AccountType=0,ModifiedUtc=SYSUTCDATETIME()
        WHERE UserId=@UserId;
    END;

    /* Insert the temporary credential only when the account has none. */
    IF NOT EXISTS(SELECT 1 FROM sec.UserCredentials WHERE UserId=@UserId)
    BEGIN
        INSERT sec.UserCredentials
        (
            UserId,PasswordHash,PasswordSalt,PasswordIterations,PasswordAlgorithm,
            PasswordChangedUtc,PasswordExpiresUtc,FailedLoginCount
        )
        VALUES
        (
            @UserId,
            0x302F84772BC4F9860671C029B8AB6D541F12E7D11E13E63F3B38349F23D07495,
            0x95E2C631A525C6E4EA495D432CC08CC3756E4FADE77A28C35E173DCBE1E545A8,
            120000,N'PBKDF2-HMAC-SHA256',SYSUTCDATETIME(),
            DATEADD(DAY,90,SYSUTCDATETIME()),0
        );

        UPDATE sec.Users SET MustChangePassword=1 WHERE UserId=@UserId;
    END;

    INSERT sec.UserApplicationRoles(UserId,ApplicationId,RoleId,IsActive)
    SELECT @UserId,a.ApplicationId,@AdminRoleId,1
    FROM sec.Applications a
    WHERE a.ApplicationCode IN(N'FIXED_ASSET',N'ADMIN')
      AND a.IsActive=1
      AND NOT EXISTS
      (
          SELECT 1 FROM sec.UserApplicationRoles x
          WHERE x.UserId=@UserId
            AND x.ApplicationId=a.ApplicationId
            AND x.RoleId=@AdminRoleId
      );

    UPDATE x SET IsActive=1
    FROM sec.UserApplicationRoles x
    JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId
    WHERE x.UserId=@UserId
      AND x.RoleId=@AdminRoleId
      AND a.ApplicationCode IN(N'FIXED_ASSET',N'ADMIN');

    COMMIT;

    SELECT u.UserId,u.UserName,u.Email,u.IsActive,u.MustChangePassword,
           a.ApplicationCode,r.RoleCode,x.IsActive AS RoleIsActive
    FROM sec.Users u
    JOIN sec.UserApplicationRoles x ON x.UserId=u.UserId
    JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId
    JOIN sec.Roles r ON r.RoleId=x.RoleId
    WHERE u.UserId=@UserId AND r.RoleCode=N'SYSTEM_ADMIN'
    ORDER BY a.ApplicationCode;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
GO
