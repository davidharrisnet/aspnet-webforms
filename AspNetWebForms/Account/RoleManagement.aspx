<%@ Page Title="Manage Roles" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="RoleManagement.aspx.cs" Inherits="Account_RoleManagement" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <h2><%: Title %></h2>
    <p>
        Signed in as <strong><%: User.Identity.Name %></strong>. Your roles:
        <asp:Literal runat="server" ID="CurrentRolesLiteral" />
    </p>

    <%-- Config-based role authorization (the declarative flavor) is set up in
         Account/Web.config for this page - see the <location path="RoleManagement.aspx">
         block there. Restricted to "signed in" rather than "Admin" here so the very first
         user can reach this page to create a role and grant it to themselves; a real admin
         console would instead lock the whole folder to an Admin role from the start and
         seed that role via the EF Migrations Seed() method (see App_Code/Migrations). --%>

    <div class="row">
        <div class="col-md-6">
            <h4>Existing roles</h4>
            <ul class="list-group">
                <asp:Repeater runat="server" ID="RolesList">
                    <ItemTemplate>
                        <li class="list-group-item"><%#: Eval("Name") %></li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>

            <h4>Create a role</h4>
            <div class="form-inline">
                <asp:TextBox runat="server" ID="NewRoleName" CssClass="form-control" placeholder="Role name" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="NewRoleName"
                    CssClass="text-danger" ErrorMessage="Role name is required" Display="Dynamic" />
                <asp:Button runat="server" Text="Create role" CssClass="btn btn-default" OnClick="CreateRole_Click" />
            </div>
        </div>

        <div class="col-md-6">
            <h4>Assign yourself a role</h4>
            <div class="form-inline">
                <asp:DropDownList runat="server" ID="AssignRoleDropDown" CssClass="form-control" />
                <asp:Button runat="server" Text="Assign to me" CssClass="btn btn-default" OnClick="AssignRole_Click" />
            </div>

            <%-- Code-based role authorization (the imperative flavor): this panel's
                 visibility is set in code-behind from User.IsInRole("Admin"), the same
                 check UrlAuthorizationModule performs internally for <allow roles="..."/>
                 in a Web.config location block. --%>
            <asp:PlaceHolder runat="server" ID="AdminPanel" Visible="false">
                <div class="alert alert-success" style="margin-top: 15px;">
                    You are in the <strong>Admin</strong> role - this panel is only rendered
                    for users who pass <code>User.IsInRole("Admin")</code>.
                </div>
            </asp:PlaceHolder>
        </div>
    </div>
</asp:Content>
