using System;
using System.Diagnostics;
using System.Web;

namespace AspNetWebForms.Modules
{
    // Registered in Web.config under system.webServer/modules. Runs on every request in
    // the pipeline (static files, .aspx, .ashx, Web API routes alike) - a module operates
    // below the page/handler level, unlike a page's own code-behind.
    public class RequestLoggingModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            app.Context.Items["RequestLoggingModule_StartTicks"] = Environment.TickCount;
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var startTicks = app.Context.Items["RequestLoggingModule_StartTicks"];
            var elapsedMs = startTicks != null ? Environment.TickCount - (int)startTicks : -1;
            Trace.WriteLine(string.Format("[RequestLoggingModule] {0} {1} -> {2} ({3} ms)",
                app.Request.HttpMethod, app.Request.Path, app.Response.StatusCode, elapsedMs));
        }

        public void Dispose()
        {
        }
    }
}
