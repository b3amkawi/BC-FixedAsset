using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Data;
using BC.FixedAsset.Services;
using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Web.SessionState;

namespace BC.FixedAsset.Web
{
    public sealed class AttachmentHandler : IHttpHandler, IReadOnlySessionState
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            var user = CurrentUser.Get();
            if (!context.User.Identity.IsAuthenticated || user == null) { context.Response.StatusCode = 401; return; }
            long id;
            if (!long.TryParse(context.Request.QueryString["id"], out id)) { context.Response.StatusCode = 400; return; }

            var attachment = new AssetSurveyService().Attachment(id, GetAccess(context, user.UserId));
            if (attachment == null) { context.Response.StatusCode = 404; return; }

            var etag = "\"attachment-" + attachment.AttachmentId + "-" + attachment.UploadedUtc.Ticks + "-" + attachment.FileSizeBytes + "\"";
            ConfigureCache(context.Response, attachment.UploadedUtc, etag);
            if (string.Equals(context.Request.Headers["If-None-Match"], etag, StringComparison.Ordinal))
            {
                context.Response.StatusCode = 304;
                context.Response.SuppressContent = true;
                return;
            }

            context.Response.ContentType = attachment.ContentType;
            context.Response.AddHeader("Content-Disposition", "inline; filename=\"" + Path.GetFileName(attachment.OriginalFileName).Replace("\"", "") + "\"");
            if (attachment.FileContent != null && attachment.FileContent.Length > 0)
            {
                context.Response.AddHeader("Content-Length", attachment.FileContent.Length.ToString());
                context.Response.OutputStream.Write(attachment.FileContent, 0, attachment.FileContent.Length);
                return;
            }

            var root = context.Server.MapPath(ConfigurationManager.AppSettings["UploadRoot"]);
            var path = Path.GetFullPath(Path.Combine(root, (attachment.RelativePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) { context.Response.StatusCode = 404; return; }
            context.Response.AddHeader("Content-Length", new FileInfo(path).Length.ToString());
            context.Response.TransmitFile(path);
        }

        private static SurveyAccessContext GetAccess(HttpContext context, int userId)
        {
            var key = "BCFA.AttachmentAccess." + userId;
            var access = context.Cache[key] as SurveyAccessContext;
            if (access != null) return access;
            var applications = new ApplicationRepository();
            access = new SurveyAccessContext
            {
                UserId = userId,
                IsSystemAdministrator = applications.HasRole(userId, "ADMIN", "SYSTEM_ADMIN"),
                RoleCode = applications.GetRoleCode(userId, "FIXED_ASSET")
            };
            context.Cache.Insert(key, access, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(2));
            return access;
        }

        private static void ConfigureCache(HttpResponse response, DateTime uploadedUtc, string etag)
        {
            response.Cache.SetCacheability(HttpCacheability.Private);
            response.Cache.SetMaxAge(TimeSpan.FromHours(1));
            response.Cache.SetSlidingExpiration(true);
            response.Cache.SetETag(etag);
            response.Cache.SetLastModified(DateTime.SpecifyKind(uploadedUtc, DateTimeKind.Utc));
        }
    }
}
