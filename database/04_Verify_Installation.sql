USE [BCFixedAsset];
GO

SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS SqlServerName;
SELECT s.name AS SchemaName FROM sys.schemas s WHERE s.name IN ('mst','sec','fa','audit') ORDER BY s.name;
SELECT ApplicationCode, NameEn, IsActive FROM sec.Applications ORDER BY DisplayOrder;
SELECT u.UserName, u.IsActive, u.MustChangePassword,
       CASE WHEN c.UserId IS NULL THEN 0 ELSE 1 END AS HasCredential
FROM sec.Users u
LEFT JOIN sec.UserCredentials c ON c.UserId=u.UserId
WHERE u.UserName='admin';
SELECT a.ApplicationCode, r.RoleCode
FROM sec.UserApplicationRoles x
JOIN sec.Users u ON u.UserId=x.UserId
JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId
JOIN sec.Roles r ON r.RoleId=x.RoleId
WHERE u.UserName='admin'
ORDER BY a.ApplicationCode;
SELECT c.name AS RequiredSurveyColumn FROM sys.columns c WHERE c.object_id=OBJECT_ID('fa.AssetSurveys') AND c.name IN('FixedAssetNo','ReturnReason','ReturnedFromStatus');
SELECT RoleCode,RoleName,IsActive FROM sec.Roles WHERE RoleCode IN('ASSET_MANAGER','FINANCE','SURVEYOR');
SELECT name AS AttachmentIndex FROM sys.indexes WHERE object_id=OBJECT_ID('fa.AssetSurveyAttachments') AND name='UX_AssetSurveyAttachments_Type';
GO
