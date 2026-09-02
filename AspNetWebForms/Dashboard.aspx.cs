using System;
using System.Web;
using System.Web.UI;
using AspNetWebForms.Data;

public partial class Dashboard : Page
{
    protected DashboardSummary Summary { get; private set; }

    protected void Page_Load(object sender, EventArgs e)
    {
        // Only executes when this request regenerates the cached output - i.e. once every
        // OutputCache Duration, not on every hit. The aggregate query below is exactly the
        // kind of expensive work OutputCache exists to shield the database from.
        var repository = new ReportingRepository();
        Summary = repository.GetSummary();
    }

    // Substitution callback: must be public static, signature (HttpContext) -> string.
    // Unlike Page_Load above, this runs on every request, cache hit or not.
    public static string GetVisitCountFragment(HttpContext context)
    {
        int visits = context.Session != null && context.Session["DashboardVisits"] != null
            ? (int)context.Session["DashboardVisits"]
            : 0;
        visits++;
        if (context.Session != null)
        {
            context.Session["DashboardVisits"] = visits;
        }
        return string.Format(
            "Session-tracked visit count for you, specifically: {0} (updates on every request, even a cache hit).",
            visits);
    }
}
