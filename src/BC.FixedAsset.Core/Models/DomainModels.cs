using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BC.FixedAsset.Core.Models
{
    public enum SurveyStatus { Draft, Submitted, ManagerReview, FinanceReview, Returned, Approved, Registered, Cancelled }
    public enum AccountType { Local, EntraIdSso }

    public sealed class UserIdentity
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Position { get; set; }
        public int? DivisionId { get; set; }
        public int? DepartmentId { get; set; }
        public string DivisionName { get; set; }
        public string DepartmentName { get; set; }
        public string ProfileImagePath { get; set; }
        public AccountType AccountType { get; set; }
        public bool MustChangePassword { get; set; }
        public bool IsActive { get; set; }
        public IReadOnlyCollection<string> Roles { get; set; }
    }

    public sealed class UserAdministrationModel
    {
        public UserIdentity User { get; set; } = new UserIdentity();
        public int? FixedAssetRoleId { get; set; }
        public int? AdministrationRoleId { get; set; }
        public DateTime? PasswordExpiresUtc { get; set; }
    }

    public sealed class ApplicationDefinition
    {
        public int ApplicationId { get; set; }
        [Required, StringLength(50)] public string ApplicationCode { get; set; }
        [Required, StringLength(150)] public string NameTh { get; set; }
        [Required, StringLength(150)] public string NameEn { get; set; }
        [StringLength(500)] public string DescriptionTh { get; set; }
        [StringLength(500)] public string DescriptionEn { get; set; }
        [StringLength(20)] public string IconText { get; set; }
        [StringLength(500)] public string IconPath { get; set; }
        [Required, StringLength(500)] public string TargetUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class AssetSurvey
    {
        public long SurveyId { get; set; }
        public string SurveyNo { get; set; }
        public DateTime SurveyDate { get; set; }
        public int SurveyorUserId { get; set; }
        public int DepartmentId { get; set; }
        public int CustodianUserId { get; set; }
        public string CustodianName { get; set; }
        public string AssetName { get; set; }
        public int CategoryId { get; set; }
        public string Brand { get; set; }
        public string ModelDescription { get; set; }
        public string SerialNumber { get; set; }
        public string OwnershipType { get; set; }
        public int ConditionId { get; set; }
        public decimal? WidthCm { get; set; }
        public decimal? LengthCm { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? WeightKg { get; set; }
        public decimal Quantity { get; set; }
        public int UomId { get; set; }
        public int BuildingId { get; set; }
        public int FloorId { get; set; }
        public int RoomId { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string PurchaseOrderNo { get; set; }
        public decimal? EstimatedValue { get; set; }
        public string PurchaseEvidenceRef { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string MissingDimensionReason { get; set; }
        public string Remark { get; set; }
        public SurveyStatus Status { get; set; }
        public string FixedAssetNo { get; set; }
        public string ReturnReason { get; set; }
        public string ReturnedFromStatus { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime ModifiedUtc { get; set; }
        public byte[] RowVersion { get; set; }
    }

    public sealed class RegisteredAssetEdit
    {
        public long FixedAssetId { get; set; }
        public string AssetName { get; set; }
        public int CategoryId { get; set; }
        public string Brand { get; set; }
        public string ModelDescription { get; set; }
        public string SerialNumber { get; set; }
        public int DepartmentId { get; set; }
        public string CustodianName { get; set; }
        public int? BuildingId { get; set; }
        public int? FloorId { get; set; }
        public int? RoomId { get; set; }
        public decimal Quantity { get; set; }
        public int UomId { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string PurchaseOrderNo { get; set; }
        public decimal? AcquisitionCost { get; set; }
        public string AssetStatus { get; set; }
        public byte[] RowVersion { get; set; }
    }

    public sealed class SurveyAttachment
    {
        public long AttachmentId { get; set; }
        public long SurveyId { get; set; }
        public string AttachmentType { get; set; }
        public string OriginalFileName { get; set; }
        public string StoredFileName { get; set; }
        public string RelativePath { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public byte[] FileContent { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTime UploadedUtc { get; set; }
    }

    public sealed class SurveyAccessContext
    {
        public int UserId { get; set; }
        public bool IsSystemAdministrator { get; set; }
        public string RoleCode { get; set; }
    }

    public sealed class DashboardSummary
    {
        public int TotalAssets { get; set; }
        public int DraftSurveys { get; set; }
        public int PendingManager { get; set; }
        public int PendingFinance { get; set; }
        public decimal TotalEstimatedValue { get; set; }
    }

    public sealed class AssetNumberScheme
    {
        public int SchemeId { get; set; }
        public int CompanyId { get; set; }
        public string SchemeName { get; set; }
        public string Pattern { get; set; }
        public int SequenceDigits { get; set; }
        public string SequenceScope { get; set; }
        public string ResetPolicy { get; set; }
        public int StartValue { get; set; }
        public DateTime EffectiveDate { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class AuthenticationResult
    {
        public bool Succeeded { get; set; }
        public string ErrorMessage { get; set; }
        public UserIdentity User { get; set; }
    }
}
