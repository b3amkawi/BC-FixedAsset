USE [BCFixedAsset];
GO
/* Idempotent upgrade from the previous package to v3. */
IF COL_LENGTH('fa.AssetSurveys','FixedAssetNo') IS NULL ALTER TABLE fa.AssetSurveys ADD FixedAssetNo nvarchar(80) NULL;
IF COL_LENGTH('fa.AssetSurveys','ReturnReason') IS NULL ALTER TABLE fa.AssetSurveys ADD ReturnReason nvarchar(1000) NULL;
IF COL_LENGTH('fa.AssetSurveys','ReturnedFromStatus') IS NULL ALTER TABLE fa.AssetSurveys ADD ReturnedFromStatus nvarchar(30) NULL;
GO
;WITH d AS(SELECT AttachmentId,ROW_NUMBER() OVER(PARTITION BY SurveyId,AttachmentType ORDER BY UploadedUtc DESC,AttachmentId DESC) rn FROM fa.AssetSurveyAttachments)
DELETE FROM d WHERE rn>1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('fa.AssetSurveyAttachments') AND name='UX_AssetSurveyAttachments_Type')
 CREATE UNIQUE INDEX UX_AssetSurveyAttachments_Type ON fa.AssetSurveyAttachments(SurveyId,AttachmentType);
GO
IF NOT EXISTS(SELECT 1 FROM sec.Roles WHERE RoleCode='ASSET_MANAGER')
 INSERT sec.Roles(RoleCode,RoleName,IsSystemRole,IsActive)VALUES('ASSET_MANAGER',N'Asset Manager',1,1);
DECLARE @Old int=(SELECT RoleId FROM sec.Roles WHERE RoleCode='DEPT_MANAGER'),@New int=(SELECT RoleId FROM sec.Roles WHERE RoleCode='ASSET_MANAGER');
IF @Old IS NOT NULL
BEGIN
 INSERT sec.UserApplicationRoles(UserId,ApplicationId,RoleId,IsActive)
 SELECT x.UserId,x.ApplicationId,@New,x.IsActive FROM sec.UserApplicationRoles x WHERE x.RoleId=@Old
 AND NOT EXISTS(SELECT 1 FROM sec.UserApplicationRoles n WHERE n.UserId=x.UserId AND n.ApplicationId=x.ApplicationId AND n.RoleId=@New);
 UPDATE sec.Roles SET IsActive=0 WHERE RoleId=@Old;
END
GO
PRINT N'BCFixedAsset v3 database upgrade completed.';
GO
