/*
  BC Fixed Asset reference data for an existing BCFixedAsset database.
  Safe to run again: inserts missing defaults only. Does not change users,
  passwords, application assignments, surveys, or registered assets.
  Run directly in SSMS; SQLCMD Mode is not required.
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
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'mst.Companies',N'U') IS NULL OR OBJECT_ID(N'fa.AssetNumberSchemes',N'U') IS NULL
    THROW 50010,N'BCFixedAsset schema is missing. Create the database schema before loading default data.',1;

BEGIN TRY
    BEGIN TRAN;

    IF NOT EXISTS(SELECT 1 FROM mst.Companies WHERE CompanyCode=N'BC')
        INSERT mst.Companies(CompanyCode,CompanyName) VALUES(N'BC',N'Better Code Co., Ltd.');
    DECLARE @CompanyId int=(SELECT CompanyId FROM mst.Companies WHERE CompanyCode=N'BC');

    IF NOT EXISTS(SELECT 1 FROM mst.Divisions WHERE CompanyId=@CompanyId AND DivisionCode=N'PE')
        INSERT mst.Divisions(CompanyId,DivisionCode,DivisionName) VALUES(@CompanyId,N'PE',N'Project & Engineering');
    IF NOT EXISTS(SELECT 1 FROM mst.Divisions WHERE CompanyId=@CompanyId AND DivisionCode=N'FA')
        INSERT mst.Divisions(CompanyId,DivisionCode,DivisionName) VALUES(@CompanyId,N'FA',N'Finance & Administration');
    DECLARE @ProjectDivision int=(SELECT DivisionId FROM mst.Divisions WHERE CompanyId=@CompanyId AND DivisionCode=N'PE');
    DECLARE @FinanceDivision int=(SELECT DivisionId FROM mst.Divisions WHERE CompanyId=@CompanyId AND DivisionCode=N'FA');

    IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode=N'MGT')
        INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(NULL,N'MGT',N'Management');
    IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode=N'PRJ')
        INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(@ProjectDivision,N'PRJ',N'Project & Development');
    IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode=N'ENG')
        INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(@ProjectDivision,N'ENG',N'Engineering');
    IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode=N'IT')
        INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(@ProjectDivision,N'IT',N'Information Technology');
    IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode=N'FIN')
        INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(@FinanceDivision,N'FIN',N'Finance & Accounting');
    IF NOT EXISTS(SELECT 1 FROM mst.Departments WHERE DepartmentCode=N'WH')
        INSERT mst.Departments(DivisionId,DepartmentCode,DepartmentName) VALUES(NULL,N'WH',N'Warehouse');

    INSERT mst.AssetCategories(CategoryCode,CategoryName,UsefulLifeMonths)
    SELECT v.Code,v.Name,v.Life FROM(VALUES
        (N'LB',N'Land & Building',240),(N'BI',N'Building Improvement',120),
        (N'MC',N'Machinery',120),(N'TE',N'Tools & Equipment',60),
        (N'VH',N'Vehicles',60),(N'IT',N'IT Equipment',36),
        (N'OE',N'Office Equipment',60),(N'FF',N'Furniture',60),
        (N'WH',N'Warehouse Equipment',60),(N'SF',N'Safety Equipment',60),
        (N'OT',N'Other',60)) v(Code,Name,Life)
    WHERE NOT EXISTS(SELECT 1 FROM mst.AssetCategories x WHERE x.CategoryCode=v.Code);

    INSERT mst.Uoms(UomCode,UomNameTh,UomNameEn)
    SELECT v.Code,v.Th,v.En FROM(VALUES
        (N'PCS',N'ชิ้น',N'Piece'),(N'UNIT',N'หน่วย',N'Unit'),
        (N'SET',N'ชุด',N'Set'),(N'MACHINE',N'เครื่อง',N'Machine'),
        (N'BOX',N'กล่อง',N'Box')) v(Code,Th,En)
    WHERE NOT EXISTS(SELECT 1 FROM mst.Uoms x WHERE x.UomCode=v.Code);

    INSERT mst.AssetConditions(ConditionCode,ConditionName,DisplayOrder)
    SELECT v.Code,v.Name,v.Ord FROM(VALUES
        (N'GOOD',N'ใช้งานปกติ',1),(N'FAIR',N'ชำรุดเล็กน้อย',2),
        (N'REPAIR',N'ต้องซ่อม',3),(N'DAMAGED',N'ชำรุด',4),
        (N'NOT_WORKING',N'ใช้งานไม่ได้',5)) v(Code,Name,Ord)
    WHERE NOT EXISTS(SELECT 1 FROM mst.AssetConditions x WHERE x.ConditionCode=v.Code);

    IF NOT EXISTS(SELECT 1 FROM mst.Buildings WHERE BuildingCode=N'HO')
        INSERT mst.Buildings(BuildingCode,BuildingName) VALUES(N'HO',N'Head Office');
    IF NOT EXISTS(SELECT 1 FROM mst.Buildings WHERE BuildingCode=N'F1')
        INSERT mst.Buildings(BuildingCode,BuildingName) VALUES(N'F1',N'Factory 1');
    IF NOT EXISTS(SELECT 1 FROM mst.Buildings WHERE BuildingCode=N'WHA')
        INSERT mst.Buildings(BuildingCode,BuildingName) VALUES(N'WHA',N'Warehouse A');
    DECLARE @HO int=(SELECT BuildingId FROM mst.Buildings WHERE BuildingCode=N'HO');
    DECLARE @F1 int=(SELECT BuildingId FROM mst.Buildings WHERE BuildingCode=N'F1');
    DECLARE @WHA int=(SELECT BuildingId FROM mst.Buildings WHERE BuildingCode=N'WHA');

    IF NOT EXISTS(SELECT 1 FROM mst.Floors WHERE BuildingId=@HO AND FloorCode=N'2F')
        INSERT mst.Floors(BuildingId,FloorCode,FloorName,DisplayOrder) VALUES(@HO,N'2F',N'2F',2);
    IF NOT EXISTS(SELECT 1 FROM mst.Floors WHERE BuildingId=@HO AND FloorCode=N'3F')
        INSERT mst.Floors(BuildingId,FloorCode,FloorName,DisplayOrder) VALUES(@HO,N'3F',N'3F',3);
    IF NOT EXISTS(SELECT 1 FROM mst.Floors WHERE BuildingId=@F1 AND FloorCode=N'1F')
        INSERT mst.Floors(BuildingId,FloorCode,FloorName,DisplayOrder) VALUES(@F1,N'1F',N'1F',1);
    IF NOT EXISTS(SELECT 1 FROM mst.Floors WHERE BuildingId=@WHA AND FloorCode=N'GF')
        INSERT mst.Floors(BuildingId,FloorCode,FloorName,DisplayOrder) VALUES(@WHA,N'GF',N'Ground Floor',1);
    DECLARE @HO2 int=(SELECT FloorId FROM mst.Floors WHERE BuildingId=@HO AND FloorCode=N'2F');
    DECLARE @HO3 int=(SELECT FloorId FROM mst.Floors WHERE BuildingId=@HO AND FloorCode=N'3F');
    DECLARE @F11 int=(SELECT FloorId FROM mst.Floors WHERE BuildingId=@F1 AND FloorCode=N'1F');
    DECLARE @WHAG int=(SELECT FloorId FROM mst.Floors WHERE BuildingId=@WHA AND FloorCode=N'GF');

    IF NOT EXISTS(SELECT 1 FROM mst.Rooms WHERE FloorId=@HO2 AND RoomCode=N'ACC')
        INSERT mst.Rooms(FloorId,RoomCode,RoomName) VALUES(@HO2,N'ACC',N'Accounting Office');
    IF NOT EXISTS(SELECT 1 FROM mst.Rooms WHERE FloorId=@HO3 AND RoomCode=N'IT')
        INSERT mst.Rooms(FloorId,RoomCode,RoomName) VALUES(@HO3,N'IT',N'IT Office');
    IF NOT EXISTS(SELECT 1 FROM mst.Rooms WHERE FloorId=@F11 AND RoomCode=N'VISION')
        INSERT mst.Rooms(FloorId,RoomCode,RoomName) VALUES(@F11,N'VISION',N'Vision Lab');
    IF NOT EXISTS(SELECT 1 FROM mst.Rooms WHERE FloorId=@WHAG AND RoomCode=N'LOAD')
        INSERT mst.Rooms(FloorId,RoomCode,RoomName) VALUES(@WHAG,N'LOAD',N'Loading Bay');

    INSERT sec.Roles(RoleCode,RoleName,IsSystemRole)
    SELECT v.Code,v.Name,1 FROM(VALUES
        (N'SYSTEM_ADMIN',N'System Administrator'),(N'SURVEYOR',N'Asset Surveyor'),
        (N'ASSET_MANAGER',N'Asset Manager'),(N'FINANCE',N'Finance Reviewer'),
        (N'MANAGEMENT',N'Management Viewer')) v(Code,Name)
    WHERE NOT EXISTS(SELECT 1 FROM sec.Roles r WHERE r.RoleCode=v.Code);

    IF NOT EXISTS(SELECT 1 FROM sec.Applications WHERE ApplicationCode=N'FIXED_ASSET')
        INSERT sec.Applications
        (ApplicationCode,NameTh,NameEn,DescriptionTh,DescriptionEn,IconText,TargetUrl,DisplayOrder,IsActive)
        VALUES(N'FIXED_ASSET',N'BC Fixed Asset',N'BC Fixed Asset',
               N'จัดการ Asset Survey การอนุมัติ และทะเบียนทรัพย์สิน',
               N'Manage asset surveys, approvals, and the asset register',N'FA',N'~/FixedAsset/Dashboard.aspx',1,1);
    IF NOT EXISTS(SELECT 1 FROM sec.Applications WHERE ApplicationCode=N'ADMIN')
        INSERT sec.Applications
        (ApplicationCode,NameTh,NameEn,DescriptionTh,DescriptionEn,IconText,TargetUrl,DisplayOrder,IsActive)
        VALUES(N'ADMIN',N'BC Administration',N'BC Administration',
               N'บริหาร Application Portal ผู้ใช้ สิทธิ์ และ Master Data',
               N'Manage applications, users, access, and master data',N'AD',N'~/Admin/Dashboard.aspx',2,1);

    IF NOT EXISTS(SELECT 1 FROM fa.AssetNumberSchemes WHERE CompanyId=@CompanyId AND IsActive=1)
        INSERT fa.AssetNumberSchemes
        (CompanyId,SchemeName,Pattern,SequenceDigits,SequenceScope,ResetPolicy,StartValue,EffectiveDate)
        VALUES(@CompanyId,N'BC Standard Asset Number',N'{COMPANY}-{CATEGORY}-{YEAR}-{SEQ}',
               4,N'Company + Category + Year',N'Yearly',1,'2026-01-01');

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;

SELECT N'Companies' AS DataSet,COUNT(*) AS [RowCount] FROM mst.Companies
UNION ALL SELECT N'Departments',COUNT(*) FROM mst.Departments
UNION ALL SELECT N'Categories',COUNT(*) FROM mst.AssetCategories
UNION ALL SELECT N'UOMs',COUNT(*) FROM mst.Uoms
UNION ALL SELECT N'Conditions',COUNT(*) FROM mst.AssetConditions
UNION ALL SELECT N'Buildings',COUNT(*) FROM mst.Buildings
UNION ALL SELECT N'Floors',COUNT(*) FROM mst.Floors
UNION ALL SELECT N'Rooms',COUNT(*) FROM mst.Rooms
UNION ALL SELECT N'Roles',COUNT(*) FROM sec.Roles
UNION ALL SELECT N'Applications',COUNT(*) FROM sec.Applications
UNION ALL SELECT N'Numbering schemes',COUNT(*) FROM fa.AssetNumberSchemes;
GO
