USE BCFixedAsset;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fa.AssetSurveyAttachments') AND name = N'IX_AssetSurveyAttachments_Survey_Type')
BEGIN
    CREATE INDEX IX_AssetSurveyAttachments_Survey_Type
        ON fa.AssetSurveyAttachments(SurveyId, AttachmentType, AttachmentId DESC)
        INCLUDE (OriginalFileName, RelativePath, ContentType, FileSizeBytes, UploadedUtc);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fa.AssetSurveys') AND name = N'IX_AssetSurveys_ModifiedUtc')
BEGIN
    CREATE INDEX IX_AssetSurveys_ModifiedUtc
        ON fa.AssetSurveys(ModifiedUtc DESC)
        INCLUDE (SurveyId, SurveyNo, Status, SurveyorUserId, DepartmentId, CustodianUserId, BuildingId, FloorId, RoomId);
END;
GO

PRINT N'Performance indexes are ready.';
GO
