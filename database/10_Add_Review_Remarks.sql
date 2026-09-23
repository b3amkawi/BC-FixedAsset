/* Add manager decision remarks as administrator-managed master data. */
USE [BCFixedAsset];
GO
SET XACT_ABORT ON;
GO
BEGIN TRAN;

IF OBJECT_ID(N'mst.ReviewRemarks', N'U') IS NULL
BEGIN
    CREATE TABLE mst.ReviewRemarks
    (
        ReviewRemarkId int IDENTITY PRIMARY KEY,
        RemarkCode nvarchar(30) NOT NULL UNIQUE,
        RemarkName nvarchar(100) NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_ReviewRemarks_IsActive DEFAULT 1
    );
END;

INSERT mst.ReviewRemarks(RemarkCode, RemarkName)
SELECT v.Code, v.Name
FROM (VALUES
    (N'SPARE_PART', N'Spare part'),
    (N'FIXED_ASSET', N'Fixed Asset')
) v(Code, Name)
WHERE NOT EXISTS
(
    SELECT 1 FROM mst.ReviewRemarks r WHERE r.RemarkCode = v.Code
);

COMMIT;
GO
