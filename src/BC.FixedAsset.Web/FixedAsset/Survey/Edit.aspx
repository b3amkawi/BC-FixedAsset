<%@ Page Title="Asset Survey Detail" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="BC.FixedAsset.Web.FixedAsset.Survey.SurveyEdit" %>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
<div class="survey-page">
  <div class="survey-page-head">
    <div><a class="back-link" href="List.aspx">← กลับไปหน้ารายการ</a><h1>บันทึกสำรวจทรัพย์สิน <span>/ Asset Survey</span></h1><p>ข้อมูลที่กรอกไว้จะไม่หายเมื่อพบช่องที่ยังไม่ครบ</p></div>
    <div class="draft-indicator"><span></span> บันทึกเป็น Draft ได้ทุกเวลา</div>
  </div>
  <asp:Label ID="lblMessage" runat="server" CssClass="message"/>
  <asp:HiddenField ID="hidSurveyId" runat="server" Value="0"/>
  <div class="survey-form-sections">
    <section class="form-section card">
      <div class="form-section-head"><span class="section-icon">01</span><div><h2>ข้อมูลทรัพย์สิน</h2><p>Asset information</p></div></div>
      <div class="form-section-body form-grid">
        <div class="field full"><label>ชื่อทรัพย์สิน / Asset name *</label><asp:TextBox ID="txtAssetName" runat="server" MaxLength="250" placeholder="ระบุชื่อทรัพย์สิน"/></div>
        <div class="field"><label>ประเภท / Category *</label><asp:DropDownList ID="ddlCategory" runat="server"/></div>
        <div class="field"><label>วันที่สำรวจ / Survey date</label><asp:TextBox ID="txtSurveyDate" runat="server" TextMode="Date"/></div>
        <div class="field"><label>ยี่ห้อ / Brand</label><asp:TextBox ID="txtBrand" runat="server" MaxLength="120"/></div>
        <div class="field"><label>รุ่น / รายละเอียด</label><asp:TextBox ID="txtModel" runat="server" MaxLength="300"/></div>
        <div class="field"><label>Serial Number</label><asp:TextBox ID="txtSerial" runat="server" MaxLength="150"/></div>
        <div class="field"><label>กรรมสิทธิ์ / Ownership</label><asp:DropDownList ID="ddlOwnership" runat="server"><asp:ListItem>Company Owned</asp:ListItem><asp:ListItem>Leased</asp:ListItem><asp:ListItem>Customer Owned</asp:ListItem></asp:DropDownList></div>
        <div class="field"><label>สภาพทรัพย์สิน / Condition</label><asp:DropDownList ID="ddlCondition" runat="server"/></div>
        <div class="field"><label>จำนวน / Quantity *</label><asp:TextBox ID="txtQuantity" runat="server" TextMode="Number" Text="1" step="0.01" min="0.01"/></div>
        <div class="field"><label>หน่วย / UOM *</label><asp:DropDownList ID="ddlUom" runat="server"/></div>
      </div>
    </section>
    <section class="form-section card">
      <div class="form-section-head"><span class="section-icon">02</span><div><h2>ผู้ดูแลและสถานที่</h2><p>Custodian &amp; location</p></div></div>
      <div class="form-section-body form-grid">
        <div class="field"><label>ฝ่าย / แผนก *</label><asp:DropDownList ID="ddlDepartment" runat="server"/></div>
        <div class="field"><label>ผู้ดูแลทรัพย์สิน / Custodian</label><asp:TextBox ID="txtCustodian" runat="server" MaxLength="250" placeholder="พิมพ์ชื่อผู้ดูแลทรัพย์สิน"/><small>กรอกชื่อได้อย่างอิสระ ไม่จำเป็นต้องเป็นผู้ใช้ในระบบ</small></div>
        <div class="field"><label>อาคาร / Building *</label><asp:DropDownList ID="ddlBuilding" runat="server"/></div>
        <div class="field"><label>ชั้น / Floor *</label><asp:DropDownList ID="ddlFloor" runat="server"/></div>
        <div class="field"><label>ห้อง / Room *</label><asp:DropDownList ID="ddlRoom" runat="server"/></div>
      </div>
    </section>
    <section class="form-section card">
      <div class="form-section-head"><span class="section-icon">03</span><div><h2>ขนาดและน้ำหนัก</h2><p>Dimensions &amp; weight</p></div></div>
      <div class="form-section-body form-grid dimension-grid">
        <div class="field"><label>กว้าง / Width (cm)</label><asp:TextBox ID="txtWidth" runat="server" TextMode="Number" step="0.01" min="0"/></div>
        <div class="field"><label>ยาว / Length (cm)</label><asp:TextBox ID="txtLength" runat="server" TextMode="Number" step="0.01" min="0"/></div>
        <div class="field"><label>สูง / Height (cm)</label><asp:TextBox ID="txtHeight" runat="server" TextMode="Number" step="0.01" min="0"/></div>
        <div class="field"><label>น้ำหนัก / Weight (kg)</label><asp:TextBox ID="txtWeight" runat="server" TextMode="Number" step="0.01" min="0"/></div>
        <div class="field full"><label>เหตุผลกรณีไม่มีข้อมูลขนาด</label><asp:TextBox ID="txtDimensionReason" runat="server" MaxLength="500" placeholder="ระบุเหตุผล หากไม่สามารถวัดขนาดได้"/></div>
      </div>
    </section>
    <section class="form-section card">
      <div class="form-section-head"><span class="section-icon">04</span><div><h2>ข้อมูลการรับเข้าและมูลค่า</h2><p>Acquisition &amp; value</p></div></div>
      <div class="form-section-body form-grid">
        <div class="field"><label>วันรับเข้า / Received date</label><asp:TextBox ID="txtReceivedDate" runat="server" TextMode="Date"/></div>
        <div class="field"><label>PO Number</label><asp:TextBox ID="txtPo" runat="server" MaxLength="100"/></div>
        <div class="field"><label>มูลค่าประมาณการ / Estimated value</label><asp:TextBox ID="txtEstimatedValue" runat="server" TextMode="Number" step="0.01" min="0"/></div>
        <div class="field full"><label>หมายเหตุ / Remark</label><asp:TextBox ID="txtRemark" runat="server" TextMode="MultiLine" Rows="3" MaxLength="1000"/></div>
      </div>
    </section>
    <section class="form-section card">
      <div class="form-section-head"><span class="section-icon">05</span><div><h2>รูปภาพประกอบ</h2><p>Photos are stored securely in the database</p></div></div>
      <div class="form-section-body image-upload-grid">
        <div class="image-field"><label>รูปของจริง / Actual asset</label><asp:Image ID="imgActual" runat="server" CssClass="upload-preview" AlternateText="รูปของจริง"/><span class="photo-action">📷 ถ่ายรูปหรือเลือกรูป</span><asp:FileUpload ID="fileActual" runat="server" accept="image/jpeg,image/png,image/webp" capture="environment"/><small>JPG, PNG, WebP · ไม่เกิน 5 MB</small></div>
        <div class="image-field"><label>รูป Serial / Serial photo</label><asp:Image ID="imgSerial" runat="server" CssClass="upload-preview" AlternateText="รูป Serial"/><span class="photo-action">📷 ถ่ายรูปหรือเลือกรูป</span><asp:FileUpload ID="fileSerial" runat="server" accept="image/jpeg,image/png,image/webp" capture="environment"/><small>JPG, PNG, WebP · ไม่เกิน 5 MB</small></div>
        <div class="image-field"><label>รูปอื่น ๆ / Other photo</label><asp:Image ID="imgOther" runat="server" CssClass="upload-preview" AlternateText="รูปอื่น ๆ"/><span class="photo-action">📷 ถ่ายรูปหรือเลือกรูป</span><asp:FileUpload ID="fileOther" runat="server" accept="image/jpeg,image/png,image/webp" capture="environment"/><small>JPG, PNG, WebP · ไม่เกิน 5 MB</small></div>
      </div>
    </section>
  </div>
  <div class="sticky-form-actions"><a class="button outline" href="List.aspx">ยกเลิก / Cancel</a><asp:Button ID="btnSave" runat="server" CssClass="button outline" Text="บันทึก Draft" OnClick="Save_Click"/><asp:Button ID="btnSubmit" runat="server" CssClass="button primary" Text="ส่งอนุมัติ / Submit" OnClick="Submit_Click"/></div>
</div>
<script>
(function(){
  var placeholder='<%= ResolveUrl("~/Assets/asset-placeholder.svg") %>';
  document.querySelectorAll('.image-field input[type=file]').forEach(function(input){var image=input.parentElement.querySelector('img');if(!image.getAttribute('src'))image.src=placeholder;input.addEventListener('change',function(){if(input.files&&input.files[0])image.src=URL.createObjectURL(input.files[0]);});});
  var building=document.getElementById('<%= ddlBuilding.ClientID %>'),floor=document.getElementById('<%= ddlFloor.ClientID %>'),room=document.getElementById('<%= ddlRoom.ClientID %>');
  function filter(list,parent){var first=null,selected=null;Array.prototype.forEach.call(list.options,function(option){var matches=option.getAttribute('data-parent')===parent;option.hidden=!matches;option.disabled=!matches;if(matches){if(!first)first=option;if(option.selected)selected=option;}});if(!selected)list.value=first?first.value:'';list.disabled=!first;}
  function updateRooms(){filter(room,floor.value);}function updateFloors(){filter(floor,building.value);updateRooms();}
  building.addEventListener('change',updateFloors);floor.addEventListener('change',updateRooms);updateFloors();
})();
</script>
</asp:Content>
