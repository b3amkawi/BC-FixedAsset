<%@ Page Title="Asset Register" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="List.aspx.cs" Inherits="BC.FixedAsset.Web.FixedAsset.Register.AssetRegister" %>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
<div class="page-head"><div><h1>Asset Register</h1><p>ทะเบียนทรัพย์สินกลางและสถานะปัจจุบัน</p></div></div><asp:Label ID="lblMessage" runat="server" CssClass="message"/>
<section class="card table-card"><div class="table-toolbar"><div class="search-box"><asp:TextBox ID="txtSearch" runat="server" placeholder="ค้นหา Asset ID, ชื่อ หรือ Serial"/><asp:Button ID="btnSearch" runat="server" CssClass="button outline compact" Text="ค้นหา" OnClick="Search_Click"/></div></div><div class="table-wrap"><asp:GridView ID="gridAssets" runat="server" AutoGenerateColumns="false" GridLines="None" OnRowCommand="Grid_RowCommand" OnRowDataBound="Grid_RowDataBound" CssClass="asset-table"><Columns>
<asp:TemplateField HeaderText="รูป"><ItemTemplate><img class="asset-thumb" src='<%# AttachmentUrl(Eval("ActualAttachmentId")) %>' alt="รูปทรัพย์สิน"/></ItemTemplate></asp:TemplateField>
<asp:TemplateField HeaderText="Fixed Asset ID"><ItemTemplate><%# Eval("FixedAssetNo") %><asp:LinkButton ID="btnDetail" runat="server" CssClass="row-detail-trigger" CommandName="Detail" CommandArgument='<%# Eval("FixedAssetId") %>' Text="ดูรายละเอียด"/></ItemTemplate></asp:TemplateField>
<asp:BoundField DataField="AssetName" HeaderText="Asset"/><asp:BoundField DataField="CategoryName" HeaderText="Category"/><asp:BoundField DataField="DepartmentName" HeaderText="Department"/><asp:BoundField DataField="Custodian" HeaderText="Custodian"/><asp:BoundField DataField="Quantity" HeaderText="Qty"/><asp:BoundField DataField="UomCode" HeaderText="UOM"/><asp:BoundField DataField="AssetStatus" HeaderText="Status"/>
<asp:TemplateField HeaderText="ดำเนินการ"><ItemTemplate><asp:HyperLink runat="server" CssClass="button small outline" NavigateUrl='<%# "../Tag.aspx?assetId="+Eval("FixedAssetId") %>' Target="_blank">Print Tag</asp:HyperLink> <asp:LinkButton ID="btnEditRow" runat="server" CssClass="button small outline" CommandName="EditAsset" CommandArgument='<%# Eval("FixedAssetId") %>' CausesValidation="false">แก้ไข</asp:LinkButton></ItemTemplate></asp:TemplateField>
</Columns></asp:GridView></div></section>
<asp:Panel ID="pnlDetail" runat="server" CssClass="modal-layer" Visible="false"><div class="modal-card register-detail-modal"><div class="modal-head register-detail-head"><div><span class="detail-eyebrow">FIXED ASSET REGISTER</span><h2>รายละเอียดทะเบียนทรัพย์สิน</h2><p class="muted">ข้อมูลสำคัญ รูปภาพ และตำแหน่งปัจจุบันในหน้าเดียว</p></div><asp:Button ID="btnClose" runat="server" Text="×" CssClass="icon-button" OnClick="Close_Click" CausesValidation="false"/></div><div class="detail-images register-gallery"><asp:Repeater ID="repImages" runat="server"><ItemTemplate><figure><img src='<%# AttachmentUrl(Eval("AttachmentId")) %>' alt='<%# Eval("AttachmentType") %>'/><figcaption><%# PhotoLabel(Eval("AttachmentType")) %></figcaption></figure></ItemTemplate></asp:Repeater></div><asp:Literal ID="litDetail" runat="server"/></div></asp:Panel>
<asp:Panel ID="pnlEdit" runat="server" CssClass="modal-layer" Visible="false"><div class="modal-card register-detail-modal" style="max-height:90vh;overflow:auto"><div class="modal-head"><h2>แก้ไขทะเบียนทรัพย์สิน</h2><asp:Button ID="btnCancelEdit" runat="server" Text="×" CssClass="icon-button" OnClick="CancelEdit_Click" CausesValidation="false"/></div><div class="card-body form-grid">
<asp:HiddenField ID="hidEditId" runat="server"/><asp:HiddenField ID="hidRowVersion" runat="server"/>
<div class="field"><label>ชื่อทรัพย์สิน *</label><asp:TextBox ID="txtAssetName" runat="server" MaxLength="250"/></div>
<div class="field"><label>ประเภท *</label><asp:DropDownList ID="ddlCategory" runat="server"/></div>
<div class="field"><label>ยี่ห้อ</label><asp:TextBox ID="txtBrand" runat="server" MaxLength="120"/></div>
<div class="field"><label>รุ่น</label><asp:TextBox ID="txtModel" runat="server" MaxLength="300"/></div>
<div class="field"><label>Serial Number</label><asp:TextBox ID="txtSerial" runat="server" MaxLength="150"/></div>
<div class="field"><label>แผนก *</label><asp:DropDownList ID="ddlDepartment" runat="server"/></div>
<div class="field"><label>ผู้ดูแลทรัพย์สิน</label><asp:TextBox ID="txtCustodian" runat="server" MaxLength="250"/></div>
<div class="field"><label>จำนวน *</label><asp:TextBox ID="txtQuantity" runat="server" TextMode="Number" step="0.01" min="0.01"/></div>
<div class="field"><label>หน่วย *</label><asp:DropDownList ID="ddlUom" runat="server"/></div>
<div class="field"><label>สถานะ *</label><asp:TextBox ID="txtStatus" runat="server" MaxLength="30"/></div>
<div class="field"><label>อาคาร</label><asp:DropDownList ID="ddlBuilding" runat="server"/></div>
<div class="field"><label>ชั้น</label><asp:DropDownList ID="ddlFloor" runat="server"/></div>
<div class="field"><label>ห้อง</label><asp:DropDownList ID="ddlRoom" runat="server"/></div>
<div class="field"><label>วันที่รับเข้า</label><asp:TextBox ID="txtReceivedDate" runat="server" TextMode="Date"/></div>
<div class="field"><label>PO Number</label><asp:TextBox ID="txtPurchaseOrder" runat="server" MaxLength="100"/></div>
<div class="field"><label>มูลค่าทรัพย์สิน</label><asp:TextBox ID="txtCost" runat="server" TextMode="Number" step="0.01" min="0"/></div>
<div class="field full actions"><asp:Button ID="btnSaveEdit" runat="server" CssClass="button primary" Text="บันทึกการแก้ไข" OnClick="SaveEdit_Click"/><asp:Button runat="server" CssClass="button" Text="ยกเลิก" OnClick="CancelEdit_Click" CausesValidation="false"/></div>
</div></div></asp:Panel>
</asp:Content>
