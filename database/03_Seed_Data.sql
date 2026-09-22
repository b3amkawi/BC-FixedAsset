USE [BCFixedAsset];
GO
IF NOT EXISTS(SELECT 1 FROM mst.Companies WHERE CompanyCode='BC') INSERT mst.Companies(CompanyCode,CompanyName) VALUES('BC',N'Better Code Co., Ltd.');
DECLARE @CompanyId int=(SELECT CompanyId FROM mst.Companies WHERE CompanyCode='BC');
IF NOT EXISTS(SELECT 1 FROM mst.Divisions WHERE DivisionCode='PE') INSERT mst.Divisions(CompanyId,DivisionCode,DivisionName) VALUES(@CompanyId,'PE',N'Project & Engineering');
IF NOT EXISTS(SELECT 1 FROM mst.Divisions WHERE DivisionCode='FA') INSERT mst.Divisions(CompanyId,DivisionCode,DivisionName) VALUES(@CompanyId,'FA',N'Finance & Administration');
DECLARE @ProjectDivision int=(SELECT DivisionId FROM mst.Divisions WHERE DivisionCode='PE'),@FinanceDivision int=(SELECT DivisionId FROM mst.Divisions WHERE DivisionCode='FA');
IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode='MGT') INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(NULL,'MGT',N'Management');
IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode='PRJ') INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(@ProjectDivision,'PRJ',N'Project & Development');
IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode='ENG') INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(@ProjectDivision,'ENG',N'Engineering');
IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode='IT') INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(@ProjectDivision,'IT',N'Information Technology');
IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode='FIN') INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(@FinanceDivision,'FIN',N'Finance & Accounting');
IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode='WH') INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(NULL,'WH',N'Warehouse');
GO
INSERT mst.AssetCategories(CategoryCode,CategoryName,UsefulLifeMonths) SELECT v.Code,v.Name,v.Life FROM(VALUES('LB',N'Land & Building',240),('BI',N'Building Improvement',120),('MC',N'Machinery',120),('TE',N'Tools & Equipment',60),('VH',N'Vehicles',60),('IT',N'IT Equipment',36),('OE',N'Office Equipment',60),('FF',N'Furniture',60),('WH',N'Warehouse Equipment',60),('SF',N'Safety Equipment',60),('OT',N'Other',60))v(Code,Name,Life) WHERE NOT EXISTS(SELECT 1 FROM mst.AssetCategories x WHERE x.CategoryCode=v.Code);
INSERT mst.Uoms(UomCode,UomNameTh,UomNameEn) SELECT v.Code,v.Th,v.En FROM(VALUES('PCS',N'ชิ้น',N'Piece'),('UNIT',N'หน่วย',N'Unit'),('SET',N'ชุด',N'Set'),('MACHINE',N'เครื่อง',N'Machine'),('BOX',N'กล่อง',N'Box'))v(Code,Th,En) WHERE NOT EXISTS(SELECT 1 FROM mst.Uoms x WHERE x.UomCode=v.Code);
INSERT mst.AssetConditions(ConditionCode,ConditionName,DisplayOrder) SELECT v.Code,v.Name,v.Ord FROM(VALUES('GOOD',N'ใช้งานปกติ',1),('FAIR',N'ชำรุดเล็กน้อย',2),('REPAIR',N'ต้องซ่อม',3),('DAMAGED',N'ชำรุด',4),('NOT_WORKING',N'ใช้งานไม่ได้',5))v(Code,Name,Ord) WHERE NOT EXISTS(SELECT 1 FROM mst.AssetConditions x WHERE x.ConditionCode=v.Code);
GO
IF NOT EXISTS(SELECT 1 FROM mst.Buildings WHERE BuildingCode='HO') INSERT mst.Buildings(BuildingCode,BuildingName) VALUES('HO',N'Head Office');
IF NOT EXISTS(SELECT 1 FROM mst.Buildings WHERE BuildingCode='F1') INSERT mst.Buildings(BuildingCode,BuildingName) VALUES('F1',N'Factory 1');
IF NOT EXISTS(SELECT 1 FROM mst.Buildings WHERE BuildingCode='WHA') INSERT mst.Buildings(BuildingCode,BuildingName) VALUES('WHA',N'Warehouse A');
DECLARE @HO int=(SELECT BuildingId FROM mst.Buildings WHERE BuildingCode='HO'),@F1 int=(SELECT BuildingId FROM mst.Buildings WHERE BuildingCode='F1'),@WHA int=(SELECT BuildingId FROM mst.Buildings WHERE BuildingCode='WHA');
IF NOT EXISTS(SELECT 1 FROM mst.Floors WHERE BuildingId=@HO AND FloorCode='2F') INSERT mst.Floors(BuildingId,FloorCode,FloorName,DisplayOrder) VALUES(@HO,'2F','2F',2);
IF NOT EXISTS(SELECT 1 FROM mst.Floors WHERE BuildingId=@HO AND FloorCode='3F') INSERT mst.Floors(BuildingId,FloorCode,FloorName,DisplayOrder) VALUES(@HO,'3F','3F',3);
IF NOT EXISTS(SELECT 1 FROM mst.Floors WHERE BuildingId=@F1 AND FloorCode='1F') INSERT mst.Floors(BuildingId,FloorCode,FloorName,DisplayOrder) VALUES(@F1,'1F','1F',1);
IF NOT EXISTS(SELECT 1 FROM mst.Floors WHERE BuildingId=@WHA AND FloorCode='GF') INSERT mst.Floors(BuildingId,FloorCode,FloorName,DisplayOrder) VALUES(@WHA,'GF',N'Ground Floor',1);
DECLARE @HO2 int=(SELECT FloorId FROM mst.Floors WHERE BuildingId=@HO AND FloorCode='2F'),@HO3 int=(SELECT FloorId FROM mst.Floors WHERE BuildingId=@HO AND FloorCode='3F'),@F11 int=(SELECT FloorId FROM mst.Floors WHERE BuildingId=@F1 AND FloorCode='1F'),@WHAG int=(SELECT FloorId FROM mst.Floors WHERE BuildingId=@WHA AND FloorCode='GF');
IF NOT EXISTS(SELECT 1 FROM mst.Rooms WHERE FloorId=@HO2 AND RoomCode='ACC') INSERT mst.Rooms(FloorId,RoomCode,RoomName) VALUES(@HO2,'ACC',N'Accounting Office');
IF NOT EXISTS(SELECT 1 FROM mst.Rooms WHERE FloorId=@HO3 AND RoomCode='IT') INSERT mst.Rooms(FloorId,RoomCode,RoomName) VALUES(@HO3,'IT',N'IT Office');
IF NOT EXISTS(SELECT 1 FROM mst.Rooms WHERE FloorId=@F11 AND RoomCode='VISION') INSERT mst.Rooms(FloorId,RoomCode,RoomName) VALUES(@F11,'VISION',N'Vision Lab');
IF NOT EXISTS(SELECT 1 FROM mst.Rooms WHERE FloorId=@WHAG AND RoomCode='LOAD') INSERT mst.Rooms(FloorId,RoomCode,RoomName) VALUES(@WHAG,'LOAD',N'Loading Bay');
GO
IF NOT EXISTS(SELECT 1 FROM sec.PasswordPolicies WHERE IsActive=1) INSERT sec.PasswordPolicies(PolicyName,MinimumLength,RequireUppercase,RequireLowercase,RequireNumber,RequireSpecialCharacter,PasswordHistoryCount,MaximumFailedAttempts,LockoutMinutes,ExpiryDays) VALUES(N'BC Standard',12,1,1,1,1,5,5,15,90);
INSERT sec.Roles(RoleCode,RoleName,IsSystemRole) SELECT v.Code,v.Name,1 FROM(VALUES('SYSTEM_ADMIN',N'System Administrator'),('SURVEYOR',N'Asset Surveyor'),('ASSET_MANAGER',N'Asset Manager'),('FINANCE',N'Finance Reviewer'),('MANAGEMENT',N'Management Viewer'))v(Code,Name) WHERE NOT EXISTS(SELECT 1 FROM sec.Roles r WHERE r.RoleCode=v.Code);
IF NOT EXISTS(SELECT 1 FROM sec.Applications WHERE ApplicationCode='FIXED_ASSET') INSERT sec.Applications(ApplicationCode,NameTh,NameEn,DescriptionTh,DescriptionEn,IconText,TargetUrl,DisplayOrder,IsActive) VALUES('FIXED_ASSET',N'BC Fixed Asset',N'BC Fixed Asset',N'จัดการ Asset Survey การอนุมัติ และทะเบียนทรัพย์สิน',N'Manage asset surveys, approvals, and the asset register','FA','~/FixedAsset/Dashboard.aspx',1,1);
IF NOT EXISTS(SELECT 1 FROM sec.Applications WHERE ApplicationCode='ADMIN') INSERT sec.Applications(ApplicationCode,NameTh,NameEn,DescriptionTh,DescriptionEn,IconText,TargetUrl,DisplayOrder,IsActive) VALUES('ADMIN',N'BC Administration',N'BC Administration',N'บริหาร Application Portal ผู้ใช้ สิทธิ์ และ Master Data',N'Manage applications, users, access, and master data','AD','~/Admin/Dashboard.aspx',2,1);
GO
IF NOT EXISTS(SELECT 1 FROM sec.Users WHERE UserName='admin') INSERT sec.Users(UserName,Email,FirstName,LastName,Position,DepartmentId,ProfileImagePath,AccountType,MustChangePassword,IsActive) VALUES('admin','admin@bettercode.co.th',N'System',N'Administrator',N'System Administrator',(SELECT DepartmentId FROM mst.Departments WHERE DepartmentCode='IT'),'~/Assets/default-profile.svg',0,1,1);
UPDATE sec.Users SET AccountType=0,MustChangePassword=1,IsActive=1,ModifiedUtc=SYSUTCDATETIME() WHERE UserName='admin';
DECLARE @AdminUser int=(SELECT UserId FROM sec.Users WHERE UserName='admin'),@AdminRole int=(SELECT RoleId FROM sec.Roles WHERE RoleCode='SYSTEM_ADMIN');
MERGE sec.UserCredentials AS target
USING (SELECT @AdminUser AS UserId) AS source ON target.UserId=source.UserId
WHEN MATCHED THEN
  UPDATE SET PasswordHash=0x302F84772BC4F9860671C029B8AB6D541F12E7D11E13E63F3B38349F23D07495,
             PasswordSalt=0x95E2C631A525C6E4EA495D432CC08CC3756E4FADE77A28C35E173DCBE1E545A8,
             PasswordIterations=120000,PasswordAlgorithm=N'PBKDF2-HMAC-SHA256',
             PasswordChangedUtc=SYSUTCDATETIME(),PasswordExpiresUtc=DATEADD(DAY,90,SYSUTCDATETIME()),
             FailedLoginCount=0,LockedUntilUtc=NULL
WHEN NOT MATCHED THEN
  INSERT(UserId,PasswordHash,PasswordSalt,PasswordIterations,PasswordAlgorithm,PasswordChangedUtc,PasswordExpiresUtc,FailedLoginCount)
  VALUES(@AdminUser,0x302F84772BC4F9860671C029B8AB6D541F12E7D11E13E63F3B38349F23D07495,0x95E2C631A525C6E4EA495D432CC08CC3756E4FADE77A28C35E173DCBE1E545A8,120000,N'PBKDF2-HMAC-SHA256',SYSUTCDATETIME(),DATEADD(DAY,90,SYSUTCDATETIME()),0);
INSERT sec.UserApplicationRoles(UserId,ApplicationId,RoleId) SELECT @AdminUser,a.ApplicationId,@AdminRole FROM sec.Applications a WHERE a.ApplicationCode IN('FIXED_ASSET','ADMIN') AND NOT EXISTS(SELECT 1 FROM sec.UserApplicationRoles x WHERE x.UserId=@AdminUser AND x.ApplicationId=a.ApplicationId AND x.RoleId=@AdminRole);
DECLARE @Company int=(SELECT CompanyId FROM mst.Companies WHERE CompanyCode='BC');
IF NOT EXISTS(SELECT 1 FROM fa.AssetNumberSchemes WHERE CompanyId=@Company AND IsActive=1) INSERT fa.AssetNumberSchemes(CompanyId,SchemeName,Pattern,SequenceDigits,SequenceScope,ResetPolicy,StartValue,EffectiveDate) VALUES(@Company,N'BC Standard Asset Number',N'{COMPANY}-{CATEGORY}-{YEAR}-{SEQ}',4,N'Company + Category + Year',N'Yearly',1,'2026-01-01');
GO
