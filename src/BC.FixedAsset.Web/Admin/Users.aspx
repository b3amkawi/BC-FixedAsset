<%@ Page Title="Users & Roles" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="BC.FixedAsset.Web.Admin.Users" %>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server"><div class="admin-tabs"><a href="Applications.aspx">▣ Application Portal</a><a class="active" href="Users.aspx">♙ ผู้ใช้และสิทธิ์</a><a href="MasterData.aspx">▤ ข้อมูลกลาง</a><a href="../FixedAsset/Numbering.aspx">⚙ เลขทรัพย์สิน</a></div>
  <div class="page-head">
    <div><h1>Users &amp; Roles</h1><p>สร้าง แก้ไข ปิดใช้งาน ลบผู้ใช้ และกำหนดสิทธิ์แยกตาม Application</p></div>
    <div class="actions"><asp:Button ID="btnNew" runat="server" CssClass="button primary" Text="เพิ่มผู้ใช้ / Create User" OnClick="New_Click"/></div>
  </div>
  <asp:Label ID="lblMessage" runat="server"/>
  <section class="card table-card"><div class="table-wrap">
    <asp:GridView ID="gridUsers" runat="server" AutoGenerateColumns="false" GridLines="None" OnRowCommand="Grid_RowCommand"><Columns>
      <asp:BoundField DataField="UserName" HeaderText="User Login"/><asp:BoundField DataField="DisplayName" HeaderText="ชื่อผู้ใช้ / Name"/>
      <asp:BoundField DataField="Email" HeaderText="Email"/><asp:BoundField DataField="DepartmentName" HeaderText="Department"/>
      <asp:BoundField DataField="FixedAssetRole" HeaderText="Fixed Asset Role"/><asp:BoundField DataField="AdministrationRole" HeaderText="Admin Role"/>
      <asp:CheckBoxField DataField="IsActive" HeaderText="Active"/>
      <asp:TemplateField HeaderText="Actions"><ItemTemplate><div class="grid-actions" style="display:flex;gap:6px;white-space:nowrap">
        <asp:LinkButton ID="btnEditRow" runat="server" CssClass="button compact" Text="แก้ไข" CommandName="EditUser" CommandArgument='<%# Eval("UserId") %>' CausesValidation="false"/>
        <asp:LinkButton ID="btnDeleteRow" runat="server" CssClass="button compact danger" Text="ลบ" CommandName="DeleteUser" CommandArgument='<%# Eval("UserId") %>' CausesValidation="false" OnClientClick="return confirm('ยืนยันการลบผู้ใช้นี้? / Delete this user?');"/>
      </div></ItemTemplate></asp:TemplateField>
    </Columns></asp:GridView>
  </div></section>
  <asp:Panel ID="pnlEditor" runat="server" CssClass="card" style="margin-top:18px" Visible="false">
    <asp:HiddenField ID="hidUserId" runat="server"/>
    <div class="card-head"><h2><asp:Literal ID="litEditorTitle" runat="server"/></h2></div>
    <div class="card-body form-grid">
      <div class="field"><label>User Login</label><asp:TextBox ID="txtUserName" runat="server" MaxLength="100"/></div>
      <div class="field"><label>Email (ข้อมูลติดต่อ ไม่ใช้ Login)</label><asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="256"/></div>
      <div class="field"><label>เบอร์ติดต่อ</label><asp:TextBox ID="txtPhone" runat="server" MaxLength="50"/></div>
      <div class="field"><label>ตำแหน่ง</label><asp:TextBox ID="txtPosition" runat="server" MaxLength="150"/></div>
      <div class="field"><label>ชื่อจริง</label><asp:TextBox ID="txtFirstName" runat="server" MaxLength="100"/></div>
      <div class="field"><label>นามสกุล</label><asp:TextBox ID="txtLastName" runat="server" MaxLength="100"/></div>
      <div class="field"><label>Department / Section</label><asp:DropDownList ID="ddlDepartment" runat="server"/></div>
      <div class="field"><label>Password Expiry</label><asp:DropDownList ID="ddlExpiry" runat="server"><asp:ListItem Value="30">30 วัน</asp:ListItem><asp:ListItem Value="60">60 วัน</asp:ListItem><asp:ListItem Value="90" Selected="true">90 วัน</asp:ListItem><asp:ListItem Value="0">ไม่หมดอายุ</asp:ListItem></asp:DropDownList></div>
      <div class="field"><label>BC Fixed Asset Role</label><asp:DropDownList ID="ddlFixedAssetRole" runat="server"/></div>
      <div class="field"><label>BC Administration Role</label><asp:DropDownList ID="ddlAdministrationRole" runat="server"/></div>
      <div class="field"><label>สถานะบัญชี</label><asp:CheckBox ID="chkIsActive" runat="server" Text=" เปิดใช้งาน / Active" Checked="true"/></div>
      <div class="field full"><label>รหัสผ่านเริ่มต้น / รหัสผ่านใหม่</label><asp:TextBox ID="txtInitialPassword" runat="server" TextMode="Password" autocomplete="new-password"/><small>ผู้ใช้ใหม่ต้องระบุรหัสผ่าน ส่วนการแก้ไขให้เว้นว่างหากไม่ต้องการ Reset Password — อย่างน้อย 12 ตัว พร้อม A-Z, a-z, ตัวเลข และอักขระพิเศษ</small></div>
      <div class="field full actions"><asp:Button ID="btnSave" runat="server" CssClass="button primary" Text="บันทึก" OnClick="Save_Click"/><asp:Button ID="btnCancel" runat="server" CssClass="button" Text="ยกเลิก" OnClick="Cancel_Click" CausesValidation="false"/></div>
    </div>
  </asp:Panel>
</asp:Content>
