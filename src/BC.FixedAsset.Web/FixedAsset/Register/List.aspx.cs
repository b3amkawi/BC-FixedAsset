using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Services;
using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace BC.FixedAsset.Web.FixedAsset.Register
{
    public partial class AssetRegister : SecurePage
    {
        protected TextBox txtSearch;
        protected GridView gridAssets;
        protected Button btnSearch, btnClose;
        protected Label lblMessage;
        protected Panel pnlDetail;
        protected Panel pnlEdit;
        protected Literal litDetail;
        protected Repeater repImages;
        protected HiddenField hidEditId, hidRowVersion;
        protected TextBox txtAssetName, txtBrand, txtModel, txtSerial, txtCustodian, txtQuantity, txtStatus, txtReceivedDate, txtPurchaseOrder, txtCost;
        protected DropDownList ddlCategory, ddlDepartment, ddlUom, ddlBuilding, ddlFloor, ddlRoom;
        private readonly AssetSurveyService service = new AssetSurveyService();
        private bool? canEdit;
        private bool CanEdit => canEdit ?? (canEdit = AssetAccess.IsSystemAdministrator).Value;

        protected void Page_Load(object sender, EventArgs e) { if (!IsPostBack) Bind(); }
        protected void Search_Click(object sender, EventArgs e) => Bind();
        private void Bind() { gridAssets.DataSource = service.GetRegister(txtSearch.Text.Trim()); gridAssets.DataBind(); }

        protected void Grid_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var detail = e.Row.FindControl("btnDetail") as LinkButton;
            var edit = e.Row.FindControl("btnEditRow") as LinkButton;
            if (edit != null) edit.Visible = CanEdit;
            if (detail == null) return;
            e.Row.CssClass = "clickable-survey-row";
            e.Row.Attributes["tabindex"] = "0";
            e.Row.Attributes["role"] = "button";
            e.Row.Attributes["aria-label"] = "ดูรายละเอียดทรัพย์สิน " + Convert.ToString(DataBinder.Eval(e.Row.DataItem, "FixedAssetNo"));
            e.Row.Attributes["data-detail-trigger"] = detail.ClientID;
        }

        protected void Grid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditAsset")
            {
                try { OpenEdit(Convert.ToInt64(e.CommandArgument)); }
                catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); }
                return;
            }
            if (e.CommandName != "Detail") return;
            try
            {
                var details = service.GetRegisterDetail(Convert.ToInt64(e.CommandArgument));
                if (details.Rows.Count == 0) throw new InvalidOperationException("ไม่พบข้อมูลทรัพย์สิน");
                var r = details.Rows[0];
                var sourceId = r["SourceSurveyId"] == DBNull.Value ? 0 : Convert.ToInt64(r["SourceSurveyId"]);
                repImages.DataSource = sourceId > 0 ? service.Attachments(sourceId, AssetAccess) : null;
                repImages.DataBind();
                var location = JoinLocation(r["BuildingName"], r["FloorName"], r["RoomName"]);
                litDetail.Text = "<div class='register-hero'><div><span class='asset-number'>" + H(r["FixedAssetNo"]) + "</span><h3>" + H(r["AssetName"]) + "</h3><p>" + H(r["CategoryName"]) + " · " + H(r["Brand"]) + " " + H(r["ModelDescription"]) + "</p></div><span class='status-pill register-status'>" + H(r["AssetStatus"]) + "</span></div>" +
                    "<div class='register-summary'><div><small>ผู้ดูแลทรัพย์สิน</small><strong>" + Display(r["Custodian"]) + "</strong></div><div><small>สถานที่ปัจจุบัน</small><strong>" + H(location) + "</strong></div><div><small>จำนวน</small><strong>" + H(r["Quantity"]) + " " + H(r["UomCode"]) + "</strong></div></div>" +
                    "<div class='detail-section'><h3>ข้อมูลระบุตัวตน</h3><dl class='detail-grid refined'>" + Field("Serial Number", r["SerialNumber"]) + Field("ยี่ห้อ / Brand", r["Brand"]) + Field("รุ่น / Model", r["ModelDescription"]) + Field("ประเภท / Category", r["CategoryName"]) + "</dl></div>" +
                    "<div class='detail-section'><h3>หน่วยงานและการจัดซื้อ</h3><dl class='detail-grid refined'>" + Field("ฝ่าย / แผนก", r["DepartmentName"]) + Field("PO Number", r["PurchaseOrderNo"]) + Field("วันที่รับเข้า", DateValue(r["ReceivedDate"])) + Field("มูลค่าทรัพย์สิน", Money(r["AcquisitionCost"])) + "</dl></div>";
                pnlDetail.Visible = true;
            }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); }
        }

        protected void Close_Click(object sender, EventArgs e) { pnlDetail.Visible = false; }
        private void OpenEdit(long id)
        {
            var asset = service.GetRegisterForEdit(id, AssetAccess);
            if (asset == null) throw new InvalidOperationException("ไม่พบข้อมูลทรัพย์สิน");
            BindReference(ddlCategory, "Category", false);
            BindReference(ddlDepartment, "Department", false);
            BindReference(ddlUom, "Uom", false);
            BindReference(ddlBuilding, "Building", true);
            BindReference(ddlFloor, "Floor", true);
            BindReference(ddlRoom, "Room", true);
            hidEditId.Value = id.ToString(CultureInfo.InvariantCulture);
            hidRowVersion.Value = Convert.ToBase64String(asset.RowVersion);
            txtAssetName.Text = asset.AssetName;
            txtBrand.Text = asset.Brand;
            txtModel.Text = asset.ModelDescription;
            txtSerial.Text = asset.SerialNumber;
            txtCustodian.Text = asset.CustodianName;
            txtQuantity.Text = asset.Quantity.ToString(CultureInfo.InvariantCulture);
            txtStatus.Text = asset.AssetStatus;
            txtReceivedDate.Text = asset.ReceivedDate.HasValue ? asset.ReceivedDate.Value.ToString("yyyy-MM-dd") : string.Empty;
            txtPurchaseOrder.Text = asset.PurchaseOrderNo;
            txtCost.Text = asset.AcquisitionCost.HasValue ? asset.AcquisitionCost.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
            Select(ddlCategory, asset.CategoryId);
            Select(ddlDepartment, asset.DepartmentId);
            Select(ddlUom, asset.UomId);
            Select(ddlBuilding, asset.BuildingId);
            Select(ddlFloor, asset.FloorId);
            Select(ddlRoom, asset.RoomId);
            pnlDetail.Visible = false;
            pnlEdit.Visible = true;
        }
        private void BindReference(DropDownList list, string type, bool optional)
        {
            list.DataSource = service.GetReference(type);
            list.DataTextField = "Name";
            list.DataValueField = "Id";
            list.DataBind();
            if (optional) list.Items.Insert(0, new ListItem("—", string.Empty));
        }
        private static void Select(DropDownList list, int? value)
        {
            var item = list.Items.FindByValue(value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
            if (item != null) { list.ClearSelection(); item.Selected = true; }
        }
        private static int? Selected(DropDownList list) => string.IsNullOrEmpty(list.SelectedValue) ? (int?)null : Convert.ToInt32(list.SelectedValue, CultureInfo.InvariantCulture);
        protected void SaveEdit_Click(object sender, EventArgs e)
        {
            if (!CanEdit) { Response.StatusCode = 403; return; }
            try
            {
                decimal quantity, cost;
                DateTime received;
                if (!decimal.TryParse(txtQuantity.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity) || quantity <= 0)
                    throw new ArgumentException("จำนวนต้องมากกว่า 0");
                decimal? acquisitionCost = null;
                if (!string.IsNullOrWhiteSpace(txtCost.Text))
                {
                    if (!decimal.TryParse(txtCost.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out cost) || cost < 0)
                        throw new ArgumentException("มูลค่าทรัพย์สินไม่ถูกต้อง");
                    acquisitionCost = cost;
                }
                DateTime? receivedDate = null;
                if (!string.IsNullOrWhiteSpace(txtReceivedDate.Text))
                {
                    if (!DateTime.TryParseExact(txtReceivedDate.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out received))
                        throw new ArgumentException("วันที่รับเข้าไม่ถูกต้อง");
                    receivedDate = received;
                }
                service.UpdateRegister(new RegisteredAssetEdit
                {
                    FixedAssetId = Convert.ToInt64(hidEditId.Value, CultureInfo.InvariantCulture),
                    RowVersion = Convert.FromBase64String(hidRowVersion.Value),
                    AssetName = txtAssetName.Text.Trim(), CategoryId = Selected(ddlCategory) ?? 0,
                    Brand = txtBrand.Text.Trim(), ModelDescription = txtModel.Text.Trim(), SerialNumber = txtSerial.Text.Trim(),
                    DepartmentId = Selected(ddlDepartment) ?? 0, CustodianName = txtCustodian.Text.Trim(),
                    BuildingId = Selected(ddlBuilding), FloorId = Selected(ddlFloor), RoomId = Selected(ddlRoom),
                    Quantity = quantity, UomId = Selected(ddlUom) ?? 0, AssetStatus = txtStatus.Text.Trim(),
                    ReceivedDate = receivedDate, PurchaseOrderNo = txtPurchaseOrder.Text.Trim(), AcquisitionCost = acquisitionCost
                }, AssetAccess);
                pnlEdit.Visible = false;
                Bind();
                lblMessage.Text = "บันทึกข้อมูลทะเบียนทรัพย์สินเรียบร้อย";
            }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); pnlEdit.Visible = true; }
        }
        protected void CancelEdit_Click(object sender, EventArgs e) { pnlEdit.Visible = false; }
        protected string AttachmentUrl(object id) => id == null || id == DBNull.Value ? ResolveUrl("~/Assets/asset-placeholder.svg") : ResolveUrl("~/Attachment.ashx?id=" + id);
        protected string PhotoLabel(object type) { var value = Convert.ToString(type); return value == "ACTUAL" ? "รูปทรัพย์สิน" : value == "SERIAL" ? "รูป Serial Number" : "รูปประกอบอื่น ๆ"; }
        private string Field(string label, object value) => "<div><dt>" + H(label) + "</dt><dd>" + Display(value) + "</dd></div>";
        private string Display(object value) { var text = Convert.ToString(value); return string.IsNullOrWhiteSpace(text) ? "<span class='empty-value'>—</span>" : H(text); }
        private string H(object value) => Server.HtmlEncode(Convert.ToString(value));
        private string JoinLocation(params object[] values) { var parts = Array.ConvertAll(values, x => Convert.ToString(x)); return string.Join(" / ", Array.FindAll(parts, x => !string.IsNullOrWhiteSpace(x))); }
        private string DateValue(object value) => value == DBNull.Value ? "" : Convert.ToDateTime(value).ToString("dd MMM yyyy");
        private string Money(object value) => value == DBNull.Value ? "" : "฿" + Convert.ToDecimal(value).ToString("N2");
    }
}
