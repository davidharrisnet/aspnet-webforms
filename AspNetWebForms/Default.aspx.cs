using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class _Default : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void ClockTimer_Tick(object sender, EventArgs e)
    {
        // Nothing to do here - the ContentTemplate's <%: DateTime.Now %> re-evaluates on
        // every render, so simply having Tick wired up is enough to trigger the refresh.
    }
}