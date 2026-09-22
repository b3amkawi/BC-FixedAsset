using BC.FixedAsset.Services;
using System;
using System.Web.Security;

namespace BC.FixedAsset.Web.Account
{
    public partial class Login : System.Web.UI.Page
    {
        protected global::System.Web.UI.WebControls.Panel pnlError;
        protected global::System.Web.UI.WebControls.Literal litError;
        protected global::System.Web.UI.WebControls.TextBox txtUserName;
        protected global::System.Web.UI.WebControls.TextBox txtPassword;
        protected global::System.Web.UI.WebControls.Button btnLogin;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser.Get() == null && User.Identity.IsAuthenticated)
                FormsAuthentication.SignOut();
            if (!IsPostBack && User.Identity.IsAuthenticated && CurrentUser.Get() != null)
                Response.Redirect("~/Portal/Default.aspx");
        }
        protected void Login_Click(object sender, EventArgs e)
        {
            var result = new AuthenticationService().AuthenticateLocal(txtUserName.Text, txtPassword.Text);
            if (!result.Succeeded) { pnlError.Visible = true; litError.Text = Server.HtmlEncode(result.ErrorMessage); return; }
            CurrentUser.Set(result.User); FormsAuthentication.SetAuthCookie(result.User.UserName, false);
            if (result.User.MustChangePassword) { Response.Redirect("~/Account/Profile.aspx?changePassword=1"); return; }
            var returnUrl = Request.QueryString["returnUrl"];
            Response.Redirect(IsLocalUrl(returnUrl) ? returnUrl : "~/Portal/Default.aspx");
        }
        private bool IsLocalUrl(string value) => !string.IsNullOrWhiteSpace(value) && value.StartsWith("/") && !value.StartsWith("//") && !value.StartsWith("/\\");
    }
}
