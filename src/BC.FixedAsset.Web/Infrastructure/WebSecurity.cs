using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Data;
using System;
using System.Web;
using System.Web.UI;

namespace BC.FixedAsset.Web
{
    public static class CurrentUser
    {
        private const string SessionKey = "BCFA.CurrentUser";
        public static UserIdentity Get() => HttpContext.Current.Session[SessionKey] as UserIdentity;
        public static void Set(UserIdentity value) => HttpContext.Current.Session[SessionKey] = value;
        public static int Id => Get()?.UserId ?? 0;
    }

    public abstract class SecurePage : Page
    {
        protected SurveyAccessContext AssetAccess
        {
            get
            {
                var repository = new ApplicationRepository();
                return new SurveyAccessContext
                {
                    UserId = CurrentUser.Id,
                    IsSystemAdministrator = repository.HasRole(CurrentUser.Id, "ADMIN", "SYSTEM_ADMIN"),
                    RoleCode = repository.GetRoleCode(CurrentUser.Id, "FIXED_ASSET")
                };
            }
        }
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (!Context.User.Identity.IsAuthenticated || CurrentUser.Get() == null)
                Response.Redirect("~/Account/Login.aspx?returnUrl=" + Server.UrlEncode(Request.RawUrl), true);
            var path = Request.AppRelativeCurrentExecutionFilePath.ToLowerInvariant();
            var applicationCode = path == "~/fixedasset/numbering.aspx" ? "ADMIN" : path.StartsWith("~/admin/") ? "ADMIN" : path.StartsWith("~/fixedasset/") ? "FIXED_ASSET" : null;
            if (applicationCode != null && !new ApplicationRepository().HasAccess(CurrentUser.Id, applicationCode))
                Response.Redirect("~/Portal/Default.aspx?denied=1", true);
        }
    }
}
