using System;
using System.Configuration;
using System.Web.Security;

namespace BC.FixedAsset.Web
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        protected global::System.Web.UI.WebControls.Literal litApplicationName;
        protected global::System.Web.UI.WebControls.Literal litEnvironment;
        protected global::System.Web.UI.WebControls.PlaceHolder phFixedAssetNavigation;
        protected global::System.Web.UI.WebControls.PlaceHolder phAdminNavigation;
        protected global::System.Web.UI.WebControls.Literal litBreadcrumb;
        protected global::System.Web.UI.WebControls.Literal litUserName;
        protected global::System.Web.UI.WebControls.Literal litUserInitials;
        protected global::System.Web.UI.WebControls.Literal litUserDepartment;
        protected global::System.Web.UI.WebControls.LinkButton btnLogout;

        protected void Page_Load(object sender, EventArgs e)
        {
            var path = Request.AppRelativeCurrentExecutionFilePath.ToLowerInvariant();
            var isNumberingAdmin = path == "~/fixedasset/numbering.aspx";
            var isAdmin = path.StartsWith("~/admin/") || isNumberingAdmin; var isFixedAsset = path.StartsWith("~/fixedasset/") && !isNumberingAdmin;
            var apps = new BC.FixedAsset.Data.ApplicationRepository();
            phAdminNavigation.Visible = apps.HasAccess(CurrentUser.Id, "ADMIN");
            phFixedAssetNavigation.Visible = apps.HasAccess(CurrentUser.Id, "FIXED_ASSET");
            litApplicationName.Text = isAdmin ? "BC Administration" : isFixedAsset ? "BC Fixed Asset" : "Application Portal";
            litBreadcrumb.Text = Server.HtmlEncode(Page.Title ?? litApplicationName.Text);
            litEnvironment.Text = Server.HtmlEncode(ConfigurationManager.AppSettings["EnvironmentName"] ?? "Development");
            var user = CurrentUser.Get();
            var displayName = user == null ? Context.User.Identity.Name : (user.FirstName + " " + user.LastName).Trim();
            if (string.IsNullOrWhiteSpace(displayName)) displayName = "BC User";
            litUserName.Text = Server.HtmlEncode(displayName);
            var parts = (displayName ?? "BC").Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            litUserInitials.Text = Server.HtmlEncode(parts.Length > 1 ? (parts[0][0].ToString()+parts[parts.Length-1][0]).ToUpperInvariant() : displayName.Substring(0, Math.Min(2, displayName.Length)).ToUpperInvariant());
            litUserDepartment.Text = Server.HtmlEncode(user?.DepartmentName ?? string.Empty);
        }

        protected void Logout_Click(object sender, EventArgs e)
        {
            CurrentUser.Set(null); Session.Clear(); FormsAuthentication.SignOut(); Response.Redirect("~/Account/Login.aspx");
        }
    }
}
