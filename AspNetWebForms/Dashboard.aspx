<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeFile="Dashboard.aspx.cs" Inherits="Dashboard" %>
<%@ OutputCache Duration="30" VaryByParam="None" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <h2><%: Title %></h2>
    <p class="text-muted">
        This whole page is server-rendered once and served from OutputCache for 30 seconds
        at a time (<code>&lt;%@ OutputCache Duration="30" VaryByParam="None" %&gt;</code>) -
        the KPI numbers below only recompute when the cache expires, regardless of how many
        requests hit the page in between.
    </p>

    <div class="row">
        <div class="col-md-4">
            <h3><%: Summary.CustomerCount %></h3>
            <p>Customers</p>
        </div>
        <div class="col-md-4">
            <h3><%: Summary.OrderCount %></h3>
            <p>Orders</p>
        </div>
        <div class="col-md-4">
            <h3><%: Summary.TotalRevenue.ToString("C") %></h3>
            <p>Total revenue</p>
        </div>
    </div>

    <hr />

    <p>
        <%-- asp:Substitution is "donut caching": this fragment re-runs its callback on
             every single request and is spliced into the cached HTML afterwards, so it
             stays live even while everything else on the page is served from cache. --%>
        <asp:Substitution runat="server" MethodName="GetVisitCountFragment" />
    </p>
</asp:Content>
