using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Core.Security;
using System;
using System.Data;
using System.Data.SqlClient;

namespace BC.FixedAsset.Data
{
    public sealed class AdministrationRepository
    {
        public DataTable GetApplications() => Query("SELECT ApplicationId,ApplicationCode,IconText,IconPath,NameTh,NameEn,DescriptionTh,DescriptionEn,TargetUrl,DisplayOrder,IsActive FROM sec.Applications ORDER BY DisplayOrder,NameEn;");

        public DataTable GetUsers() => Query(@"SELECT u.UserId,u.UserName,u.DisplayName,u.Email,u.Phone,u.Position,
            d.DepartmentName,v.DivisionName,u.AccountType,u.IsActive,u.MustChangePassword,c.LastLoginUtc,
            (SELECT TOP(1) r.RoleName FROM sec.UserApplicationRoles x JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId JOIN sec.Roles r ON r.RoleId=x.RoleId WHERE x.UserId=u.UserId AND x.IsActive=1 AND a.ApplicationCode='FIXED_ASSET') FixedAssetRole,
            (SELECT TOP(1) r.RoleName FROM sec.UserApplicationRoles x JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId JOIN sec.Roles r ON r.RoleId=x.RoleId WHERE x.UserId=u.UserId AND x.IsActive=1 AND a.ApplicationCode='ADMIN') AdministrationRole
            FROM sec.Users u LEFT JOIN mst.Departments d ON d.DepartmentId=u.DepartmentId LEFT JOIN mst.Divisions v ON v.DivisionId=u.DivisionId
            LEFT JOIN sec.UserCredentials c ON c.UserId=u.UserId ORDER BY u.DisplayName;");

        public DataTable GetDepartments() => Query("SELECT DepartmentId,DepartmentName FROM mst.Departments WHERE IsActive=1 ORDER BY DepartmentName;");
        public DataTable GetRoles() => Query("SELECT RoleId,RoleCode,RoleName FROM sec.Roles WHERE IsActive=1 ORDER BY RoleName;");

        public UserAdministrationModel GetUser(int userId)
        {
            const string sql = @"SELECT u.UserId,u.UserName,u.Email,u.Phone,u.FirstName,u.LastName,u.Position,u.DivisionId,u.DepartmentId,u.ProfileImagePath,u.AccountType,u.MustChangePassword,u.IsActive,c.PasswordExpiresUtc,
              (SELECT TOP(1) x.RoleId FROM sec.UserApplicationRoles x JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId WHERE x.UserId=u.UserId AND x.IsActive=1 AND a.ApplicationCode='FIXED_ASSET') FixedAssetRoleId,
              (SELECT TOP(1) x.RoleId FROM sec.UserApplicationRoles x JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId WHERE x.UserId=u.UserId AND x.IsActive=1 AND a.ApplicationCode='ADMIN') AdministrationRoleId
              FROM sec.Users u LEFT JOIN sec.UserCredentials c ON c.UserId=u.UserId WHERE u.UserId=@UserId;";
            using (var connection = Db.OpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int));
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new UserAdministrationModel
                    {
                        User = new UserIdentity
                        {
                            UserId = reader.GetInt32(reader.GetOrdinal("UserId")), UserName = Convert.ToString(reader["UserName"]), Email = Convert.ToString(reader["Email"]),
                            Phone = Convert.ToString(reader["Phone"]), FirstName = Convert.ToString(reader["FirstName"]), LastName = Convert.ToString(reader["LastName"]),
                            Position = Convert.ToString(reader["Position"]), DivisionId = NullableInt(reader["DivisionId"]), DepartmentId = NullableInt(reader["DepartmentId"]),
                            ProfileImagePath = Convert.ToString(reader["ProfileImagePath"]), AccountType = (AccountType)Convert.ToInt32(reader["AccountType"]),
                            MustChangePassword = Convert.ToBoolean(reader["MustChangePassword"]), IsActive = Convert.ToBoolean(reader["IsActive"])
                        },
                        FixedAssetRoleId = NullableInt(reader["FixedAssetRoleId"]), AdministrationRoleId = NullableInt(reader["AdministrationRoleId"]),
                        PasswordExpiresUtc = NullableDateTime(reader["PasswordExpiresUtc"])
                    };
                }
            }
        }

        public void SaveApplication(ApplicationDefinition app, int userId)
        {
            const string sql = @"IF @ApplicationId=0
              INSERT sec.Applications(ApplicationCode,NameTh,NameEn,DescriptionTh,DescriptionEn,IconText,IconPath,TargetUrl,DisplayOrder,IsActive,CreatedByUserId,ModifiedByUserId)
              VALUES(@ApplicationCode,@NameTh,@NameEn,@DescriptionTh,@DescriptionEn,@IconText,@IconPath,@TargetUrl,@DisplayOrder,@IsActive,@UserId,@UserId)
            ELSE UPDATE sec.Applications SET ApplicationCode=@ApplicationCode,NameTh=@NameTh,NameEn=@NameEn,DescriptionTh=@DescriptionTh,DescriptionEn=@DescriptionEn,IconText=@IconText,IconPath=@IconPath,TargetUrl=@TargetUrl,DisplayOrder=@DisplayOrder,IsActive=@IsActive,ModifiedByUserId=@UserId,ModifiedUtc=SYSUTCDATETIME() WHERE ApplicationId=@ApplicationId;";
            using (var c=Db.OpenConnection()) using(var cmd=new SqlCommand(sql,c))
            {
                cmd.Parameters.Add(Db.Parameter("@ApplicationId",app.ApplicationId,SqlDbType.Int));cmd.Parameters.Add(Db.Parameter("@ApplicationCode",app.ApplicationCode,SqlDbType.NVarChar,50));cmd.Parameters.Add(Db.Parameter("@NameTh",app.NameTh,SqlDbType.NVarChar,150));cmd.Parameters.Add(Db.Parameter("@NameEn",app.NameEn,SqlDbType.NVarChar,150));cmd.Parameters.Add(Db.Parameter("@DescriptionTh",app.DescriptionTh,SqlDbType.NVarChar,500));cmd.Parameters.Add(Db.Parameter("@DescriptionEn",app.DescriptionEn,SqlDbType.NVarChar,500));cmd.Parameters.Add(Db.Parameter("@IconText",app.IconText,SqlDbType.NVarChar,20));cmd.Parameters.Add(Db.Parameter("@IconPath",app.IconPath,SqlDbType.NVarChar,500));cmd.Parameters.Add(Db.Parameter("@TargetUrl",app.TargetUrl,SqlDbType.NVarChar,500));cmd.Parameters.Add(Db.Parameter("@DisplayOrder",app.DisplayOrder,SqlDbType.Int));cmd.Parameters.Add(Db.Parameter("@IsActive",app.IsActive,SqlDbType.Bit));cmd.Parameters.Add(Db.Parameter("@UserId",userId,SqlDbType.Int));cmd.ExecuteNonQuery();
            }
        }

        public int CreateLocalUser(UserIdentity user, PasswordHashResult password, DateTime? passwordExpiresUtc, int? fixedAssetRoleId, int? administrationRoleId)
        {
            using(var c=Db.OpenConnection())using(var tx=c.BeginTransaction())
            {
                try
                {
                    const string userSql=@"INSERT sec.Users(UserName,Email,Phone,FirstName,LastName,Position,DivisionId,DepartmentId,ProfileImagePath,AccountType,MustChangePassword,IsActive)
                        VALUES(@UserName,@Email,@Phone,@FirstName,@LastName,@Position,COALESCE(@DivisionId,(SELECT DivisionId FROM mst.Departments WHERE DepartmentId=@DepartmentId)),@DepartmentId,'~/Assets/default-profile.svg',0,0,1);SELECT CAST(SCOPE_IDENTITY() AS int);";
                    int id;using(var cmd=new SqlCommand(userSql,c,tx)){AddUserParameters(cmd,user);id=Convert.ToInt32(cmd.ExecuteScalar());}
                    SaveCredential(c,tx,id,password,passwordExpiresUtc);
                    ReplaceApplicationRole(c,tx,id,"FIXED_ASSET",fixedAssetRoleId);
                    ReplaceApplicationRole(c,tx,id,"ADMIN",administrationRoleId);
                    tx.Commit();return id;
                }catch{tx.Rollback();throw;}
            }
        }

        public void UpdateLocalUser(UserAdministrationModel model, PasswordHashResult replacementPassword, DateTime? passwordExpiresUtc)
        {
            using (var connection = Db.OpenConnection()) using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    const string sql = @"UPDATE sec.Users SET UserName=@UserName,Email=@Email,Phone=@Phone,FirstName=@FirstName,LastName=@LastName,Position=@Position,DivisionId=COALESCE(@DivisionId,(SELECT DivisionId FROM mst.Departments WHERE DepartmentId=@DepartmentId)),DepartmentId=@DepartmentId,IsActive=@IsActive,MustChangePassword=0,ModifiedUtc=SYSUTCDATETIME() WHERE UserId=@UserId;";
                    using (var command = new SqlCommand(sql, connection, transaction))
                    {
                        AddUserParameters(command, model.User); command.Parameters.Add(Db.Parameter("@UserId", model.User.UserId, SqlDbType.Int));
                        command.Parameters.Add(Db.Parameter("@IsActive", model.User.IsActive, SqlDbType.Bit));
                        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("User was not found.");
                    }
                    if (replacementPassword != null) SaveCredential(connection, transaction, model.User.UserId, replacementPassword, passwordExpiresUtc);
                    ReplaceApplicationRole(connection, transaction, model.User.UserId, "FIXED_ASSET", model.FixedAssetRoleId);
                    ReplaceApplicationRole(connection, transaction, model.User.UserId, "ADMIN", model.AdministrationRoleId);
                    transaction.Commit();
                }
                catch { transaction.Rollback(); throw; }
            }
        }

        public bool DeleteOrDeactivateUser(int userId)
        {
            using (var connection = Db.OpenConnection()) using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    const string dependencySql = @"SELECT CASE WHEN EXISTS(SELECT 1 FROM fa.AssetSurveys WHERE SurveyorUserId=@UserId OR CustodianUserId=@UserId)
                      OR EXISTS(SELECT 1 FROM fa.AssetSurveyAttachments WHERE UploadedByUserId=@UserId)
                      OR EXISTS(SELECT 1 FROM fa.ApprovalTasks WHERE AssignedUserId=@UserId OR CompletedByUserId=@UserId)
                      OR EXISTS(SELECT 1 FROM fa.FixedAssets WHERE CustodianUserId=@UserId)
                      OR EXISTS(SELECT 1 FROM audit.AuditLogs WHERE UserId=@UserId)
                      OR EXISTS(SELECT 1 FROM sec.Applications WHERE CreatedByUserId=@UserId OR ModifiedByUserId=@UserId)
                      OR EXISTS(SELECT 1 FROM fa.AssetNumberSchemes WHERE ModifiedByUserId=@UserId)
                      OR EXISTS(SELECT 1 FROM mst.Departments WHERE ActiveManagerUserId=@UserId) THEN 1 ELSE 0 END;";
                    bool hasDependencies; using (var command = new SqlCommand(dependencySql, connection, transaction)) { command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int)); hasDependencies = Convert.ToBoolean(command.ExecuteScalar()); }
                    if (hasDependencies)
                    {
                        using (var command = new SqlCommand("UPDATE sec.Users SET IsActive=0,ModifiedUtc=SYSUTCDATETIME() WHERE UserId=@UserId;DELETE sec.UserApplicationRoles WHERE UserId=@UserId;", connection, transaction)) { command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int)); command.ExecuteNonQuery(); }
                        transaction.Commit(); return false;
                    }
                    using (var command = new SqlCommand("DELETE sec.UserApplicationRoles WHERE UserId=@UserId;DELETE sec.PasswordHistory WHERE UserId=@UserId;DELETE sec.UserCredentials WHERE UserId=@UserId;DELETE sec.Users WHERE UserId=@UserId;", connection, transaction)) { command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int)); command.ExecuteNonQuery(); }
                    transaction.Commit(); return true;
                }
                catch { transaction.Rollback(); throw; }
            }
        }

        private static void SaveCredential(SqlConnection connection, SqlTransaction transaction, int userId, PasswordHashResult password, DateTime? passwordExpiresUtc)
        {
            const string sql = @"MERGE sec.UserCredentials AS target USING(SELECT @UserId UserId) AS source ON target.UserId=source.UserId
              WHEN MATCHED THEN UPDATE SET PasswordHash=@Hash,PasswordSalt=@Salt,PasswordIterations=@Iterations,PasswordAlgorithm=@Algorithm,PasswordChangedUtc=SYSUTCDATETIME(),PasswordExpiresUtc=@ExpiresUtc,FailedLoginCount=0,LockedUntilUtc=NULL
              WHEN NOT MATCHED THEN INSERT(UserId,PasswordHash,PasswordSalt,PasswordIterations,PasswordAlgorithm,PasswordChangedUtc,PasswordExpiresUtc,FailedLoginCount) VALUES(@UserId,@Hash,@Salt,@Iterations,@Algorithm,SYSUTCDATETIME(),@ExpiresUtc,0);";
            using(var command=new SqlCommand(sql,connection,transaction)){command.Parameters.Add(Db.Parameter("@UserId",userId,SqlDbType.Int));command.Parameters.Add(Db.Parameter("@Hash",password.Hash,SqlDbType.VarBinary,64));command.Parameters.Add(Db.Parameter("@Salt",password.Salt,SqlDbType.VarBinary,64));command.Parameters.Add(Db.Parameter("@Iterations",password.Iterations,SqlDbType.Int));command.Parameters.Add(Db.Parameter("@Algorithm",password.Algorithm,SqlDbType.NVarChar,50));command.Parameters.Add(Db.Parameter("@ExpiresUtc",passwordExpiresUtc,SqlDbType.DateTime2));command.ExecuteNonQuery();}
        }

        private static void ReplaceApplicationRole(SqlConnection connection, SqlTransaction transaction, int userId, string applicationCode, int? roleId)
        {
            const string deleteSql = @"DELETE x FROM sec.UserApplicationRoles x JOIN sec.Applications a ON a.ApplicationId=x.ApplicationId WHERE x.UserId=@UserId AND a.ApplicationCode=@ApplicationCode;";
            using (var command = new SqlCommand(deleteSql, connection, transaction)) { command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int)); command.Parameters.Add(Db.Parameter("@ApplicationCode", applicationCode, SqlDbType.NVarChar, 50)); command.ExecuteNonQuery(); }
            if (!roleId.HasValue) return;
            const string insertSql = @"INSERT sec.UserApplicationRoles(UserId,ApplicationId,RoleId) SELECT @UserId,ApplicationId,@RoleId FROM sec.Applications WHERE ApplicationCode=@ApplicationCode AND IsActive=1;";
            using (var command = new SqlCommand(insertSql, connection, transaction))
            {
                command.Parameters.Add(Db.Parameter("@UserId", userId, SqlDbType.Int)); command.Parameters.Add(Db.Parameter("@RoleId", roleId.Value, SqlDbType.Int)); command.Parameters.Add(Db.Parameter("@ApplicationCode", applicationCode, SqlDbType.NVarChar, 50));
                if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Application is not active or was not found: " + applicationCode);
            }
        }

        private static void AddUserParameters(SqlCommand command, UserIdentity user)
        {
            command.Parameters.Add(Db.Parameter("@UserName", user.UserName, SqlDbType.NVarChar, 100)); command.Parameters.Add(Db.Parameter("@Email", user.Email, SqlDbType.NVarChar, 256));
            command.Parameters.Add(Db.Parameter("@Phone", user.Phone, SqlDbType.NVarChar, 50)); command.Parameters.Add(Db.Parameter("@FirstName", user.FirstName, SqlDbType.NVarChar, 100));
            command.Parameters.Add(Db.Parameter("@LastName", user.LastName, SqlDbType.NVarChar, 100)); command.Parameters.Add(Db.Parameter("@Position", user.Position, SqlDbType.NVarChar, 150));
            command.Parameters.Add(Db.Parameter("@DivisionId", user.DivisionId, SqlDbType.Int)); command.Parameters.Add(Db.Parameter("@DepartmentId", user.DepartmentId, SqlDbType.Int));
        }

        private static int? NullableInt(object value) => value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        private static DateTime? NullableDateTime(object value) => value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        private static DataTable Query(string sql){using(var c=Db.OpenConnection())using(var cmd=new SqlCommand(sql,c)){var t=new DataTable();using(var a=new SqlDataAdapter(cmd))a.Fill(t);return t;}}
    }
}
