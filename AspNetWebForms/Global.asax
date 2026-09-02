<%@ Application Language="C#" %>
<%@ Import Namespace="AspNetWebForms" %>
<%@ Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.Web.Http" %>
<%@ Import Namespace="System.Web.Optimization" %>
<%@ Import Namespace="System.Web.Routing" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e)
    {
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
        GlobalConfiguration.Configure(WebApiConfig.Register);
    }

    void Application_Error(object sender, EventArgs e)
    {
        var app = (HttpApplication)sender;
        var ex = app.Server.GetLastError();
        // In a real deployment this would go to a proper sink (ELMAH, Serilog, Application
        // Insights, etc.) rather than Trace - see customErrors/Error.aspx in Web.config for
        // the user-facing side of this handler.
        Trace.WriteLine("[Application_Error] Unhandled exception: " + ex);
    }

</script>
