using BC.FixedAsset.Core.Models;using System;using System.Collections.Generic;using System.Data;using System.Data.SqlClient;
namespace BC.FixedAsset.Data
{
 public sealed class FixedAssetRepository
 {
  public AssetNumberScheme GetNumberScheme(){const string sql=@"SELECT TOP(1) SchemeId,CompanyId,SchemeName,Pattern,SequenceDigits,SequenceScope,ResetPolicy,StartValue,EffectiveDate,IsActive FROM fa.AssetNumberSchemes WHERE IsActive=1 ORDER BY EffectiveDate DESC,SchemeId DESC;";using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c))using(var r=cmd.ExecuteReader()){if(!r.Read())return null;return new AssetNumberScheme{SchemeId=Convert.ToInt32(r[0]),CompanyId=Convert.ToInt32(r[1]),SchemeName=Convert.ToString(r[2]),Pattern=Convert.ToString(r[3]),SequenceDigits=Convert.ToInt32(r[4]),SequenceScope=Convert.ToString(r[5]),ResetPolicy=Convert.ToString(r[6]),StartValue=Convert.ToInt32(r[7]),EffectiveDate=Convert.ToDateTime(r[8]),IsActive=Convert.ToBoolean(r[9])};}}
  public void SaveNumberScheme(AssetNumberScheme scheme,int userId){const string sql=@"UPDATE fa.AssetNumberSchemes SET Pattern=@Pattern,SequenceDigits=@Digits,SequenceScope=@Scope,ResetPolicy=@Reset,StartValue=@StartValue,ModifiedByUserId=@UserId,ModifiedUtc=SYSUTCDATETIME() WHERE SchemeId=@SchemeId AND IsActive=1;";using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){Add(cmd,"@Pattern",scheme.Pattern,SqlDbType.NVarChar,200);Add(cmd,"@Digits",scheme.SequenceDigits,SqlDbType.Int);Add(cmd,"@Scope",scheme.SequenceScope,SqlDbType.NVarChar,50);Add(cmd,"@Reset",scheme.ResetPolicy,SqlDbType.NVarChar,20);Add(cmd,"@StartValue",scheme.StartValue,SqlDbType.Int);Add(cmd,"@UserId",userId,SqlDbType.Int);Add(cmd,"@SchemeId",scheme.SchemeId,SqlDbType.Int);if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Active numbering scheme was not found.");}}
  public DashboardSummary GetDashboard(){const string sql=@"SELECT (SELECT COUNT(*) FROM fa.FixedAssets WHERE IsActive=1) TotalAssets,(SELECT COUNT(*) FROM fa.AssetSurveys WHERE Status='Draft') DraftSurveys,(SELECT COUNT(*) FROM fa.AssetSurveys WHERE Status='ManagerReview') PendingManager,(SELECT COUNT(*) FROM fa.AssetSurveys WHERE Status='FinanceReview') PendingFinance,(SELECT ISNULL(SUM(EstimatedValue),0) FROM fa.AssetSurveys) TotalEstimatedValue;";using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c))using(var r=cmd.ExecuteReader()){r.Read();return new DashboardSummary{TotalAssets=Convert.ToInt32(r[0]),DraftSurveys=Convert.ToInt32(r[1]),PendingManager=Convert.ToInt32(r[2]),PendingFinance=Convert.ToInt32(r[3]),TotalEstimatedValue=Convert.ToDecimal(r[4])};}}
  public DataTable GetReference(string type){string sql;switch(type){case "Department":sql="SELECT DepartmentId Id,DepartmentName Name,DepartmentCode Code FROM mst.Departments WHERE IsActive=1 ORDER BY DepartmentName";break;case "Category":sql="SELECT CategoryId Id,CategoryName Name,CategoryCode Code FROM mst.AssetCategories WHERE IsActive=1 ORDER BY CategoryName";break;case "Condition":sql="SELECT ConditionId Id,ConditionName Name,ConditionCode Code FROM mst.AssetConditions WHERE IsActive=1 ORDER BY DisplayOrder";break;case "Uom":sql="SELECT UomId Id,UomCode Name,UomCode Code FROM mst.Uoms WHERE IsActive=1 ORDER BY UomCode";break;case "User":sql="SELECT UserId Id,DisplayName Name FROM sec.Users WHERE IsActive=1 ORDER BY DisplayName";break;case "Building":sql="SELECT BuildingId Id,BuildingName Name,BuildingCode Code FROM mst.Buildings WHERE IsActive=1 ORDER BY BuildingName";break;case "Floor":sql="SELECT FloorId Id,FloorName Name,FloorCode Code,BuildingId ParentId FROM mst.Floors WHERE IsActive=1 ORDER BY DisplayOrder,FloorName";break;case "Room":sql="SELECT RoomId Id,RoomName Name,RoomCode Code,FloorId ParentId FROM mst.Rooms WHERE IsActive=1 ORDER BY RoomName";break;default:throw new ArgumentOutOfRangeException(nameof(type));}using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){var t=new DataTable();using(var a=new SqlDataAdapter(cmd))a.Fill(t);return t;}}
  public DataTable GetRegister(string query){const string sql=@"SELECT TOP(500) f.FixedAssetId,f.SourceSurveyId,f.FixedAssetNo,f.AssetName,f.Brand,f.ModelDescription,f.SerialNumber,c.CategoryName,d.DepartmentName,COALESCE(NULLIF(f.CustodianName,''),u.DisplayName) Custodian,b.BuildingName,fl.FloorName,r.RoomName,f.Quantity,m.UomCode,f.AssetStatus,f.AcquisitionCost,f.ReceivedDate,f.PurchaseOrderNo,(SELECT TOP(1) a.AttachmentId FROM fa.AssetSurveyAttachments a WHERE a.SurveyId=f.SourceSurveyId AND a.AttachmentType='ACTUAL') ActualAttachmentId FROM fa.FixedAssets f INNER JOIN mst.AssetCategories c ON c.CategoryId=f.CategoryId INNER JOIN mst.Departments d ON d.DepartmentId=f.DepartmentId LEFT JOIN sec.Users u ON u.UserId=f.CustodianUserId LEFT JOIN mst.Buildings b ON b.BuildingId=f.BuildingId LEFT JOIN mst.Floors fl ON fl.FloorId=f.FloorId LEFT JOIN mst.Rooms r ON r.RoomId=f.RoomId LEFT JOIN mst.Uoms m ON m.UomId=f.UomId WHERE (@Query='' OR f.FixedAssetNo LIKE '%'+@Query+'%' OR f.AssetName LIKE '%'+@Query+'%' OR f.SerialNumber LIKE '%'+@Query+'%' OR ISNULL(f.CustodianName,'') LIKE '%'+@Query+'%') ORDER BY f.FixedAssetNo;";using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){cmd.Parameters.Add(Db.Parameter("@Query",query??string.Empty,SqlDbType.NVarChar,150));var t=new DataTable();using(var a=new SqlDataAdapter(cmd))a.Fill(t);return t;}}
  public DataTable GetRegisterDetail(long id){const string sql=@"SELECT f.FixedAssetId,f.SourceSurveyId,f.FixedAssetNo,f.AssetName,f.Brand,f.ModelDescription,f.SerialNumber,c.CategoryName,d.DepartmentName,COALESCE(NULLIF(f.CustodianName,''),u.DisplayName) Custodian,b.BuildingName,fl.FloorName,r.RoomName,f.Quantity,m.UomCode,f.AssetStatus,f.AcquisitionCost,f.ReceivedDate,f.PurchaseOrderNo FROM fa.FixedAssets f INNER JOIN mst.AssetCategories c ON c.CategoryId=f.CategoryId INNER JOIN mst.Departments d ON d.DepartmentId=f.DepartmentId LEFT JOIN sec.Users u ON u.UserId=f.CustodianUserId LEFT JOIN mst.Buildings b ON b.BuildingId=f.BuildingId LEFT JOIN mst.Floors fl ON fl.FloorId=f.FloorId LEFT JOIN mst.Rooms r ON r.RoomId=f.RoomId LEFT JOIN mst.Uoms m ON m.UomId=f.UomId WHERE f.FixedAssetId=@Id;";using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));var t=new DataTable();using(var a=new SqlDataAdapter(cmd))a.Fill(t);return t;}}
  public RegisteredAssetEdit GetRegisterForEdit(long id)
  {
   const string sql=@"SELECT FixedAssetId,AssetName,CategoryId,Brand,ModelDescription,SerialNumber,DepartmentId,CustodianName,BuildingId,FloorId,RoomId,Quantity,UomId,ReceivedDate,PurchaseOrderNo,AcquisitionCost,AssetStatus,RowVersion FROM fa.FixedAssets WHERE FixedAssetId=@Id;";
   using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c))
   {
    cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));
    using(var r=cmd.ExecuteReader())
    {
     if(!r.Read())return null;
     return new RegisteredAssetEdit{FixedAssetId=id,AssetName=Convert.ToString(r["AssetName"]),CategoryId=Convert.ToInt32(r["CategoryId"]),Brand=Convert.ToString(r["Brand"]),ModelDescription=Convert.ToString(r["ModelDescription"]),SerialNumber=Convert.ToString(r["SerialNumber"]),DepartmentId=Convert.ToInt32(r["DepartmentId"]),CustodianName=Convert.ToString(r["CustodianName"]),BuildingId=NullableInt(r["BuildingId"]),FloorId=NullableInt(r["FloorId"]),RoomId=NullableInt(r["RoomId"]),Quantity=Convert.ToDecimal(r["Quantity"]),UomId=Convert.ToInt32(r["UomId"]),ReceivedDate=r["ReceivedDate"]==DBNull.Value?(DateTime?)null:Convert.ToDateTime(r["ReceivedDate"]),PurchaseOrderNo=Convert.ToString(r["PurchaseOrderNo"]),AcquisitionCost=r["AcquisitionCost"]==DBNull.Value?(decimal?)null:Convert.ToDecimal(r["AcquisitionCost"]),AssetStatus=Convert.ToString(r["AssetStatus"]),RowVersion=(byte[])r["RowVersion"]};
    }
   }
  }
  public void UpdateRegister(RegisteredAssetEdit asset,int userId)
  {
   const string sql=@"UPDATE f SET AssetName=@AssetName,CategoryId=@CategoryId,Brand=@Brand,ModelDescription=@ModelDescription,SerialNumber=@SerialNumber,DepartmentId=@DepartmentId,CustodianUserId=CASE WHEN ISNULL(f.CustodianName,N'')=@CustodianName THEN f.CustodianUserId ELSE NULL END,CustodianName=@CustodianName,BuildingId=@BuildingId,FloorId=@FloorId,RoomId=@RoomId,Quantity=@Quantity,UomId=@UomId,ReceivedDate=@ReceivedDate,PurchaseOrderNo=@PurchaseOrderNo,AcquisitionCost=@AcquisitionCost,AssetStatus=@AssetStatus,ModifiedUtc=SYSUTCDATETIME()
    FROM fa.FixedAssets f WHERE f.FixedAssetId=@Id AND f.RowVersion=@RowVersion
    AND (@FloorId IS NULL OR EXISTS(SELECT 1 FROM mst.Floors fl WHERE fl.FloorId=@FloorId AND fl.BuildingId=@BuildingId))
    AND (@RoomId IS NULL OR EXISTS(SELECT 1 FROM mst.Rooms rm WHERE rm.RoomId=@RoomId AND rm.FloorId=@FloorId))
    AND EXISTS(SELECT 1 FROM sec.UserApplicationRoles ur JOIN sec.Roles r ON r.RoleId=ur.RoleId JOIN sec.Applications a ON a.ApplicationId=ur.ApplicationId WHERE ur.UserId=@UserId AND ur.IsActive=1 AND r.IsActive=1 AND r.RoleCode='SYSTEM_ADMIN' AND a.ApplicationCode='ADMIN' AND a.IsActive=1);";
   using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c))
   {
    Add(cmd,"@Id",asset.FixedAssetId,SqlDbType.BigInt);Add(cmd,"@UserId",userId,SqlDbType.Int);Add(cmd,"@RowVersion",asset.RowVersion,SqlDbType.Binary,8);Add(cmd,"@AssetName",asset.AssetName,SqlDbType.NVarChar,250);Add(cmd,"@CategoryId",asset.CategoryId,SqlDbType.Int);Add(cmd,"@Brand",asset.Brand,SqlDbType.NVarChar,120);Add(cmd,"@ModelDescription",asset.ModelDescription,SqlDbType.NVarChar,300);Add(cmd,"@SerialNumber",asset.SerialNumber,SqlDbType.NVarChar,150);Add(cmd,"@DepartmentId",asset.DepartmentId,SqlDbType.Int);Add(cmd,"@CustodianName",asset.CustodianName,SqlDbType.NVarChar,250);Add(cmd,"@BuildingId",asset.BuildingId,SqlDbType.Int);Add(cmd,"@FloorId",asset.FloorId,SqlDbType.Int);Add(cmd,"@RoomId",asset.RoomId,SqlDbType.Int);Add(cmd,"@Quantity",asset.Quantity,SqlDbType.Decimal);Add(cmd,"@UomId",asset.UomId,SqlDbType.Int);Add(cmd,"@ReceivedDate",asset.ReceivedDate,SqlDbType.Date);Add(cmd,"@PurchaseOrderNo",asset.PurchaseOrderNo,SqlDbType.NVarChar,100);Add(cmd,"@AcquisitionCost",asset.AcquisitionCost,SqlDbType.Decimal);Add(cmd,"@AssetStatus",asset.AssetStatus,SqlDbType.NVarChar,30);
    if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Asset was changed by another user, was not found, or you do not have permission to edit it.");
   }
  }
  private static int? NullableInt(object value){return value==DBNull.Value?(int?)null:Convert.ToInt32(value);}
  public long SaveDraft(AssetSurvey survey,SurveyAccessContext access)
  {
   if(survey.SurveyId>0&&!new AssetSurveyRepository().CanEdit(survey.SurveyId,access))throw new InvalidOperationException("Only the owner can update a Draft or Returned survey.");
   survey.SurveyorUserId=access.UserId;
   using(var connection=Db.OpenConnection())return SaveDraftCore(survey,connection,null);
  }
  public int ImportDrafts(IList<AssetSurvey> surveys,SurveyAccessContext access)
  {
   using(var connection=Db.OpenConnection())using(var transaction=connection.BeginTransaction())
   {
    try
    {
     foreach(var survey in surveys)
     {
      if(survey.SurveyId!=0)throw new InvalidOperationException("Imported surveys must be new drafts.");
      survey.SurveyorUserId=access.UserId;
      SaveDraftCore(survey,connection,transaction);
     }
     transaction.Commit();
     return surveys.Count;
    }
    catch{transaction.Rollback();throw;}
   }
  }
  private static long SaveDraftCore(AssetSurvey survey,SqlConnection connection,SqlTransaction transaction)
  {
   using(var cmd=new SqlCommand("fa.usp_SaveAssetSurvey",connection,transaction))
   {
    cmd.CommandType=CommandType.StoredProcedure;
    Add(cmd,"@SurveyId",survey.SurveyId,SqlDbType.BigInt);Add(cmd,"@SurveyorUserId",survey.SurveyorUserId,SqlDbType.Int);
    Add(cmd,"@SurveyDate",survey.SurveyDate,SqlDbType.Date);Add(cmd,"@DepartmentId",survey.DepartmentId,SqlDbType.Int);
    Add(cmd,"@CustodianUserId",survey.CustodianUserId==0?(object)null:survey.CustodianUserId,SqlDbType.Int);
    Add(cmd,"@CustodianName",survey.CustodianName,SqlDbType.NVarChar,250);Add(cmd,"@AssetName",survey.AssetName,SqlDbType.NVarChar,250);
    Add(cmd,"@CategoryId",survey.CategoryId,SqlDbType.Int);Add(cmd,"@Brand",survey.Brand,SqlDbType.NVarChar,120);
    Add(cmd,"@ModelDescription",survey.ModelDescription,SqlDbType.NVarChar,300);Add(cmd,"@SerialNumber",survey.SerialNumber,SqlDbType.NVarChar,150);
    Add(cmd,"@OwnershipType",survey.OwnershipType,SqlDbType.NVarChar,50);Add(cmd,"@ConditionId",survey.ConditionId,SqlDbType.Int);
    Add(cmd,"@WidthCm",survey.WidthCm,SqlDbType.Decimal);Add(cmd,"@LengthCm",survey.LengthCm,SqlDbType.Decimal);
    Add(cmd,"@HeightCm",survey.HeightCm,SqlDbType.Decimal);Add(cmd,"@WeightKg",survey.WeightKg,SqlDbType.Decimal);
    Add(cmd,"@Quantity",survey.Quantity,SqlDbType.Decimal);Add(cmd,"@UomId",survey.UomId,SqlDbType.Int);
    Add(cmd,"@BuildingId",survey.BuildingId,SqlDbType.Int);Add(cmd,"@FloorId",survey.FloorId,SqlDbType.Int);
    Add(cmd,"@RoomId",survey.RoomId,SqlDbType.Int);Add(cmd,"@ReceivedDate",survey.ReceivedDate,SqlDbType.Date);
    Add(cmd,"@PurchaseOrderNo",survey.PurchaseOrderNo,SqlDbType.NVarChar,100);Add(cmd,"@EstimatedValue",survey.EstimatedValue,SqlDbType.Decimal);
    Add(cmd,"@MissingDimensionReason",survey.MissingDimensionReason,SqlDbType.NVarChar,500);
    Add(cmd,"@Remark",survey.Remark,SqlDbType.NVarChar,1000);
    var output=new SqlParameter("@SavedSurveyId",SqlDbType.BigInt){Direction=ParameterDirection.Output};cmd.Parameters.Add(output);
    cmd.ExecuteNonQuery();return Convert.ToInt64(output.Value);
   }
  }
  private static void Add(SqlCommand cmd,string name,object value,SqlDbType type,int size=0){var p=Db.Parameter(name,value,type,size);if(type==SqlDbType.Decimal){p.Precision=18;p.Scale=2;}cmd.Parameters.Add(p);}
 }
}
