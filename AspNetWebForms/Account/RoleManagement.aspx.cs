using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Linq;
using System.Web.UI;
using AspNetWebForms;

public partial class Account_RoleManagement : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            BindRoles();
        }
    }

    private void BindRoles()
    {
        var roleManager = new ApplicationRoleManager();
        var roleNames = roleManager.Roles.Select(r => r.Name).OrderBy(n => n).ToList();

        RolesList.DataSource = roleManager.Roles.OrderBy(r => r.Name).ToList();
        RolesList.DataBind();

        AssignRoleDropDown.DataSource = roleNames;
        AssignRoleDropDown.DataBind();

        var userManager = new UserManager();
        var userId = User.Identity.GetUserId();
        var currentRoles = userManager.GetRoles(userId);
        CurrentRolesLiteral.Text = currentRoles.Any() ? string.Join(", ", currentRoles) : "(none)";

        AdminPanel.Visible = User.IsInRole("Admin");
    }

    protected void CreateRole_Click(object sender, EventArgs e)
    {
        if (!IsValid) return;

        var roleManager = new ApplicationRoleManager();
        var roleName = NewRoleName.Text.Trim();
        if (!string.IsNullOrEmpty(roleName) && !roleManager.RoleExists(roleName))
        {
            roleManager.Create(new IdentityRole(roleName));
        }
        BindRoles();
    }

    protected void AssignRole_Click(object sender, EventArgs e)
    {
        var role = AssignRoleDropDown.SelectedValue;
        if (string.IsNullOrEmpty(role))
        {
            BindRoles();
            return;
        }

        var userManager = new UserManager();
        var userId = User.Identity.GetUserId();
        if (!userManager.IsInRole(userId, role))
        {
            userManager.AddToRole(userId, role);

            // The auth cookie's role claims were fixed at sign-in time - refresh it now
            // so the new role takes effect immediately instead of on next login.
            var user = userManager.FindById(userId);
            IdentityHelper.SignIn(userManager, user, isPersistent: false);
        }
        BindRoles();
    }
}
