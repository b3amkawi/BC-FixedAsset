using BC.FixedAsset.Core.Models;
using BC.FixedAsset.Services;
using System;
using System.Web.UI.WebControls;

namespace BC.FixedAsset.Web.FixedAsset
{
    public partial class Numbering : SecurePage
    {
        protected HiddenField hidSchemeId;
        protected TextBox txtPattern, txtDigits, txtStart;
        protected DropDownList ddlReset, ddlScope;
        protected Button btnSave;
        protected Label lblMessage;
        private readonly AssetNumberService service = new AssetNumberService();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            var scheme=service.GetScheme();if(scheme==null)return;
            hidSchemeId.Value=scheme.SchemeId.ToString();txtPattern.Text=scheme.Pattern;txtDigits.Text=scheme.SequenceDigits.ToString();
            txtStart.Text=scheme.StartValue.ToString();ddlReset.SelectedValue=scheme.ResetPolicy;ddlScope.SelectedValue=scheme.SequenceScope;
        }

        protected void Save_Click(object sender,EventArgs e)
        {
            try
            {
                service.SaveScheme(new AssetNumberScheme{SchemeId=Convert.ToInt32(hidSchemeId.Value),Pattern=txtPattern.Text.Trim(),SequenceDigits=Convert.ToInt32(txtDigits.Text),StartValue=Convert.ToInt32(txtStart.Text),ResetPolicy=ddlReset.SelectedValue,SequenceScope=ddlScope.SelectedValue},CurrentUser.Id);
                lblMessage.CssClass="message";lblMessage.Text="บันทึก Numbering Scheme เรียบร้อย / Numbering scheme saved.";
            }
            catch(Exception ex){lblMessage.CssClass="message";lblMessage.Text=Server.HtmlEncode(ex.Message);}
        }
    }
}
