<%@ Page Title="Customers" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeFile="Customers.aspx.cs" Inherits="Customers" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <h2>Customers</h2>
    <p>
        <a runat="server" href="~/Handlers/CustomerExport.ashx" class="btn btn-default">Export CSV</a>
    </p>

    <asp:ObjectDataSource ID="CustomersDataSource" runat="server"
        TypeName="AspNetWebForms.Data.CustomerRepository"
        SelectMethod="GetCustomers"
        SortParameterName="sortExpression"
        UpdateMethod="UpdateCustomer"
        DeleteMethod="DeleteCustomer">
        <SelectParameters>
            <asp:Parameter Name="sortExpression" Type="String" DefaultValue="" />
        </SelectParameters>
    </asp:ObjectDataSource>

    <%-- No code-behind is involved in paging, sorting, editing, or deleting below - the
         GridView/ObjectDataSource pair handles all of it declaratively. --%>
    <asp:GridView ID="CustomersGrid" runat="server" AutoGenerateColumns="False"
        DataSourceID="CustomersDataSource" DataKeyNames="CustomerId"
        AllowPaging="True" PageSize="5" AllowSorting="True" CssClass="table table-striped">
        <Columns>
            <asp:BoundField DataField="CustomerId" HeaderText="ID" SortExpression="CustomerId" ReadOnly="True" />
            <asp:BoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
            <asp:BoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
            <asp:BoundField DataField="CreatedDate" HeaderText="Created" SortExpression="CreatedDate" ReadOnly="True" DataFormatString="{0:d}" />
            <asp:CommandField ShowEditButton="True" ShowDeleteButton="True" />
            <asp:TemplateField HeaderText="Orders">
                <ItemTemplate>
                    <asp:HyperLink runat="server" NavigateUrl='<%# "~/CustomerDetails.aspx?CustomerId=" + Eval("CustomerId") %>' Text="View details" CssClass="btn btn-default btn-xs" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>No customers yet.</EmptyDataTemplate>
    </asp:GridView>
</asp:Content>
