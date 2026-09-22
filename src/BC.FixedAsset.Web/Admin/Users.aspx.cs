using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Services;
using System;
using System.Web.UI.WebControls;

namespace BC.FixedAsset.Web.Admin
{
    public partial class Users : SecurePage
    {
        protected GridView gridUsers;
        protected Panel pnlEditor;
        protected Label lblMessage;
        protected HiddenField hidUserId;
        protected TextBox txtUserName, txtEmail, txtPhone, txtPosition, txtFirstName, txtLastName, txtInitialPassword;
        protected DropDownList ddlDepartment, ddlExpiry, ddlFixedAssetRole, ddlAdministrationRole;
        protected CheckBox chkIsActive;
        protected Literal litEditorTitle;
        protected Button btnNew, btnSave, btnCancel;
        private readonly AdministrationService service = new AdministrationService();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            BindGrid(); BindSelectors();
        }

        private void BindSelectors()
        {
            ddlDepartment.DataSource = service.GetDepartments(); ddlDepartment.DataTextField = "DepartmentName"; ddlDepartment.DataValueField = "DepartmentId"; ddlDepartment.DataBind();
            BindRoles(ddlFixedAssetRole); BindRoles(ddlAdministrationRole);
        }

        private void BindRoles(DropDownList list)
        {
            list.Items.Clear(); list.Items.Add(new ListItem("ไม่มีสิทธิ์ / No access", string.Empty)); list.AppendDataBoundItems = true;
            list.DataSource = service.GetRoles(); list.DataTextField = "RoleName"; list.DataValueField = "RoleId"; list.DataBind();
        }

        private void BindGrid() { gridUsers.DataSource = service.GetUsers(); gridUsers.DataBind(); }

        protected void New_Click(object sender, EventArgs e)
        {
            ClearEditor(); pnlEditor.Visible = true; litEditorTitle.Text = "สร้าง User ใหม่ / Create User"; btnSave.Text = "สร้าง User";
        }

        protected void Grid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int userId; if (!int.TryParse(Convert.ToString(e.CommandArgument), out userId)) return;
            if (e.CommandName == "EditUser") { LoadEditor(userId); return; }
            if (e.CommandName == "DeleteUser")
            {
                try
                {
                    var deleted = service.DeleteUser(userId, CurrentUser.Id); BindGrid();
                    Show(deleted ? "ลบผู้ใช้เรียบร้อย / User deleted." : "ผู้ใช้มีข้อมูลอ้างอิง ระบบจึงปิดใช้งานบัญชีแทนการลบ / Account deactivated because referenced records exist.", false);
                }
                catch (Exception ex) { Show(Server.HtmlEncode(ex.Message), true); }
            }
        }

        private void LoadEditor(int userId)
        {
            var model = service.GetUser(userId); if (model == null) { Show("User was not found.", true); return; }
            hidUserId.Value = model.User.UserId.ToString(); txtUserName.Text = model.User.UserName; txtEmail.Text = model.User.Email; txtPhone.Text = model.User.Phone;
            txtPosition.Text = model.User.Position; txtFirstName.Text = model.User.FirstName; txtLastName.Text = model.User.LastName; chkIsActive.Checked = model.User.IsActive;
            Select(ddlDepartment, model.User.DepartmentId); Select(ddlFixedAssetRole, model.FixedAssetRoleId); Select(ddlAdministrationRole, model.AdministrationRoleId);
            ddlExpiry.SelectedValue = ExpiryValue(model.PasswordExpiresUtc); txtInitialPassword.Text = string.Empty;
            pnlEditor.Visible = true; litEditorTitle.Text = "แก้ไข User / Edit User"; btnSave.Text = "บันทึกการแก้ไข";
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            try
            {
                int userId; int.TryParse(hidUserId.Value, out userId);
                var user = new UserIdentity
                {
                    UserId = userId, UserName = txtUserName.Text.Trim(), Email = txtEmail.Text.Trim(), Phone = txtPhone.Text.Trim(), Position = txtPosition.Text.Trim(),
                    FirstName = txtFirstName.Text.Trim(), LastName = txtLastName.Text.Trim(), DepartmentId = SelectedInt(ddlDepartment), IsActive = chkIsActive.Checked
                };
                var expiryDays = Convert.ToInt32(ddlExpiry.SelectedValue);
                if (userId == 0)
                {
                    service.CreateLocalUser(user, txtInitialPassword.Text, expiryDays, SelectedInt(ddlFixedAssetRole), SelectedInt(ddlAdministrationRole));
                    Show("สร้าง User และกำหนดสิทธิ์เรียบร้อย ระบบจะบังคับเปลี่ยนรหัสผ่านเมื่อ Login ครั้งแรก / User created.", false);
                }
                else
                {
                    service.UpdateLocalUser(new UserAdministrationModel { User = user, FixedAssetRoleId = SelectedInt(ddlFixedAssetRole), AdministrationRoleId = SelectedInt(ddlAdministrationRole) }, txtInitialPassword.Text, expiryDays, CurrentUser.Id);
                    Show("บันทึกข้อมูลผู้ใช้และสิทธิ์เรียบร้อย / User updated.", false);
                }
                pnlEditor.Visible = false; BindGrid();
            }
            catch (Exception ex) { Show(Server.HtmlEncode(ex.Message), true); pnlEditor.Visible = true; }
        }

        protected void Cancel_Click(object sender, EventArgs e) { pnlEditor.Visible = false; }

        private void ClearEditor()
        {
            hidUserId.Value = string.Empty; txtUserName.Text = txtEmail.Text = txtPhone.Text = txtPosition.Text = txtFirstName.Text = txtLastName.Text = txtInitialPassword.Text = string.Empty;
            if (ddlDepartment.Items.Count > 0) ddlDepartment.SelectedIndex = 0; ddlExpiry.SelectedValue = "90"; ddlFixedAssetRole.SelectedIndex = 0; ddlAdministrationRole.SelectedIndex = 0; chkIsActive.Checked = true;
        }

        private static int? SelectedInt(ListControl list) => string.IsNullOrWhiteSpace(list.SelectedValue) ? (int?)null : Convert.ToInt32(list.SelectedValue);
        private static void Select(ListControl list, int? value) { list.ClearSelection(); var item = list.Items.FindByValue(value.HasValue ? value.Value.ToString() : string.Empty); if (item != null) item.Selected = true; }
        private static string ExpiryValue(DateTime? expiry) { if (!expiry.HasValue) return "0"; var days = (expiry.Value - DateTime.UtcNow).TotalDays; return days <= 30 ? "30" : days <= 60 ? "60" : "90"; }
        private void Show(string message, bool error) { lblMessage.CssClass = error ? "message error" : "message"; lblMessage.Text = message; }
    }
}
