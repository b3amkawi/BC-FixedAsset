using BC.FixedAsset.Services;
using System;
using System.Web.UI.WebControls;

namespace BC.FixedAsset.Web.Account
{
    public partial class Profile : SecurePage
    {
        protected TextBox txtUserName, txtEmail, txtPhone, txtFirstName, txtLastName, txtPosition, txtDivision, txtDepartment;
        protected TextBox txtCurrentPassword, txtNewPassword, txtConfirmPassword;
        protected Label lblMessage;
        protected Panel pnlPasswordRequired;
        protected Literal litInitials;
        protected Button btnSave, btnChangePassword;
        private readonly UserProfileService service = new UserProfileService();

        protected void Page_Load(object sender, EventArgs e)
        {
            pnlPasswordRequired.Visible = CurrentUser.Get().MustChangePassword;
            if (IsPostBack) return;
            var user = CurrentUser.Get();
            var first = string.IsNullOrWhiteSpace(user.FirstName) ? "B" : user.FirstName.Substring(0,1);
            var last = string.IsNullOrWhiteSpace(user.LastName) ? "C" : user.LastName.Substring(0,1);
            litInitials.Text = Server.HtmlEncode((first+last).ToUpperInvariant());
            txtUserName.Text = user.UserName; txtEmail.Text = user.Email; txtPhone.Text = user.Phone;
            txtFirstName.Text = user.FirstName; txtLastName.Text = user.LastName; txtPosition.Text = user.Position;
            txtDivision.Text = user.DivisionName; txtDepartment.Text = user.DepartmentName;
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            try
            {
                var user = CurrentUser.Get();
                user.Email=txtEmail.Text.Trim();user.Phone=txtPhone.Text.Trim();user.FirstName=txtFirstName.Text.Trim();
                user.LastName=txtLastName.Text.Trim();user.Position=txtPosition.Text.Trim();
                service.UpdateProfile(user);CurrentUser.Set(user);Show("บันทึก Profile เรียบร้อย / Profile updated.");
            }
            catch(Exception ex){Show(Server.HtmlEncode(ex.Message));}
        }

        protected void ChangePassword_Click(object sender, EventArgs e)
        {
            try
            {
                var user=CurrentUser.Get();
                service.ChangePassword(user,txtCurrentPassword.Text,txtNewPassword.Text,txtConfirmPassword.Text);
                CurrentUser.Set(user);pnlPasswordRequired.Visible=false;Show("เปลี่ยนรหัสผ่านเรียบร้อย / Password changed.");
                txtCurrentPassword.Text=txtNewPassword.Text=txtConfirmPassword.Text=string.Empty;
            }
            catch(Exception ex){Show(Server.HtmlEncode(ex.Message));}
        }

        private void Show(string message){lblMessage.CssClass="message";lblMessage.Text=message;}
    }
}
