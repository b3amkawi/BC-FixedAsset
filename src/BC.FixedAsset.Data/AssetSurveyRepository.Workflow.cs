using BC.FixedAsset.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace BC.FixedAsset.Data
{
    public sealed partial class AssetSurveyRepository
    {
        public AssetSurvey Get(long id, SurveyAccessContext access)
        {
            const string sql = @"SELECT s.* FROM fa.AssetSurveys s WHERE s.SurveyId=@Id AND
              (@Admin=1 OR s.SurveyorUserId=@UserId OR (@Role IN('ASSET_MANAGER','DEPT_MANAGER') AND s.Status='ManagerReview') OR (@Role='FINANCE' AND s.Status='FinanceReview'));";
            using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){Access(cmd,id,access);using(var r=cmd.ExecuteReader())return r.Read()?Map(r):null;}
        }

        public DataTable Detail(long id, SurveyAccessContext access)
        {
            const string sql=@"SELECT s.*,d.DepartmentName,cat.CategoryName,con.ConditionName,m.UomCode,b.BuildingName,fl.FloorName,rm.RoomName,
              COALESCE(NULLIF(s.CustodianName,''),cu.DisplayName) Custodian,cr.DisplayName CreatedByName FROM fa.AssetSurveys s JOIN mst.Departments d ON d.DepartmentId=s.DepartmentId
              JOIN mst.AssetCategories cat ON cat.CategoryId=s.CategoryId JOIN mst.AssetConditions con ON con.ConditionId=s.ConditionId
              JOIN mst.Uoms m ON m.UomId=s.UomId JOIN mst.Buildings b ON b.BuildingId=s.BuildingId JOIN mst.Floors fl ON fl.FloorId=s.FloorId
              JOIN mst.Rooms rm ON rm.RoomId=s.RoomId LEFT JOIN sec.Users cu ON cu.UserId=s.CustodianUserId JOIN sec.Users cr ON cr.UserId=s.SurveyorUserId
              WHERE s.SurveyId=@Id AND (@Admin=1 OR s.SurveyorUserId=@UserId OR (@Role IN('ASSET_MANAGER','DEPT_MANAGER') AND s.Status='ManagerReview') OR (@Role='FINANCE' AND s.Status='FinanceReview'));";
            using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){Access(cmd,id,access);var t=new DataTable();using(var a=new SqlDataAdapter(cmd))a.Fill(t);return t;}
        }

        public IList<SurveyAttachment> Attachments(long id, SurveyAccessContext access)
        {
            const string sql=@"SELECT a.AttachmentId,a.SurveyId,a.AttachmentType,a.OriginalFileName,a.StoredFileName,a.RelativePath,a.ContentType,a.FileSizeBytes,CAST(NULL AS varbinary(max)) FileContent,a.UploadedByUserId,a.UploadedUtc FROM fa.AssetSurveyAttachments a JOIN fa.AssetSurveys s ON s.SurveyId=a.SurveyId WHERE a.SurveyId=@Id AND
              (@Admin=1 OR s.SurveyorUserId=@UserId OR (@Role IN('ASSET_MANAGER','DEPT_MANAGER') AND s.Status='ManagerReview') OR (@Role='FINANCE' AND s.Status='FinanceReview') OR (s.Status='Registered' AND @Role<>''));";
            var result=new List<SurveyAttachment>();using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){Access(cmd,id,access);using(var r=cmd.ExecuteReader())while(r.Read())result.Add(MapAttachment(r));}return result;
        }

        public SurveyAttachment Attachment(long attachmentId, SurveyAccessContext access)
        {
            const string sql=@"SELECT a.* FROM fa.AssetSurveyAttachments a JOIN fa.AssetSurveys s ON s.SurveyId=a.SurveyId WHERE a.AttachmentId=@AttachmentId AND
              (@Admin=1 OR s.SurveyorUserId=@UserId OR (@Role IN('ASSET_MANAGER','DEPT_MANAGER') AND s.Status='ManagerReview') OR (@Role='FINANCE' AND s.Status='FinanceReview') OR (s.Status='Registered' AND @Role<>''));";
            using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){cmd.Parameters.Add(Db.Parameter("@AttachmentId",attachmentId,SqlDbType.BigInt));AccessOnly(cmd,access);using(var r=cmd.ExecuteReader())return r.Read()?MapAttachment(r):null;}
        }

        public bool CanEdit(long id,SurveyAccessContext access)
        {
            const string sql="SELECT CASE WHEN EXISTS(SELECT 1 FROM fa.AssetSurveys WHERE SurveyId=@Id AND Status IN('Draft','Returned') AND (@Admin=1 OR SurveyorUserId=@UserId)) THEN 1 ELSE 0 END";
            using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){Access(cmd,id,access);return Convert.ToBoolean(cmd.ExecuteScalar());}
        }

        public string SaveAttachment(long id,string type,string original,string contentType,long size,byte[] content,SurveyAccessContext access)
        {
            if(!CanEdit(id,access))throw new InvalidOperationException("Only the owner can update a Draft or Returned survey.");
            const string sql=@"DECLARE @Old nvarchar(500);SELECT @Old=RelativePath FROM fa.AssetSurveyAttachments WHERE SurveyId=@Id AND AttachmentType=@Type;
              MERGE fa.AssetSurveyAttachments t USING(SELECT @Id SurveyId,@Type AttachmentType)s ON t.SurveyId=s.SurveyId AND t.AttachmentType=s.AttachmentType
              WHEN MATCHED THEN UPDATE SET OriginalFileName=@Original,StoredFileName='',RelativePath='',ContentType=@ContentType,FileSizeBytes=@Size,FileContent=@Content,UploadedByUserId=@UserId,UploadedUtc=SYSUTCDATETIME()
              WHEN NOT MATCHED THEN INSERT(SurveyId,AttachmentType,OriginalFileName,StoredFileName,RelativePath,ContentType,FileSizeBytes,FileContent,UploadedByUserId) VALUES(@Id,@Type,@Original,'','',@ContentType,@Size,@Content,@UserId);SELECT @Old;";
            using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));cmd.Parameters.Add(Db.Parameter("@Type",type,SqlDbType.NVarChar,50));cmd.Parameters.Add(Db.Parameter("@Original",original,SqlDbType.NVarChar,260));cmd.Parameters.Add(Db.Parameter("@ContentType",contentType,SqlDbType.NVarChar,100));cmd.Parameters.Add(Db.Parameter("@Size",size,SqlDbType.BigInt));cmd.Parameters.Add(new SqlParameter("@Content",SqlDbType.VarBinary,-1){Value=content});cmd.Parameters.Add(Db.Parameter("@UserId",access.UserId,SqlDbType.Int));return Convert.ToString(cmd.ExecuteScalar());}
        }

        public IList<string> Delete(long id,SurveyAccessContext access)
        {
            if(!CanEdit(id,access))throw new InvalidOperationException("This survey cannot be deleted.");var files=new List<string>();
            using(var c=Db.OpenConnection())using(var tx=c.BeginTransaction()){try{using(var cmd=new SqlCommand("SELECT RelativePath FROM fa.AssetSurveyAttachments WHERE SurveyId=@Id",c,tx)){cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));using(var r=cmd.ExecuteReader())while(r.Read())files.Add(Convert.ToString(r[0]));}using(var cmd=new SqlCommand("DELETE fa.ApprovalTasks WHERE SurveyId=@Id;DELETE fa.AssetSurveyAttachments WHERE SurveyId=@Id;DELETE fa.AssetSurveys WHERE SurveyId=@Id",c,tx)){cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));cmd.ExecuteNonQuery();}Audit(c,tx,access.UserId,"DELETE",id,null);tx.Commit();return files;}catch{tx.Rollback();throw;}}
        }

        public string Advance(long id,SurveyAccessContext access)
        {
            using(var c=Db.OpenConnection())using(var tx=c.BeginTransaction()){try{string status;int owner,category;DateTime date;using(var cmd=new SqlCommand("SELECT Status,SurveyorUserId,CategoryId,SurveyDate FROM fa.AssetSurveys WITH(UPDLOCK) WHERE SurveyId=@Id",c,tx)){cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));using(var r=cmd.ExecuteReader()){if(!r.Read())throw new InvalidOperationException("Survey not found.");status=Convert.ToString(r[0]);owner=Convert.ToInt32(r[1]);category=Convert.ToInt32(r[2]);date=Convert.ToDateTime(r[3]);}}
                string next;if((status=="Draft"||status=="Returned")&&(owner==access.UserId||access.IsSystemAdministrator))next="ManagerReview";else if(status=="ManagerReview"&&(access.IsSystemAdministrator||access.RoleCode=="ASSET_MANAGER"||access.RoleCode=="DEPT_MANAGER"))next="FinanceReview";else if(status=="FinanceReview"&&(access.IsSystemAdministrator||access.RoleCode=="FINANCE"))next="Registered";else throw new InvalidOperationException("You cannot process this workflow stage.");
                string number=null;if(next=="Registered"){using(var cmd=new SqlCommand("fa.usp_GenerateFixedAssetNumber",c,tx)){cmd.CommandType=CommandType.StoredProcedure;cmd.Parameters.Add(Db.Parameter("@CompanyId",1,SqlDbType.Int));cmd.Parameters.Add(Db.Parameter("@CategoryId",category,SqlDbType.Int));cmd.Parameters.Add(Db.Parameter("@EffectiveDate",date,SqlDbType.Date));cmd.Parameters.Add(Db.Parameter("@RequestedByUserId",access.UserId,SqlDbType.Int));var p=new SqlParameter("@AssetNumber",SqlDbType.NVarChar,80){Direction=ParameterDirection.Output};cmd.Parameters.Add(p);cmd.ExecuteNonQuery();number=Convert.ToString(p.Value);}using(var cmd=new SqlCommand(@"INSERT fa.FixedAssets(FixedAssetNo,SourceSurveyId,AssetName,CategoryId,Brand,ModelDescription,SerialNumber,DepartmentId,CustodianUserId,CustodianName,BuildingId,FloorId,RoomId,Quantity,UomId,ReceivedDate,PurchaseOrderNo,AcquisitionCost) SELECT @No,SurveyId,AssetName,CategoryId,Brand,ModelDescription,SerialNumber,DepartmentId,CustodianUserId,CustodianName,BuildingId,FloorId,RoomId,Quantity,UomId,ReceivedDate,PurchaseOrderNo,EstimatedValue FROM fa.AssetSurveys WHERE SurveyId=@Id",c,tx)){cmd.Parameters.Add(Db.Parameter("@No",number,SqlDbType.NVarChar,80));cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));cmd.ExecuteNonQuery();}}
                using(var cmd=new SqlCommand(@"UPDATE fa.AssetSurveys SET Status=@Next,FixedAssetNo=COALESCE(@No,FixedAssetNo),ReturnReason=NULL,ReturnedFromStatus=NULL,ModifiedUtc=SYSUTCDATETIME() WHERE SurveyId=@Id;UPDATE fa.ApprovalTasks SET Status='Completed',Decision='Approved',CompletedUtc=SYSUTCDATETIME(),CompletedByUserId=@UserId WHERE SurveyId=@Id AND Status='Pending';IF @Next IN('ManagerReview','FinanceReview') INSERT fa.ApprovalTasks(SurveyId,Stage,Status)VALUES(@Id,@Next,'Pending')",c,tx)){cmd.Parameters.Add(Db.Parameter("@Next",next,SqlDbType.NVarChar,30));cmd.Parameters.Add(Db.Parameter("@No",number,SqlDbType.NVarChar,80));cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));cmd.Parameters.Add(Db.Parameter("@UserId",access.UserId,SqlDbType.Int));cmd.ExecuteNonQuery();}Audit(c,tx,access.UserId,"ADVANCE",id,next);tx.Commit();return next;}catch{tx.Rollback();throw;}}
        }

        public void Return(long id,string reason,SurveyAccessContext access)
        {
            if(string.IsNullOrWhiteSpace(reason))throw new ArgumentException("Return reason is required.");using(var c=Db.OpenConnection())using(var tx=c.BeginTransaction()){try{string status;using(var cmd=new SqlCommand("SELECT Status FROM fa.AssetSurveys WITH(UPDLOCK) WHERE SurveyId=@Id",c,tx)){cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));status=Convert.ToString(cmd.ExecuteScalar());}if(!((status=="ManagerReview"&&(access.IsSystemAdministrator||access.RoleCode=="ASSET_MANAGER"||access.RoleCode=="DEPT_MANAGER"))||(status=="FinanceReview"&&(access.IsSystemAdministrator||access.RoleCode=="FINANCE"))))throw new InvalidOperationException("You cannot return this stage.");using(var cmd=new SqlCommand("UPDATE fa.AssetSurveys SET Status='Returned',ReturnedFromStatus=@Status,ReturnReason=@Reason,ModifiedUtc=SYSUTCDATETIME() WHERE SurveyId=@Id;UPDATE fa.ApprovalTasks SET Status='Completed',Decision='Returned',Comment=@Reason,CompletedUtc=SYSUTCDATETIME(),CompletedByUserId=@UserId WHERE SurveyId=@Id AND Status='Pending'",c,tx)){cmd.Parameters.Add(Db.Parameter("@Status",status,SqlDbType.NVarChar,30));cmd.Parameters.Add(Db.Parameter("@Reason",reason.Trim(),SqlDbType.NVarChar,1000));cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));cmd.Parameters.Add(Db.Parameter("@UserId",access.UserId,SqlDbType.Int));cmd.ExecuteNonQuery();}Audit(c,tx,access.UserId,"RETURN",id,reason);tx.Commit();}catch{tx.Rollback();throw;}}
        }

        private static void Access(SqlCommand cmd,long id,SurveyAccessContext a){cmd.Parameters.Add(Db.Parameter("@Id",id,SqlDbType.BigInt));AccessOnly(cmd,a);}
        private static void AccessOnly(SqlCommand cmd,SurveyAccessContext a){cmd.Parameters.Add(Db.Parameter("@UserId",a.UserId,SqlDbType.Int));cmd.Parameters.Add(Db.Parameter("@Admin",a.IsSystemAdministrator,SqlDbType.Bit));cmd.Parameters.Add(Db.Parameter("@Role",a.RoleCode??"",SqlDbType.NVarChar,50));}
        private static void Audit(SqlConnection c,SqlTransaction tx,int user,string action,long id,string value){using(var cmd=new SqlCommand("INSERT audit.AuditLogs(UserId,ApplicationCode,ModuleCode,ActionCode,EntityName,EntityId,NewValues)VALUES(@U,'FIXED_ASSET','SURVEY',@A,'AssetSurvey',@I,@V)",c,tx)){cmd.Parameters.Add(Db.Parameter("@U",user,SqlDbType.Int));cmd.Parameters.Add(Db.Parameter("@A",action,SqlDbType.NVarChar,80));cmd.Parameters.Add(Db.Parameter("@I",id.ToString(),SqlDbType.NVarChar,100));cmd.Parameters.Add(Db.Parameter("@V",value,SqlDbType.NVarChar));cmd.ExecuteNonQuery();}}
        private static SurveyAttachment MapAttachment(IDataRecord r){return new SurveyAttachment{AttachmentId=Convert.ToInt64(r["AttachmentId"]),SurveyId=Convert.ToInt64(r["SurveyId"]),AttachmentType=Convert.ToString(r["AttachmentType"]),OriginalFileName=Convert.ToString(r["OriginalFileName"]),StoredFileName=Convert.ToString(r["StoredFileName"]),RelativePath=Convert.ToString(r["RelativePath"]),ContentType=Convert.ToString(r["ContentType"]),FileSizeBytes=Convert.ToInt64(r["FileSizeBytes"]),FileContent=r["FileContent"]==DBNull.Value?null:(byte[])r["FileContent"],UploadedByUserId=Convert.ToInt32(r["UploadedByUserId"]),UploadedUtc=Convert.ToDateTime(r["UploadedUtc"])};}
        private static AssetSurvey Map(IDataRecord r){return new AssetSurvey{SurveyId=Convert.ToInt64(r["SurveyId"]),SurveyNo=Convert.ToString(r["SurveyNo"]),FixedAssetNo=Convert.ToString(r["FixedAssetNo"]),SurveyDate=Convert.ToDateTime(r["SurveyDate"]),SurveyorUserId=Convert.ToInt32(r["SurveyorUserId"]),DepartmentId=Convert.ToInt32(r["DepartmentId"]),CustodianUserId=r["CustodianUserId"]==DBNull.Value?0:Convert.ToInt32(r["CustodianUserId"]),CustodianName=Convert.ToString(r["CustodianName"]),AssetName=Convert.ToString(r["AssetName"]),CategoryId=Convert.ToInt32(r["CategoryId"]),Brand=Convert.ToString(r["Brand"]),ModelDescription=Convert.ToString(r["ModelDescription"]),SerialNumber=Convert.ToString(r["SerialNumber"]),OwnershipType=Convert.ToString(r["OwnershipType"]),ConditionId=Convert.ToInt32(r["ConditionId"]),WidthCm=N<decimal>(r,"WidthCm"),LengthCm=N<decimal>(r,"LengthCm"),HeightCm=N<decimal>(r,"HeightCm"),WeightKg=N<decimal>(r,"WeightKg"),Quantity=Convert.ToDecimal(r["Quantity"]),UomId=Convert.ToInt32(r["UomId"]),BuildingId=Convert.ToInt32(r["BuildingId"]),FloorId=Convert.ToInt32(r["FloorId"]),RoomId=Convert.ToInt32(r["RoomId"]),ReceivedDate=N<DateTime>(r,"ReceivedDate"),PurchaseOrderNo=Convert.ToString(r["PurchaseOrderNo"]),EstimatedValue=N<decimal>(r,"EstimatedValue"),MissingDimensionReason=Convert.ToString(r["MissingDimensionReason"]),Remark=Convert.ToString(r["Remark"]),Status=(SurveyStatus)Enum.Parse(typeof(SurveyStatus),Convert.ToString(r["Status"])),ReturnReason=Convert.ToString(r["ReturnReason"]),ReturnedFromStatus=Convert.ToString(r["ReturnedFromStatus"]),CreatedUtc=Convert.ToDateTime(r["CreatedUtc"]),ModifiedUtc=Convert.ToDateTime(r["ModifiedUtc"])};}
        private static T? N<T>(IDataRecord r,string n)where T:struct{return r[n]==DBNull.Value?(T?)null:(T)Convert.ChangeType(r[n],typeof(T));}
    }
}
