using BC.FixedAsset.Services;
using System;
using System.Web;

namespace BC.FixedAsset.Web.Portal
{
    public partial class PortalHome : SecurePage
    {
        protected global::System.Web.UI.WebControls.Repeater rptApplications;
        protected global::System.Web.UI.WebControls.Panel pnlEmpty;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return; var items = new PortalService().GetApplications(CurrentUser.Id); rptApplications.DataSource = items; rptApplications.DataBind(); pnlEmpty.Visible = items.Count == 0;
        }
        protected string GetTargetUrl(object value) => HttpUtility.HtmlAttributeEncode(ResolveUrl(Convert.ToString(value)));
        protected string GetIconMarkup(object pathValue, object textValue)
        {
            var path=Convert.ToString(pathValue);
            if(string.IsNullOrWhiteSpace(path)) return HttpUtility.HtmlEncode(Convert.ToString(textValue));
            return "<img src=\""+HttpUtility.HtmlAttributeEncode(ResolveUrl(path))+"\" alt=\"\" />";
        }
    }
}
