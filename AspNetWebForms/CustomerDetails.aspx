<%@ Page Title="Customer Details" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeFile="CustomerDetails.aspx.cs" Inherits="CustomerDetails" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <h2>Customer Details</h2>

    <asp:ObjectDataSource ID="CustomerDetailsSource" runat="server"
        TypeName="AspNetWebForms.Data.CustomerRepository"
        SelectMethod="GetCustomerById">
        <SelectParameters>
            <asp:QueryStringParameter Name="customerId" QueryStringField="CustomerId" Type="Int32" />
        </SelectParameters>
    </asp:ObjectDataSource>

    <asp:DetailsView ID="CustomerDetailsView" runat="server" DataSourceID="CustomerDetailsSource"
        AutoGenerateRows="False" CssClass="table">
        <Fields>
            <asp:BoundField DataField="CustomerId" HeaderText="ID" />
            <asp:BoundField DataField="Name" HeaderText="Name" />
            <asp:BoundField DataField="Email" HeaderText="Email" />
            <asp:BoundField DataField="CreatedDate" HeaderText="Customer since" DataFormatString="{0:d}" />
        </Fields>
    </asp:DetailsView>

    <h3>Order history</h3>

    <asp:ObjectDataSource ID="OrdersDataSource" runat="server"
        TypeName="AspNetWebForms.Data.OrderRepository"
        SelectMethod="GetOrdersByCustomer">
        <SelectParameters>
            <asp:QueryStringParameter Name="customerId" QueryStringField="CustomerId" Type="Int32" />
        </SelectParameters>
    </asp:ObjectDataSource>

    <asp:GridView ID="OrdersGrid" runat="server" AutoGenerateColumns="False"
        DataSourceID="OrdersDataSource" CssClass="table table-striped">
        <Columns>
            <asp:BoundField DataField="OrderId" HeaderText="Order #" />
            <asp:BoundField DataField="OrderDate" HeaderText="Date" DataFormatString="{0:d}" />
            <asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="{0:C}" />
            <asp:BoundField DataField="Status" HeaderText="Status" />
        </Columns>
        <EmptyDataTemplate>No orders yet.</EmptyDataTemplate>
    </asp:GridView>

    <p><a runat="server" href="~/Customers.aspx" class="btn btn-default">Back to list</a></p>
</asp:Content>
