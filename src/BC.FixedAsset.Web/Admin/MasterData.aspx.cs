using BC.FixedAsset.Data;
using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace BC.FixedAsset.Web.Admin
{
    public partial class MasterData : SecurePage
    {
        protected DropDownList ddlKind, ddlParent;
        protected GridView gridItems;
        protected Button btnNew, btnCancel, btnSave, btnDelete;
        protected Label lblMessage, lblEditorError;
        protected Literal litCount, litEditorTitle, litEditorKind, litNumericLabel;
        protected Panel pnlEditor, pnlSecondary, pnlParent, pnlNumeric, pnlActive;
        protected TextBox txtCode, txtName, txtSecondary, txtNumeric;
        protected CheckBox chkActive;
        protected HiddenField hidId, hidDeleteId;
        private readonly MasterDataRepository repository = new MasterDataRepository();
        private static readonly string[] KindKeys = { "Company", "Division", "Department", "Category", "Uom", "Condition", "Building", "Floor", "Room" };

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (!new ApplicationRepository().HasRole(CurrentUser.Id, "ADMIN", "SYSTEM_ADMIN"))
                Response.Redirect("~/Portal/Default.aspx?denied=1", true);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            foreach (var key in KindKeys)
            {
                var kind = MasterDataRepository.GetKind(key);
                ddlKind.Items.Add(new ListItem(kind.Label, key));
            }
            BindGrid();
        }

        protected void Kind_Changed(object sender, EventArgs e)
        {
            pnlEditor.Visible = false;
            lblMessage.Text = "";
            BindGrid();
        }

        private void BindGrid()
        {
            var kind = MasterDataRepository.GetKind(ddlKind.SelectedValue);
            var items = repository.GetItems(kind.Key);
            gridItems.Columns[2].Visible = kind.SecondaryColumn != null;
            gridItems.Columns[3].Visible = kind.ParentColumn != null;
            gridItems.Columns[4].Visible = kind.NumericColumn != null;
            gridItems.Columns[5].Visible = kind.HasActive;
            gridItems.DataSource = items;
            gridItems.DataBind();
            litCount.Text = items.Rows.Count + " records";
        }

        private void PrepareEditor()
        {
            var kind = MasterDataRepository.GetKind(ddlKind.SelectedValue);
            pnlSecondary.Visible = kind.SecondaryColumn != null;
            pnlParent.Visible = kind.ParentColumn != null;
            pnlNumeric.Visible = kind.NumericColumn != null;
            pnlActive.Visible = kind.HasActive;
            txtCode.MaxLength = kind.CodeLength;
            txtName.MaxLength = kind.NameLength;
            litEditorKind.Text = Server.HtmlEncode(kind.Label);
            litNumericLabel.Text = kind.Key == "Category" ? "Useful life (months)" : "Display order";
            if (kind.ParentColumn != null)
            {
                ddlParent.DataSource = repository.GetParents(kind.Key);
                ddlParent.DataTextField = "Name";
                ddlParent.DataValueField = "Id";
                ddlParent.DataBind();
                if (!kind.ParentRequired) ddlParent.Items.Insert(0, new ListItem("— None —", ""));
            }
            pnlEditor.Visible = true;
        }

        protected void New_Click(object sender, EventArgs e)
        {
            lblEditorError.Text = "";
            hidId.Value = "0";
            txtCode.Text = txtName.Text = txtSecondary.Text = "";
            txtNumeric.Text = "0";
            chkActive.Checked = true;
            litEditorTitle.Text = "เพิ่มข้อมูล";
            PrepareEditor();
        }

        protected void Grid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "EditItem") return;
            try
            {
                lblEditorError.Text = "";
                var id = Convert.ToInt32(e.CommandArgument);
                var rows = repository.GetItems(ddlKind.SelectedValue).Select("Id=" + id);
                if (rows.Length != 1) throw new InvalidOperationException("ไม่พบข้อมูล");
                var row = rows[0];
                hidId.Value = id.ToString();
                txtCode.Text = Convert.ToString(row["Code"]);
                txtName.Text = Convert.ToString(row["Name"]);
                txtSecondary.Text = Convert.ToString(row["SecondaryName"]);
                txtNumeric.Text = row["NumericValue"] == DBNull.Value ? "0" : Convert.ToString(row["NumericValue"]);
                chkActive.Checked = Convert.ToBoolean(row["IsActive"]);
                litEditorTitle.Text = "แก้ไขข้อมูล";
                PrepareEditor();
                if (row["ParentId"] != DBNull.Value)
                {
                    var item = ddlParent.Items.FindByValue(Convert.ToString(row["ParentId"]));
                    if (item != null) ddlParent.SelectedValue = item.Value;
                }
            }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); }
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            try
            {
                var kind = MasterDataRepository.GetKind(ddlKind.SelectedValue);
                int id, parent, number = 0;
                if (!int.TryParse(hidId.Value, out id) || id < 0) throw new ArgumentException("Invalid record ID.");
                if (kind.NumericColumn != null && !int.TryParse(txtNumeric.Text, out number)) throw new ArgumentException("Enter a valid number.");
                int? parentId = null;
                if (kind.ParentColumn != null && int.TryParse(ddlParent.SelectedValue, out parent)) parentId = parent;
                repository.Save(kind.Key, new MasterDataRecord
                {
                    Id = id, Code = txtCode.Text.Trim(), Name = txtName.Text.Trim(),
                    SecondaryName = txtSecondary.Text.Trim(), ParentId = parentId,
                    NumericValue = number, IsActive = chkActive.Checked
                });
                pnlEditor.Visible = false;
                lblEditorError.Text = "";
                BindGrid();
                lblMessage.Text = "บันทึกข้อมูลเรียบร้อย";
            }
            catch (Exception ex)
            {
                pnlEditor.Visible = true;
                lblEditorError.Text = Server.HtmlEncode(ex.Message);
            }
        }

        protected void Delete_Click(object sender, EventArgs e)
        {
            try
            {
                int id;
                if (!int.TryParse(hidDeleteId.Value, out id)) throw new ArgumentException("Invalid record ID.");
                repository.Delete(ddlKind.SelectedValue, id);
                hidDeleteId.Value = "";
                BindGrid();
                lblMessage.Text = "ลบข้อมูลเรียบร้อย";
            }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); }
        }

        protected void Cancel_Click(object sender, EventArgs e) { pnlEditor.Visible = false; }
    }
}
