USE [BCFixedAsset];
GO
/* Asset Survey usability and database-backed photos. Safe to run more than once. */
IF COL_LENGTH('fa.AssetSurveys','CustodianName') IS NULL
    ALTER TABLE fa.AssetSurveys ADD CustodianName nvarchar(250) NULL;
IF COL_LENGTH('fa.FixedAssets','CustodianName') IS NULL
    ALTER TABLE fa.FixedAssets ADD CustodianName nvarchar(250) NULL;
IF COL_LENGTH('fa.AssetSurveyAttachments','FileContent') IS NULL
    ALTER TABLE fa.AssetSurveyAttachments ADD FileContent varbinary(max) NULL;
GO

/* Keep the visible name when migrating records that used a user-account custodian. */
UPDATE s SET CustodianName=u.DisplayName
FROM fa.AssetSurveys s
JOIN sec.Users u ON u.UserId=s.CustodianUserId
WHERE NULLIF(LTRIM(RTRIM(s.CustodianName)),N'') IS NULL;

UPDATE f SET CustodianName=u.DisplayName
FROM fa.FixedAssets f
JOIN sec.Users u ON u.UserId=f.CustodianUserId
WHERE NULLIF(LTRIM(RTRIM(f.CustodianName)),N'') IS NULL;
GO

CREATE OR ALTER PROCEDURE fa.usp_SaveAssetSurvey
 @SurveyId bigint,@SurveyorUserId int,@SurveyDate date,@DepartmentId int,@CustodianUserId int=NULL,@CustodianName nvarchar(250)=NULL,@AssetName nvarchar(250),@CategoryId int,@Brand nvarchar(120)=NULL,@ModelDescription nvarchar(300)=NULL,@SerialNumber nvarchar(150)=NULL,@OwnershipType nvarchar(50)=NULL,@ConditionId int,@WidthCm decimal(18,2)=NULL,@LengthCm decimal(18,2)=NULL,@HeightCm decimal(18,2)=NULL,@WeightKg decimal(18,2)=NULL,@Quantity decimal(18,2),@UomId int,@BuildingId int,@FloorId int,@RoomId int,@ReceivedDate date=NULL,@PurchaseOrderNo nvarchar(100)=NULL,@EstimatedValue decimal(18,2)=NULL,@MissingDimensionReason nvarchar(500)=NULL,@Remark nvarchar(1000)=NULL,@SavedSurveyId bigint OUTPUT
AS
BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
 SET @CustodianName=NULLIF(LTRIM(RTRIM(@CustodianName)),N'');
 IF @SurveyId=0 BEGIN DECLARE @n bigint=NEXT VALUE FOR fa.SurveyNoSequence;DECLARE @no nvarchar(50)=N'SUR-'+CONVERT(nvarchar(4),YEAR(@SurveyDate))+N'-'+RIGHT(REPLICATE('0',6)+CONVERT(nvarchar(20),@n),6);
  INSERT fa.AssetSurveys(SurveyNo,SurveyDate,SurveyorUserId,DepartmentId,CustodianUserId,CustodianName,AssetName,CategoryId,Brand,ModelDescription,SerialNumber,OwnershipType,ConditionId,WidthCm,LengthCm,HeightCm,WeightKg,Quantity,UomId,BuildingId,FloorId,RoomId,ReceivedDate,PurchaseOrderNo,EstimatedValue,MissingDimensionReason,Remark)
  VALUES(@no,@SurveyDate,@SurveyorUserId,@DepartmentId,@CustodianUserId,@CustodianName,@AssetName,@CategoryId,@Brand,@ModelDescription,@SerialNumber,@OwnershipType,@ConditionId,@WidthCm,@LengthCm,@HeightCm,@WeightKg,@Quantity,@UomId,@BuildingId,@FloorId,@RoomId,@ReceivedDate,@PurchaseOrderNo,@EstimatedValue,@MissingDimensionReason,@Remark);SET @SavedSurveyId=SCOPE_IDENTITY();END
 ELSE BEGIN UPDATE fa.AssetSurveys SET SurveyDate=@SurveyDate,DepartmentId=@DepartmentId,CustodianUserId=@CustodianUserId,CustodianName=@CustodianName,AssetName=@AssetName,CategoryId=@CategoryId,Brand=@Brand,ModelDescription=@ModelDescription,SerialNumber=@SerialNumber,OwnershipType=@OwnershipType,ConditionId=@ConditionId,WidthCm=@WidthCm,LengthCm=@LengthCm,HeightCm=@HeightCm,WeightKg=@WeightKg,Quantity=@Quantity,UomId=@UomId,BuildingId=@BuildingId,FloorId=@FloorId,RoomId=@RoomId,ReceivedDate=@ReceivedDate,PurchaseOrderNo=@PurchaseOrderNo,EstimatedValue=@EstimatedValue,MissingDimensionReason=@MissingDimensionReason,Remark=@Remark,ModifiedUtc=SYSUTCDATETIME() WHERE SurveyId=@SurveyId AND Status IN('Draft','Returned');SET @SavedSurveyId=@SurveyId;END
END
GO
PRINT N'BCFixedAsset v4 database upgrade completed.';
GO
