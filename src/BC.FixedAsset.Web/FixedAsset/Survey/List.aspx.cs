using BC.FixedAsset.Services;
using System;
using System.Data;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;

namespace BC.FixedAsset.Web.FixedAsset.Survey
{
    public partial class SurveyList : SecurePage
    {
        protected TextBox txtSearch;
        protected DropDownList ddlStatus, ddlRoomFilter, ddlDepartmentFilter, ddlCustodianFilter;
        protected GridView gridSurvey;
        protected Button btnSearch, btnExport, btnImport, btnClose, btnPreviousPage, btnNextPage;
        protected FileUpload fileImport;
        protected Label lblMessage, lblPageInfo;
        protected Panel pnlDetail;
        protected Literal litDetail;
        protected Repeater repImages;
        private readonly AssetSurveyService service = new AssetSurveyService();

        protected void Page_Load(object sender, EventArgs e) { if (!IsPostBack) { BindFilters(); Bind(); } }
        private const int PageSize = 20;
        private int CurrentPage { get => ViewState["SurveyPage"] == null ? 0 : Convert.ToInt32(ViewState["SurveyPage"]); set => ViewState["SurveyPage"] = Math.Max(0, value); }
        protected void Search_Click(object sender, EventArgs e) { CurrentPage = 0; Bind(); }
        private DataTable Data() => service.Search(txtSearch.Text, ddlStatus.SelectedValue, AssetAccess);
        private string SortExpression { get => Convert.ToString(ViewState["SurveySortExpression"]); set => ViewState["SurveySortExpression"] = value; }
        private string SortDirection { get => Convert.ToString(ViewState["SurveySortDirection"]); set => ViewState["SurveySortDirection"] = value; }

        private void Bind()
        {
            int totalRows;
            var table = service.SearchPage(txtSearch.Text, ddlStatus.SelectedValue, AssetAccess, ddlRoomFilter.SelectedValue, ddlDepartmentFilter.SelectedValue, ddlCustodianFilter.SelectedValue, SortExpression, SortDirection, CurrentPage, PageSize, "", out totalRows);
            if (table.Rows.Count == 0 && CurrentPage > 0) { CurrentPage--; Bind(); return; }
            gridSurvey.DataSource = table;
            gridSurvey.DataBind();
            var totalPages = totalRows == 0 ? 0 : (int)Math.Ceiling(totalRows / (double)PageSize);
            lblPageInfo.Text = totalRows == 0 ? "ไม่พบข้อมูล" : string.Format("หน้า {0} / {1} · ทั้งหมด {2:N0} รายการ", CurrentPage + 1, totalPages, totalRows);
            btnPreviousPage.Enabled = CurrentPage > 0;
            btnNextPage.Enabled = CurrentPage + 1 < totalPages;
        }

        private DataView FilteredData()
        {
            var view = Data().DefaultView;
            var filters = new System.Collections.Generic.List<string>();
            AddFilter(filters, "RoomName", ddlRoomFilter.SelectedValue);
            AddFilter(filters, "DepartmentName", ddlDepartmentFilter.SelectedValue);
            AddFilter(filters, "Custodian", ddlCustodianFilter.SelectedValue);
            view.RowFilter = string.Join(" AND ", filters);
            return view;
        }

        private void BindFilters()
        {
            var table = service.SurveyFilterOptions(AssetAccess);
            BindFilter(ddlRoomFilter, table, "Room", "ทุกห้อง");
            BindFilter(ddlDepartmentFilter, table, "Department", "ทุกแผนก");
            BindFilter(ddlCustodianFilter, table, "Custodian", "ผู้ดูแลทั้งหมด");
        }

        private static void BindFilter(DropDownList list, DataTable table, string filterType, string allText)
        {
            list.Items.Clear(); list.Items.Add(new ListItem(allText, ""));
            var values = new System.Collections.Generic.SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (DataRow row in table.Rows) { if (!string.Equals(Convert.ToString(row["FilterType"]), filterType, StringComparison.Ordinal)) continue; var value = Convert.ToString(row["Value"]).Trim(); if (value.Length > 0) values.Add(value); }
            foreach (var value in values) list.Items.Add(new ListItem(value, value));
        }

        private static void AddFilter(System.Collections.Generic.ICollection<string> filters, string column, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) filters.Add("[" + column + "] = '" + value.Replace("'", "''") + "'");
        }

        protected void Grid_Sorting(object sender, GridViewSortEventArgs e)
        {
            SortDirection = SortExpression == e.SortExpression && SortDirection == "ASC" ? "DESC" : "ASC";
            SortExpression = e.SortExpression;
            CurrentPage = 0;
            Bind();
        }

        protected void PreviousPage_Click(object sender, EventArgs e) { CurrentPage--; Bind(); }
        protected void NextPage_Click(object sender, EventArgs e) { CurrentPage++; Bind(); }

        private static string SortClause(string expression, string direction)
        {
            var suffix = direction == "DESC" ? " DESC" : " ASC";
            switch (expression)
            {
                case "SurveyNo": return "SurveyNo" + suffix;
                case "AssetName": return "AssetName" + suffix + ", SerialNumber" + suffix;
                case "Dimensions": return "WidthCm" + suffix + ", LengthCm" + suffix + ", HeightCm" + suffix;
                case "WeightKg": return "WeightKg" + suffix;
                case "Quantity": return "Quantity" + suffix + ", UomCode" + suffix;
                case "Location": return "BuildingName" + suffix + ", FloorName" + suffix + ", RoomName" + suffix;
                case "Custodian": return "Custodian" + suffix;
                case "CreatedByName": return "CreatedByName" + suffix;
                default: return "SurveyNo ASC";
            }
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
            if (e.CommandName != "DeleteSurvey" && e.CommandName != "Detail") return;
            try
            {
                var id = Convert.ToInt64(e.CommandArgument);
                if (e.CommandName == "DeleteSurvey")
                {
                    service.Delete(id, AssetAccess, Server.MapPath(System.Configuration.ConfigurationManager.AppSettings["UploadRoot"]));
                    Bind(); lblMessage.Text = "ลบรายการเรียบร้อย";
                }
                else if (e.CommandName == "Detail") ShowDetail(id);
            }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); }
        }

        private void ShowDetail(long id)
        {
            var table = service.Detail(id, AssetAccess);
            if (table.Rows.Count == 0) throw new InvalidOperationException("ไม่พบข้อมูลหรือไม่มีสิทธิ์");
            var row = table.Rows[0];
            var photos = service.Attachments(id, AssetAccess);
            repImages.DataSource = photos.Count > 0 ? (object)photos : new[] { new { AttachmentId = (object)DBNull.Value, AttachmentType = "NONE" } };
            repImages.DataBind();
            litDetail.Text = DetailHtml(row);
            pnlDetail.Visible = true;
        }

        protected string AttachmentUrl(object id) => id == null || id == DBNull.Value ? ResolveUrl("~/Assets/asset-placeholder.svg") : ResolveUrl("~/Attachment.ashx?id=" + id);
        protected string PhotoLabel(object type) { var value = Convert.ToString(type); return value == "ACTUAL" ? "รูปทรัพย์สิน" : value == "SERIAL" ? "รูป Serial Number" : value == "NONE" ? "ยังไม่มีรูปภาพ" : "รูปประกอบอื่น ๆ"; }
        protected bool CanEdit(object status) => Convert.ToString(status) == "Draft" || Convert.ToString(status) == "Returned";
        protected string Dimension(object width, object length, object height) => string.Format("{0} × {1} × {2} cm", Value(width), Value(length), Value(height));

        private string DetailHtml(DataRow r) =>
            "<div class='review-detail-summary'><div><small>เลขที่สำรวจ</small><strong>" + Display(r["SurveyNo"]) + "</strong></div><div><small>สถานะ</small><strong>" + Display(r["Status"]) + "</strong></div></div>" +
            "<div class='detail-section'><h3>ข้อมูลทรัพย์สิน</h3><dl class='detail-grid refined'>" + Field("ชื่อทรัพย์สิน", r["AssetName"]) + Field("ประเภท", r["CategoryName"]) + Field("ยี่ห้อ", r["Brand"]) + Field("รุ่น / รายละเอียด", r["ModelDescription"]) + Field("Serial Number", r["SerialNumber"]) + Field("สภาพ", r["ConditionName"]) + Field("จำนวน", Convert.ToString(r["Quantity"]) + " " + r["UomCode"]) + Field("ขนาด (ก × ย × ส)", Dimension(r["WidthCm"], r["LengthCm"], r["HeightCm"])) + Field("น้ำหนัก", Weight(r["WeightKg"])) + Field("กรรมสิทธิ์", r["OwnershipType"]) + "</dl></div>" +
            "<div class='detail-section'><h3>ผู้ดูแลและสถานที่</h3><dl class='detail-grid refined'>" + Field("ผู้ดูแลทรัพย์สิน", r["Custodian"]) + Field("ฝ่าย / แผนก", r["DepartmentName"]) + Field("สถานที่", Location(r)) + Field("ผู้สร้างรายการ", r["CreatedByName"]) + "</dl></div>" +
            "<div class='detail-section'><h3>ข้อมูลการรับเข้าและ Workflow</h3><dl class='detail-grid refined'>" + Field("PO Number", r["PurchaseOrderNo"]) + Field("วันที่รับเข้า", DateValue(r["ReceivedDate"])) + Field("มูลค่าประมาณการ", Money(r["EstimatedValue"])) + Field("หมายเหตุ", r["Remark"]) + Field("เหตุผล Return", r["ReturnReason"]) + "</dl></div>";
        private string Field(string label, object value) => "<div><dt>" + H(label) + "</dt><dd>" + Display(value) + "</dd></div>";
        private string Display(object value) { var text = Convert.ToString(value); return string.IsNullOrWhiteSpace(text) ? "<span class='empty-value'>—</span>" : H(text); }
        private string H(object value) => Server.HtmlEncode(Convert.ToString(value));
        private static string Location(DataRow r) => string.Join(" / ", Array.FindAll(new[] { Convert.ToString(r["BuildingName"]), Convert.ToString(r["FloorName"]), Convert.ToString(r["RoomName"]) }, x => !string.IsNullOrWhiteSpace(x)));
        private static string Value(object value) => value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)) ? "-" : Convert.ToString(value);
        private static string Weight(object value) => value == DBNull.Value ? "" : Convert.ToString(value) + " kg";
        private static string DateValue(object value) => value == DBNull.Value ? "" : Convert.ToDateTime(value).ToString("dd MMM yyyy");
        private static string Money(object value) => value == DBNull.Value ? "" : "฿" + Convert.ToDecimal(value).ToString("N2");

        protected void Close_Click(object sender, EventArgs e) { pnlDetail.Visible = false; }
        protected void Import_Click(object sender, EventArgs e)
        {
            try
            {
                if (!fileImport.HasFile) throw new ArgumentException("กรุณาเลือกไฟล์ Excel");
                var result = new ExcelSurveyImporter(service, AssetAccess).Import(fileImport.PostedFile);
                CurrentPage = 0; BindFilters(); Bind();
                lblMessage.Text = Server.HtmlEncode(string.Format("นำเข้าสำเร็จ {0:N0} รายการ, ข้ามข้อมูลซ้ำ {1:N0} รายการ", result.Imported, result.Skipped));
            }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); }
        }
        protected void Export_Click(object sender, EventArgs e)
        {
            var table = FilteredData().ToTable(); Response.Clear(); Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition", "attachment;filename=AssetSurvey-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".xls");
            var content = new StringBuilder(); foreach (DataColumn column in table.Columns) content.Append(column.ColumnName).Append('\t'); content.AppendLine();
            foreach (DataRow row in table.Rows) { foreach (var value in row.ItemArray) content.Append(Convert.ToString(value).Replace("\t", " ")).Append('\t'); content.AppendLine(); }
            Response.ContentEncoding = Encoding.UTF8; Response.Write("\uFEFF" + content); Response.End();
        }
    }
}
