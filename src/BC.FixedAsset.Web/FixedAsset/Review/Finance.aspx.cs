using BC.FixedAsset.Services;
using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace BC.FixedAsset.Web.FixedAsset.Review
{
    public partial class FinanceReview : SecurePage
    {
        protected GridView gridReview;
        protected TextBox txtReason;
        protected DropDownList ddlRoomFilter, ddlDepartmentFilter, ddlCustodianFilter;
        protected HiddenField hidReturnId, hidApproveId;
        protected Button btnReturn, btnApprove, btnCloseDetail, btnApproveFromDetail, btnPreviousPage, btnNextPage, btnFilter;
        protected Label lblMessage, lblPageInfo;
        protected Panel pnlDetail;
        protected Literal litDetail;
        protected Repeater repImages;
        private readonly AssetSurveyService service = new AssetSurveyService();
        private const int PageSize = 20;
        private int CurrentPage { get => ViewState["FinancePage"] == null ? 0 : Convert.ToInt32(ViewState["FinancePage"]); set => ViewState["FinancePage"] = Math.Max(0, value); }

        protected void Page_Load(object sender, EventArgs e) { if (!IsPostBack) { BindFilters(); Bind(); } }
        protected void Filter_Click(object sender, EventArgs e) { CurrentPage = 0; Bind(); }
        private void Bind()
        {
            int totalRows;
            var table = service.SearchPage("", "FinanceReview", AssetAccess, ddlRoomFilter.SelectedValue, ddlDepartmentFilter.SelectedValue, ddlCustodianFilter.SelectedValue, "", "", CurrentPage, PageSize, "FinanceReview", out totalRows);
            if (table.Rows.Count == 0 && CurrentPage > 0) { CurrentPage--; Bind(); return; }
            gridReview.DataSource = table; gridReview.DataBind();
            var totalPages = totalRows == 0 ? 0 : (int)Math.Ceiling(totalRows / (double)PageSize);
            lblPageInfo.Text = totalRows == 0 ? "ไม่พบข้อมูล" : string.Format("หน้า {0} / {1} · ทั้งหมด {2:N0} รายการ", CurrentPage + 1, totalPages, totalRows);
            btnPreviousPage.Enabled = CurrentPage > 0; btnNextPage.Enabled = CurrentPage + 1 < totalPages;
        }
        protected void PreviousPage_Click(object sender, EventArgs e) { CurrentPage--; Bind(); }
        protected void NextPage_Click(object sender, EventArgs e) { CurrentPage++; Bind(); }
        private void BindFilters()
        {
            var table = service.SurveyFilterOptions(AssetAccess, "FinanceReview");
            BindFilter(ddlRoomFilter, table, "Room", "ทุกห้อง"); BindFilter(ddlDepartmentFilter, table, "Department", "ทุกแผนก"); BindFilter(ddlCustodianFilter, table, "Custodian", "ผู้ดูแลทั้งหมด");
        }
        private static void BindFilter(DropDownList list, DataTable table, string filterType, string allText)
        {
            list.Items.Clear(); list.Items.Add(new ListItem(allText, ""));
            var values = new System.Collections.Generic.SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (DataRow row in table.Rows) { if (!string.Equals(Convert.ToString(row["FilterType"]), filterType, StringComparison.Ordinal)) continue; var value = Convert.ToString(row["Value"]).Trim(); if (value.Length > 0) values.Add(value); }
            foreach (var value in values) list.Items.Add(new ListItem(value, value));
        }

        protected void Grid_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var detail = e.Row.FindControl("btnDetail") as LinkButton;
            if (detail == null) return;
            e.Row.CssClass = "clickable-survey-row";
            e.Row.Attributes["tabindex"] = "0";
            e.Row.Attributes["role"] = "button";
            e.Row.Attributes["aria-label"] = "ดูรายละเอียดรายการ " + Convert.ToString(DataBinder.Eval(e.Row.DataItem, "SurveyNo"));
            e.Row.Attributes["data-detail-trigger"] = detail.ClientID;
        }

        protected void Grid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Detail") return;
            try { ShowDetail(Convert.ToInt64(e.CommandArgument)); }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); }
        }

        private void ShowDetail(long id)
        {
            var table = service.Detail(id, AssetAccess);
            if (table.Rows.Count == 0) throw new InvalidOperationException("ไม่พบข้อมูลหรือไม่มีสิทธิ์ตรวจสอบรายการนี้");
            var row = table.Rows[0];
            hidApproveId.Value = id.ToString();
            var photos = service.Attachments(id, AssetAccess);
            repImages.DataSource = photos.Count > 0 ? (object)photos : new[] { new { AttachmentId = (object)DBNull.Value, AttachmentType = "NONE" } };
            repImages.DataBind();
            litDetail.Text = DetailHtml(row);
            pnlDetail.Visible = true;
        }

        protected void CloseDetail_Click(object sender, EventArgs e) { pnlDetail.Visible = false; }
        protected void ApproveFromDetail_Click(object sender, EventArgs e) { RegisterCurrent(); }
        protected void Approve_Click(object sender, EventArgs e) { RegisterCurrent(); }

        private void RegisterCurrent()
        {
            try
            {
                long id;
                if (!long.TryParse(hidApproveId.Value, out id)) throw new ArgumentException("ไม่พบรายการสำหรับขึ้นทะเบียน");
                service.Approve(id, AssetAccess);
                hidApproveId.Value = "";
                pnlDetail.Visible = false;
                Bind();
                lblMessage.Text = "ขึ้นทะเบียนเรียบร้อย";
            }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); }
        }

        protected void Return_Click(object sender, EventArgs e)
        {
            try
            {
                long id;
                if (!long.TryParse(hidReturnId.Value, out id) || string.IsNullOrWhiteSpace(txtReason.Text)) throw new ArgumentException("กรุณาระบุเหตุผลสำหรับ Return");
                service.Return(id, txtReason.Text.Trim(), AssetAccess);
                hidReturnId.Value = ""; txtReason.Text = ""; pnlDetail.Visible = false; Bind(); lblMessage.Text = "Return เรียบร้อย";
            }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); ClientScript.RegisterStartupScript(GetType(), "returnDialog", "document.getElementById('returnDialog').hidden=false;", true); }
        }

        protected string AttachmentUrl(object id) => id == null || id == DBNull.Value ? ResolveUrl("~/Assets/asset-placeholder.svg") : ResolveUrl("~/Attachment.ashx?id=" + id);
        protected string PhotoLabel(object type) { var value = Convert.ToString(type); return value == "ACTUAL" ? "รูปทรัพย์สิน" : value == "SERIAL" ? "รูป Serial Number" : value == "NONE" ? "ยังไม่มีรูปภาพ" : "รูปประกอบอื่น ๆ"; }
        private string DetailHtml(DataRow r) =>
            "<div class='review-detail-summary'><div><small>เลขที่สำรวจ</small><strong>" + Display(r["SurveyNo"]) + "</strong></div><div><small>มูลค่าประมาณการ</small><strong>" + Display(Money(r["EstimatedValue"])) + "</strong></div></div>" +
            "<div class='detail-section'><h3>ข้อมูลทรัพย์สิน</h3><dl class='detail-grid refined'>" + Field("ชื่อทรัพย์สิน", r["AssetName"]) + Field("ประเภท", r["CategoryName"]) + Field("ยี่ห้อ", r["Brand"]) + Field("รุ่น / รายละเอียด", r["ModelDescription"]) + Field("Serial Number", r["SerialNumber"]) + Field("สภาพ", r["ConditionName"]) + Field("จำนวน", Convert.ToString(r["Quantity"]) + " " + r["UomCode"]) + Field("ขนาด (ก × ย × ส)", Dimension(r)) + "</dl></div>" +
            "<div class='detail-section'><h3>ผู้ดูแลและสถานที่</h3><dl class='detail-grid refined'>" + Field("ผู้ดูแลทรัพย์สิน", r["Custodian"]) + Field("ฝ่าย / แผนก", r["DepartmentName"]) + Field("สถานที่", Location(r)) + Field("ผู้สร้างรายการ", r["CreatedByName"]) + "</dl></div>" +
            "<div class='detail-section'><h3>ข้อมูลการรับเข้าและการเงิน</h3><dl class='detail-grid refined'>" + Field("PO Number", r["PurchaseOrderNo"]) + Field("วันที่รับเข้า", DateValue(r["ReceivedDate"])) + Field("มูลค่าประมาณการ", Money(r["EstimatedValue"])) + Field("หมายเหตุ", r["Remark"]) + "</dl></div>";
        private string Field(string label, object value) => "<div><dt>" + H(label) + "</dt><dd>" + Display(value) + "</dd></div>";
        private string Display(object value) { var text = Convert.ToString(value); return string.IsNullOrWhiteSpace(text) ? "<span class='empty-value'>—</span>" : H(text); }
        private string H(object value) => Server.HtmlEncode(Convert.ToString(value));
        private static string Dimension(DataRow r) => string.Format("{0} × {1} × {2} cm", Value(r["WidthCm"]), Value(r["LengthCm"]), Value(r["HeightCm"]));
        private static string Location(DataRow r) => string.Join(" / ", Array.FindAll(new[] { Convert.ToString(r["BuildingName"]), Convert.ToString(r["FloorName"]), Convert.ToString(r["RoomName"]) }, x => !string.IsNullOrWhiteSpace(x)));
        private static string Value(object value) => value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)) ? "-" : Convert.ToString(value);
        private static string DateValue(object value) => value == DBNull.Value ? "" : Convert.ToDateTime(value).ToString("dd MMM yyyy");
        private static string Money(object value) => value == DBNull.Value ? "" : "฿" + Convert.ToDecimal(value).ToString("N2");
    }
}
