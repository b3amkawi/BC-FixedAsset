using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Core.Security;
using BC.FixedAsset.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Text.RegularExpressions;

namespace BC.FixedAsset.Services
{
    public sealed class AuthenticationService
    {
        private readonly UserRepository _users = new UserRepository();

        public AuthenticationResult AuthenticateLocal(string userName, string password)
        {
            var record = _users.FindForAuthentication((userName ?? string.Empty).Trim());
            if (record == null || !record.IsActive) return Failed("Invalid username or password.");
            if (record.User.AccountType != AccountType.Local) return Failed("This account must sign in through corporate SSO.");
            if (record.LockedUntilUtc.HasValue && record.LockedUntilUtc.Value > DateTime.UtcNow) return Failed("Account is temporarily locked.");

            var valid = PasswordHasher.Verify(password, record.PasswordHash, record.PasswordSalt, record.PasswordIterations);
            _users.RecordLoginResult(record.User.UserId, valid, 5, 15);
            if (!valid) return Failed("Invalid username or password.");
            return new AuthenticationResult { Succeeded = true, User = record.User };
        }

        private static AuthenticationResult Failed(string message) => new AuthenticationResult { Succeeded = false, ErrorMessage = message };
    }

    public sealed class UserProfileService
    {
        private readonly UserRepository _users = new UserRepository();

        public void UpdateProfile(UserIdentity user)
        {
            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
                throw new ArgumentException("Email, first name and last name are required.");
            _users.UpdateProfile(user);
        }

        public void ChangePassword(UserIdentity user, string currentPassword, string newPassword, string confirmation)
        {
            if (newPassword != confirmation) throw new ArgumentException("New password and confirmation do not match.");
            if (string.IsNullOrWhiteSpace(newPassword)) throw new ArgumentException("New password is required.");
            var current = _users.FindForAuthentication(user.UserName);
            if (current == null || !PasswordHasher.Verify(currentPassword, current.PasswordHash, current.PasswordSalt, current.PasswordIterations))
                throw new ArgumentException("Current password is incorrect.");
            var hash = PasswordHasher.Hash(newPassword);
            _users.ChangePassword(user.UserId, hash);
            user.MustChangePassword = false;
        }
    }

    public sealed class PortalService
    {
        private readonly ApplicationRepository _applications = new ApplicationRepository();
        public IList<ApplicationDefinition> GetApplications(int userId) => _applications.GetForUser(userId);
    }

    public sealed class AssetSurveyService
    {
        private readonly AssetSurveyRepository _surveys = new AssetSurveyRepository();
        private readonly FixedAssetRepository _fixedAssets = new FixedAssetRepository();
        public DataTable Search(string query, string status, SurveyAccessContext access, string queueStatus = "") => _surveys.Search(query, status, access, queueStatus);
        public AssetSurvey Get(long id, SurveyAccessContext access) => _surveys.Get(id, access);
        public DataTable Detail(long id, SurveyAccessContext access) => _surveys.Detail(id, access);
        public IList<SurveyAttachment> Attachments(long id, SurveyAccessContext access) => _surveys.Attachments(id, access);
        public SurveyAttachment Attachment(long id, SurveyAccessContext access) => _surveys.Attachment(id, access);
        public long SaveDraft(AssetSurvey survey, SurveyAccessContext access) => _fixedAssets.SaveDraft(survey, access);
        public string Submit(long id, SurveyAccessContext access) => _surveys.Advance(id, access);
        public string Approve(long id, SurveyAccessContext access) => _surveys.Advance(id, access);
        public void Return(long id, string reason, SurveyAccessContext access) => _surveys.Return(id, reason, access);
        public void Delete(long id, SurveyAccessContext access, string uploadRoot)
        {
            foreach(var relative in _surveys.Delete(id, access))
            {
                var full=Path.GetFullPath(Path.Combine(uploadRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
                if(full.StartsWith(Path.GetFullPath(uploadRoot),StringComparison.OrdinalIgnoreCase)&&File.Exists(full))File.Delete(full);
            }
        }
        public void SaveAttachment(long surveyId,string type,HttpPostedFile file,SurveyAccessContext access,string uploadRoot)
        {
            if(file==null||file.ContentLength==0)return;
            if(file.ContentLength>5*1024*1024)throw new ArgumentException("Each image must not exceed 5 MB.");
            var extension=Path.GetExtension(file.FileName).ToLowerInvariant();
            if(extension!=".jpg"&&extension!=".jpeg"&&extension!=".png"&&extension!=".webp")throw new ArgumentException("Only JPG, PNG and WEBP images are allowed.");
            byte[] content;using(var memory=new MemoryStream()){file.InputStream.CopyTo(memory);content=memory.ToArray();}
            var old=_surveys.SaveAttachment(surveyId,type,Path.GetFileName(file.FileName),file.ContentType,file.ContentLength,content,access);
            if(!string.IsNullOrEmpty(old)){var oldFull=Path.GetFullPath(Path.Combine(uploadRoot,old.Replace('/',Path.DirectorySeparatorChar)));if(oldFull.StartsWith(Path.GetFullPath(uploadRoot),StringComparison.OrdinalIgnoreCase)&&File.Exists(oldFull))File.Delete(oldFull);}
        }
        public DataTable GetReference(string type) => _fixedAssets.GetReference(type);
        public DashboardSummary GetDashboard() => _fixedAssets.GetDashboard();
        public DataTable GetRegister(string query) => _fixedAssets.GetRegister(query);
        public DataTable GetRegisterDetail(long id) => _fixedAssets.GetRegisterDetail(id);
    }

    public sealed class AssetNumberService
    {
        private readonly FixedAssetRepository _repository = new FixedAssetRepository();
        public AssetNumberScheme GetScheme() => _repository.GetNumberScheme();
        public void SaveScheme(AssetNumberScheme scheme, int userId)
        {
            if (scheme == null || string.IsNullOrWhiteSpace(scheme.Pattern) || !scheme.Pattern.Contains("{SEQ}")) throw new ArgumentException("Pattern must contain {SEQ}.");
            if (scheme.SequenceDigits < 1 || scheme.SequenceDigits > 12) throw new ArgumentException("Sequence digits must be between 1 and 12.");
            if (scheme.StartValue < 1) throw new ArgumentException("Start value must be at least 1.");
            _repository.SaveNumberScheme(scheme, userId);
        }
        public string Generate(int companyId, int categoryId, DateTime effectiveDate, int userId)
        {
            using (var connection = Db.OpenConnection())
            using (var command = new SqlCommand("fa.usp_GenerateFixedAssetNumber", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(Db.Parameter("@CompanyId", companyId, SqlDbType.Int));
                command.Parameters.Add(Db.Parameter("@CategoryId", categoryId, SqlDbType.Int));
                command.Parameters.Add(Db.Parameter("@EffectiveDate", effectiveDate, SqlDbType.Date));
                command.Parameters.Add(Db.Parameter("@RequestedByUserId", userId, SqlDbType.Int));
                var output = new SqlParameter("@AssetNumber", SqlDbType.NVarChar, 80) { Direction = ParameterDirection.Output };
                command.Parameters.Add(output); command.ExecuteNonQuery(); return Convert.ToString(output.Value);
            }
        }
    }

    public sealed class AdministrationService
    {
        private readonly AdministrationRepository _repository = new AdministrationRepository();
        public DataTable GetApplications() => _repository.GetApplications();
        public DataTable GetUsers() => _repository.GetUsers();
        public DataTable GetDepartments() => _repository.GetDepartments();
        public DataTable GetRoles() => _repository.GetRoles();
        public UserAdministrationModel GetUser(int userId) => _repository.GetUser(userId);
        public void SaveApplication(ApplicationDefinition application, int userId)
        {
            if (application == null || string.IsNullOrWhiteSpace(application.ApplicationCode) || string.IsNullOrWhiteSpace(application.NameTh) || string.IsNullOrWhiteSpace(application.NameEn))
                throw new ArgumentException("Application code and Thai/English names are required.");
            if (!IsAllowedLink(application.TargetUrl)) throw new ArgumentException("Target URL must be an application-local path or an HTTPS URL.");
            if (!string.IsNullOrWhiteSpace(application.IconPath) && !IsAllowedLink(application.IconPath)) throw new ArgumentException("Icon path must be an application-local path or an HTTPS URL.");
            _repository.SaveApplication(application, userId);
        }
        private static bool IsAllowedLink(string value) => !string.IsNullOrWhiteSpace(value) &&
            (value.StartsWith("~/", StringComparison.Ordinal) ||
             (value.StartsWith("/", StringComparison.Ordinal) && !value.StartsWith("//", StringComparison.Ordinal) && !value.StartsWith("/\\", StringComparison.Ordinal)) ||
             value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        public int CreateLocalUser(UserIdentity user, string initialPassword, int? fixedAssetRoleId, int? administrationRoleId)
        {
            ValidateUser(user);
            if (string.IsNullOrWhiteSpace(initialPassword)) throw new ArgumentException("Initial password is required.");
            var hash = PasswordHasher.Hash(initialPassword);
            return _repository.CreateLocalUser(user, hash, null, fixedAssetRoleId, administrationRoleId);
        }

        public void UpdateLocalUser(UserAdministrationModel model, string replacementPassword, int currentUserId)
        {
            if (model == null || model.User == null) throw new ArgumentException("User is required.");
            ValidateUser(model.User);
            if (model.User.UserId == currentUserId && !model.User.IsActive) throw new ArgumentException("You cannot deactivate your own account.");
            if (model.User.UserId == currentUserId && !model.AdministrationRoleId.HasValue) throw new ArgumentException("You cannot remove your own Administration access.");
            PasswordHashResult hash = null;
            if (!string.IsNullOrWhiteSpace(replacementPassword))
            {
                hash = PasswordHasher.Hash(replacementPassword);
            }
            _repository.UpdateLocalUser(model, hash, null);
        }

        public bool DeleteUser(int userId, int currentUserId)
        {
            if (userId == currentUserId) throw new ArgumentException("You cannot delete your own account.");
            return _repository.DeleteOrDeactivateUser(userId);
        }

        private static void ValidateUser(UserIdentity user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
                throw new ArgumentException("Username, first name and last name are required.");
            if (!Regex.IsMatch(user.UserName, "^[a-zA-Z0-9._-]{3,100}$")) throw new ArgumentException("Username must be 3-100 characters and use letters, numbers, dot, dash or underscore.");
        }
    }
}
