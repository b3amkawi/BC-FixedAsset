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
                litDetail.Text = "<dl class='detail-grid'>" +
                    Field("Fixed Asset ID", r["FixedAssetNo"]) + Field("ทรัพย์สิน", r["AssetName"]) +
                    Field("Category", r["CategoryName"]) + Field("Serial", r["SerialNumber"]) +
                    Field("Brand", r["Brand"]) + Field("Model", r["ModelDescription"]) +
                    Field("Department", r["DepartmentName"]) + Field("Custodian", r["Custodian"]) +
                    Field("สถานที่", Convert.ToString(r["BuildingName"]) + " / " + r["FloorName"] + " / " + r["RoomName"]) +
                    Field("Quantity", Convert.ToString(r["Quantity"]) + " " + r["UomCode"]) +
                    Field("PO", r["PurchaseOrderNo"]) + Field("Cost", r["AcquisitionCost"]) +
                    Field("Status", r["AssetStatus"]) + "</dl>";
                pnlDetail.Visible = true;
            }
            catch (Exception ex) { lblMessage.Text = Server.HtmlEncode(ex.Message); }
        }

        protected void Close_Click(object sender, EventArgs e) { pnlDetail.Visible = false; }
        protected string AttachmentUrl(object id) => ResolveUrl("~/Attachment.ashx?id=" + id);
        private string Field(string label, object value) => "<dt>" + Server.HtmlEncode(label) + "</dt><dd>" + Server.HtmlEncode(Convert.ToString(value)) + "</dd>";
    }
}
