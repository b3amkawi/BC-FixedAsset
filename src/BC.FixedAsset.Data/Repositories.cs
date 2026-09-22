using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Core.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace BC.FixedAsset.Data
{
    public sealed class UserCredentialRecord
    {
        public UserIdentity User { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public int PasswordIterations { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
        public DateTime? PasswordExpiresUtc { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class UserRepository
    {
        public UserCredentialRecord FindForAuthentication(string userName)
        {
            const string sql = @"SELECT TOP (1) u.UserId,u.UserName,u.Email,u.Phone,u.FirstName,u.LastName,u.Position,
                u.DivisionId,u.DepartmentId,d.DivisionName,dp.DepartmentName,u.ProfileImagePath,u.AccountType,u.MustChangePassword,u.IsActive,
                c.PasswordHash,c.PasswordSalt,c.PasswordIterations,c.FailedLoginCount,c.LockedUntilUtc,c.PasswordExpiresUtc
                FROM sec.Users u
                LEFT JOIN mst.Divisions d ON d.DivisionId=u.DivisionId
                LEFT JOIN mst.Departments dp ON dp.DepartmentId=u.DepartmentId
                LEFT JOIN sec.UserCredentials c ON c.UserId=u.UserId
                WHERE u.UserName=@UserName;";
            using (var connection = Db.OpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(Db.Parameter("@UserName", userName, SqlDbType.NVarChar, 100));
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new UserCredentialRecord
                    {
                        User = MapUser(reader),
                        PasswordHash = reader["PasswordHash"] as byte[],
                        PasswordSalt = reader["PasswordSalt"] as byte[],
                        PasswordIterations = reader["PasswordIterations"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PasswordIterations"]),
                        FailedLoginCount = reader["FailedLoginCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["FailedLoginCount"]),
                        LockedUntilUtc = reader["LockedUntilUtc"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LockedUntilUtc"]),
                        PasswordExpiresUtc = reader["PasswordExpiresUtc"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["PasswordExpiresUtc"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"])
                    };
                }
            }
        }

        public PasswordPolicy GetActivePasswordPolicy()
        {
            const string sql = @"SELECT TOP(1) MinimumLength,RequireUppercase,RequireLowercase,RequireNumber,RequireSpecialCharacter,
                PasswordHistoryCount,MaximumFailedAttempts,LockoutMinutes,ExpiryDays FROM sec.PasswordPolicies WHERE IsActive=1 ORDER BY PolicyId DESC;";
            using (var connection = Db.OpenConnection()) using (var command = new SqlCommand(sql, connection)) using (var reader = command.ExecuteReader())
            {
                if (!reader.Read()) return new PasswordPolicy();
                return new PasswordPolicy
                {
                    MinimumLength=Convert.ToInt32(reader[0]),RequireUppercase=Convert.ToBoolean(reader[1]),RequireLowercase=Convert.ToBoolean(reader[2]),
                    RequireNumber=Convert.ToBoolean(reader[3]),RequireSpecialCharacter=Convert.ToBoolean(reader[4]),PasswordHistoryCount=Convert.ToInt32(reader[5]),
                    MaximumFailedAttempts=Convert.ToInt32(reader[6]),LockoutMinutes=Convert.ToInt32(reader[7]),ExpiryDays=Convert.ToInt32(reader[8])
                };
            }
        }

        public IList<PasswordHashResult> GetPasswordHistory(int userId)
        {
            const string sql = @"SELECT PasswordHash,PasswordSalt,PasswordIterations FROM sec.PasswordHistory WHERE UserId=@UserId ORDER BY PasswordHistoryId DESC;";
            var items = new List<PasswordHashResult>();
            using (var connection = Db.OpenConnection()) using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int));
                using (var reader = command.ExecuteReader()) while (reader.Read()) items.Add(new PasswordHashResult
                {
                    Hash=(byte[])reader[0],Salt=(byte[])reader[1],Iterations=Convert.ToInt32(reader[2]),Algorithm="PBKDF2-HMAC-SHA256"
                });
            }
            return items;
        }

        public void UpdateProfile(UserIdentity user)
        {
            const string sql = @"UPDATE sec.Users SET Email=@Email,Phone=@Phone,FirstName=@FirstName,LastName=@LastName,Position=@Position,ModifiedUtc=SYSUTCDATETIME() WHERE UserId=@UserId;";
            using (var connection = Db.OpenConnection()) using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(Db.Parameter("@Email", user.Email, SqlDbType.NVarChar, 256));command.Parameters.Add(Db.Parameter("@Phone", user.Phone, SqlDbType.NVarChar, 50));
                command.Parameters.Add(Db.Parameter("@FirstName", user.FirstName, SqlDbType.NVarChar, 100));command.Parameters.Add(Db.Parameter("@LastName", user.LastName, SqlDbType.NVarChar, 100));
                command.Parameters.Add(Db.Parameter("@Position", user.Position, SqlDbType.NVarChar, 150));command.Parameters.Add(Db.Parameter("@UserId", user.UserId, SqlDbType.Int));
                if (command.ExecuteNonQuery()!=1) throw new InvalidOperationException("User was not found.");
            }
        }

        public void ChangePassword(int userId, PasswordHashResult password, DateTime? expiresUtc, int historyCount)
        {
            using (var connection=Db.OpenConnection()) using (var transaction=connection.BeginTransaction())
            {
                try
                {
                    const string historySql=@"INSERT sec.PasswordHistory(UserId,PasswordHash,PasswordSalt,PasswordIterations)
                        SELECT UserId,PasswordHash,PasswordSalt,PasswordIterations FROM sec.UserCredentials WHERE UserId=@UserId AND PasswordHash IS NOT NULL;";
                    using(var command=new SqlCommand(historySql,connection,transaction)){command.Parameters.Add(Db.Parameter("@UserId",userId,SqlDbType.Int));command.ExecuteNonQuery();}
                    const string updateSql=@"UPDATE sec.UserCredentials SET PasswordHash=@Hash,PasswordSalt=@Salt,PasswordIterations=@Iterations,PasswordAlgorithm=@Algorithm,
                        PasswordChangedUtc=SYSUTCDATETIME(),PasswordExpiresUtc=@ExpiresUtc,FailedLoginCount=0,LockedUntilUtc=NULL WHERE UserId=@UserId;
                        UPDATE sec.Users SET MustChangePassword=0,ModifiedUtc=SYSUTCDATETIME() WHERE UserId=@UserId;";
                    using(var command=new SqlCommand(updateSql,connection,transaction)){command.Parameters.Add(Db.Parameter("@Hash",password.Hash,SqlDbType.VarBinary,64));command.Parameters.Add(Db.Parameter("@Salt",password.Salt,SqlDbType.VarBinary,64));command.Parameters.Add(Db.Parameter("@Iterations",password.Iterations,SqlDbType.Int));command.Parameters.Add(Db.Parameter("@Algorithm",password.Algorithm,SqlDbType.NVarChar,50));command.Parameters.Add(Db.Parameter("@ExpiresUtc",expiresUtc,SqlDbType.DateTime2));command.Parameters.Add(Db.Parameter("@UserId",userId,SqlDbType.Int));command.ExecuteNonQuery();}
                    const string trimSql=@"DELETE FROM sec.PasswordHistory WHERE UserId=@UserId AND PasswordHistoryId NOT IN
                        (SELECT TOP (@HistoryCount) PasswordHistoryId FROM sec.PasswordHistory WHERE UserId=@UserId ORDER BY PasswordHistoryId DESC);";
                    using(var command=new SqlCommand(trimSql,connection,transaction)){command.Parameters.Add(Db.Parameter("@UserId",userId,SqlDbType.Int));command.Parameters.Add(Db.Parameter("@HistoryCount",Math.Max(0,historyCount),SqlDbType.Int));command.ExecuteNonQuery();}
                    transaction.Commit();
                }
                catch { transaction.Rollback(); throw; }
            }
        }

        public void RecordLoginResult(int userId, bool succeeded, int maximumAttempts, int lockoutMinutes)
        {
            const string sql = @"UPDATE sec.UserCredentials SET
                FailedLoginCount=CASE WHEN @Succeeded=1 THEN 0 ELSE FailedLoginCount+1 END,
                LockedUntilUtc=CASE WHEN @Succeeded=1 THEN NULL WHEN FailedLoginCount+1>=@MaximumAttempts THEN DATEADD(MINUTE,@LockoutMinutes,SYSUTCDATETIME()) ELSE LockedUntilUtc END,
                LastLoginUtc=CASE WHEN @Succeeded=1 THEN SYSUTCDATETIME() ELSE LastLoginUtc END
                WHERE UserId=@UserId;";
            using (var connection = Db.OpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(Db.Parameter("@Succeeded", succeeded, SqlDbType.Bit));
                command.Parameters.Add(Db.Parameter("@MaximumAttempts", maximumAttempts, SqlDbType.Int));
                command.Parameters.Add(Db.Parameter("@LockoutMinutes", lockoutMinutes, SqlDbType.Int));
                command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int));
                command.ExecuteNonQuery();
            }
        }

        private static UserIdentity MapUser(IDataRecord r) => new UserIdentity
        {
            UserId = Convert.ToInt32(r["UserId"]), UserName = Convert.ToString(r["UserName"]), Email = Convert.ToString(r["Email"]),
            Phone = Convert.ToString(r["Phone"]), FirstName = Convert.ToString(r["FirstName"]), LastName = Convert.ToString(r["LastName"]),
            Position = Convert.ToString(r["Position"]), DivisionId = r["DivisionId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["DivisionId"]), DepartmentId = r["DepartmentId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["DepartmentId"]), DivisionName = Convert.ToString(r["DivisionName"]), DepartmentName = Convert.ToString(r["DepartmentName"]),
            ProfileImagePath = Convert.ToString(r["ProfileImagePath"]), AccountType = (AccountType)Convert.ToByte(r["AccountType"]), MustChangePassword = Convert.ToBoolean(r["MustChangePassword"])
        };
    }

    public sealed class ApplicationRepository
    {
        public bool HasRole(int userId, string applicationCode, string roleCode)
        {
            const string sql = @"SELECT CASE WHEN EXISTS(SELECT 1 FROM sec.UserApplicationRoles x
                JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId JOIN sec.Roles r ON r.RoleId=x.RoleId
                WHERE x.UserId=@UserId AND x.IsActive=1 AND a.IsActive=1 AND r.IsActive=1
                  AND a.ApplicationCode=@ApplicationCode AND r.RoleCode=@RoleCode) THEN 1 ELSE 0 END;";
            using (var connection=Db.OpenConnection()) using (var command=new SqlCommand(sql,connection))
            {
                command.Parameters.Add(Db.Parameter("@UserId",userId,SqlDbType.Int));
                command.Parameters.Add(Db.Parameter("@ApplicationCode",applicationCode,SqlDbType.NVarChar,50));
                command.Parameters.Add(Db.Parameter("@RoleCode",roleCode,SqlDbType.NVarChar,50));
                return Convert.ToBoolean(command.ExecuteScalar());
            }
        }

        public string GetRoleCode(int userId, string applicationCode)
        {
            const string sql = @"SELECT TOP(1) r.RoleCode FROM sec.UserApplicationRoles x
                JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId JOIN sec.Roles r ON r.RoleId=x.RoleId
                WHERE x.UserId=@UserId AND x.IsActive=1 AND a.IsActive=1 AND r.IsActive=1 AND a.ApplicationCode=@ApplicationCode
                ORDER BY CASE WHEN r.RoleCode='SYSTEM_ADMIN' THEN 0 ELSE 1 END,r.RoleCode;";
            using(var connection=Db.OpenConnection())using(var command=new SqlCommand(sql,connection))
            {command.Parameters.Add(Db.Parameter("@UserId",userId,SqlDbType.Int));command.Parameters.Add(Db.Parameter("@ApplicationCode",applicationCode,SqlDbType.NVarChar,50));return Convert.ToString(command.ExecuteScalar());}
        }
        public bool HasAccess(int userId, string applicationCode)
        {
            const string sql = @"SELECT CASE WHEN EXISTS(SELECT 1 FROM sec.UserApplicationRoles uar INNER JOIN sec.Applications a ON a.ApplicationId=uar.ApplicationId WHERE uar.UserId=@UserId AND uar.IsActive=1 AND a.IsActive=1 AND a.ApplicationCode=@ApplicationCode) THEN 1 ELSE 0 END;";
            using (var connection = Db.OpenConnection()) using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int)); command.Parameters.Add(Db.Parameter("@ApplicationCode", applicationCode, SqlDbType.NVarChar, 50));
                return Convert.ToBoolean(command.ExecuteScalar());
            }
        }

        public IList<ApplicationDefinition> GetForUser(int userId)
        {
            const string sql = @"SELECT a.ApplicationId,a.ApplicationCode,a.NameTh,a.NameEn,a.DescriptionTh,a.DescriptionEn,a.IconText,a.IconPath,a.TargetUrl,a.DisplayOrder,a.IsActive
                FROM sec.Applications a INNER JOIN sec.UserApplicationRoles uar ON uar.ApplicationId=a.ApplicationId
                WHERE uar.UserId=@UserId AND uar.IsActive=1 AND a.IsActive=1 GROUP BY a.ApplicationId,a.ApplicationCode,a.NameTh,a.NameEn,a.DescriptionTh,a.DescriptionEn,a.IconText,a.IconPath,a.TargetUrl,a.DisplayOrder,a.IsActive
                ORDER BY a.DisplayOrder,a.NameEn;";
            var items = new List<ApplicationDefinition>();
            using (var connection = Db.OpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int));
                using (var reader = command.ExecuteReader()) while (reader.Read()) items.Add(new ApplicationDefinition
                {
                    ApplicationId=Convert.ToInt32(reader["ApplicationId"]),ApplicationCode=Convert.ToString(reader["ApplicationCode"]),NameTh=Convert.ToString(reader["NameTh"]),NameEn=Convert.ToString(reader["NameEn"]),
                    DescriptionTh=Convert.ToString(reader["DescriptionTh"]),DescriptionEn=Convert.ToString(reader["DescriptionEn"]),IconText=Convert.ToString(reader["IconText"]),IconPath=Convert.ToString(reader["IconPath"]),
                    TargetUrl=Convert.ToString(reader["TargetUrl"]),DisplayOrder=Convert.ToInt32(reader["DisplayOrder"]),IsActive=Convert.ToBoolean(reader["IsActive"])
                });
            }
            return items;
        }
    }

    public sealed partial class AssetSurveyRepository
    {
        public DataTable Search(string query, string status, SurveyAccessContext access, string queueStatus)
        {
            const string sql = @"SELECT TOP (1000) s.SurveyId,s.SurveyNo,s.FixedAssetNo,s.SurveyDate,s.AssetName,s.Brand,s.ModelDescription,s.SerialNumber,
                s.WidthCm,s.LengthCm,s.HeightCm,s.WeightKg,s.Quantity,m.UomCode,s.PurchaseOrderNo,s.ReceivedDate,s.Status,s.EstimatedValue,
                d.DepartmentName,COALESCE(NULLIF(s.CustodianName,''),u.DisplayName) AS Custodian,creator.DisplayName AS CreatedByName,b.BuildingName,fl.FloorName,rm.RoomName,s.ReturnReason,
                (SELECT TOP(1) AttachmentId FROM fa.AssetSurveyAttachments a WHERE a.SurveyId=s.SurveyId AND a.AttachmentType='ACTUAL') ActualAttachmentId
                FROM fa.AssetSurveys s INNER JOIN mst.Departments d ON d.DepartmentId=s.DepartmentId
                LEFT JOIN sec.Users u ON u.UserId=s.CustodianUserId LEFT JOIN sec.Users creator ON creator.UserId=s.SurveyorUserId
                LEFT JOIN mst.Uoms m ON m.UomId=s.UomId LEFT JOIN mst.Buildings b ON b.BuildingId=s.BuildingId
                LEFT JOIN mst.Floors fl ON fl.FloorId=s.FloorId LEFT JOIN mst.Rooms rm ON rm.RoomId=s.RoomId
                WHERE (@Query='' OR s.SurveyNo LIKE '%'+@Query+'%' OR ISNULL(s.FixedAssetNo,'') LIKE '%'+@Query+'%' OR s.AssetName LIKE '%'+@Query+'%' OR ISNULL(s.SerialNumber,'') LIKE '%'+@Query+'%' OR ISNULL(s.CustodianName,'') LIKE '%'+@Query+'%')
                  AND (@Status='' OR s.Status=@Status)
                  AND (@IsAdmin=1 OR s.SurveyorUserId=@UserId
                    OR (@QueueStatus='ManagerReview' AND @RoleCode IN('ASSET_MANAGER','DEPT_MANAGER') AND s.Status='ManagerReview')
                    OR (@QueueStatus='FinanceReview' AND @RoleCode='FINANCE' AND s.Status='FinanceReview'))
                ORDER BY s.ModifiedUtc DESC;";
            using (var connection = Db.OpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(Db.Parameter("@Query", query ?? string.Empty, SqlDbType.NVarChar, 150));
                command.Parameters.Add(Db.Parameter("@Status", status ?? string.Empty, SqlDbType.NVarChar, 30));
                command.Parameters.Add(Db.Parameter("@UserId", access.UserId, SqlDbType.Int));
                command.Parameters.Add(Db.Parameter("@IsAdmin", access.IsSystemAdministrator, SqlDbType.Bit));
                command.Parameters.Add(Db.Parameter("@RoleCode", access.RoleCode ?? string.Empty, SqlDbType.NVarChar, 50));
                command.Parameters.Add(Db.Parameter("@QueueStatus", queueStatus ?? string.Empty, SqlDbType.NVarChar, 30));
                var table = new DataTable(); using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table); return table;
            }
        }
    }
}
