using BC.FixedAsset.Services;
using System;
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
        protected Literal litDetail;
        protected Repeater repImages;
        private readonly AssetSurveyService service = new AssetSurveyService();

        protected void Page_Load(object sender, EventArgs e) { if (!IsPostBack) Bind(); }
        protected void Search_Click(object sender, EventArgs e) => Bind();
        private void Bind() { gridAssets.DataSource = service.GetRegister(txtSearch.Text.Trim()); gridAssets.DataBind(); }

        protected void Grid_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var detail = e.Row.FindControl("btnDetail") as LinkButton;
            if (detail == null) return;
            e.Row.CssClass = "clickable-survey-row";
            e.Row.Attributes["tabindex"] = "0";
            e.Row.Attributes["role"] = "button";
            e.Row.Attributes["aria-label"] = "ดูรายละเอียดทรัพย์สิน " + Convert.ToString(DataBinder.Eval(e.Row.DataItem, "FixedAssetNo"));
            e.Row.Attributes["data-detail-trigger"] = detail.ClientID;
        }

        protected void Grid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
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
