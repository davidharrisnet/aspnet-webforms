<%@ Page Title="Error" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeFile="Error.aspx.cs" Inherits="ErrorPage" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <h2>Something went wrong</h2>
    <p>
        An unexpected error occurred while processing your request. It has been logged;
        please try again, or head back to the <a runat="server" href="~/">home page</a>.
    </p>
</asp:Content>
