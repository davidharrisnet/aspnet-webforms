# AspNetWebForms — Component Survey

Low-level inventory of every part of this repository: what it is, what it does at runtime, and how it connects to the rest of the app. Written as groundwork for evaluating a Java 21 / Spring Boot / Angular port — no migration recommendations here, just the as-is system.

---

## 1. Solution & build model

**`AspNetWebForms.sln`** declares one project using `ProjectTypeGuid` `{E24C65DC-7377-472B-9ABA-BC803B73C61A}` — an ASP.NET **Web Site** project, not a Web Application Project (WAP). This is the older of the two IIS project models Visual Studio supports:

- There is **no `.csproj`**. The `.sln` points straight at the `AspNetWebForms\` folder (`SlnRelativePath`) and embeds compiler settings inline (`TargetFrameworkMoniker = .NETFramework,Version=v4.7.2`, IIS Express virtual path/port `62169`).
- Every `.cs` file under `App_Code/` is compiled as a single dynamic assembly by the ASP.NET runtime the first time it's touched (or ahead-of-time by `aspnet_compiler.exe`/`aspnet_merge.exe`). There is no `bin\Debug\net472\AspNetWebForms.dll` produced by an MSBuild step the way a WAP or SDK-style project would produce.
- Each `.aspx`/`.ascx`/`.master` file is **also** compiled dynamically, from its markup plus its paired code-behind file, referenced via `CodeFile="X.aspx.cs" Inherits="ClassName"` in the `@Page`/`@Control`/`@Master` directive (as opposed to WAP's `CodeBehind` + a compiled `.designer.cs`). This is why the code-behind classes here are declared `public partial class X` with no matching `X.designer.cs` in the tree — the designer partial is generated in-memory at compile time, not checked in.
- **Dependency resolution**: `packages.config` (classic NuGet, not `PackageReference`/`.nuspec`-based SDK resolution). Resolved assemblies are copied into `AspNetWebForms/Bin/` and are checked into source control directly (there's no `packages/` restore step wired into a build — the `Bin/` DLLs are the actual runtime dependencies; the top-level `packages/` folder is NuGet's package cache, also committed).
- **Running it**: only via Visual Studio 2017 (F5, hosts under IIS Express) or by pointing any IIS/IIS Express instance at the `AspNetWebForms/` folder as a site root. There is no CLI build/run/test command — no `dotnet`, no test project, no linter.

## 2. Runtime configuration

### `Web.config` (base)
The root ASP.NET config, `system.web`-based (`System.Web` pipeline, not the OWIN/Katana pipeline exclusively — both coexist here, see §4):

- `<authentication mode="None"/>` — ASP.NET's built-in `FormsAuthentication` module is explicitly turned off (`<remove name="FormsAuthentication"/>` under `system.webServer/modules`). Authentication is instead handled entirely by OWIN cookie middleware (§4/§6), not `System.Web.Security`.
- `<compilation debug="true" targetFramework="4.7.2"/>` / `<httpRuntime targetFramework="4.7.2"/>` — quirks-mode compatibility pinned to 4.7.2 behavior.
- `<pages><namespaces>` auto-imports `System.Web.Optimization` and `Microsoft.AspNet.Identity` into every `.aspx` page's implicit usings, and registers the `webopt:` tag prefix for `Microsoft.AspNet.Web.Optimization.WebForms` (used in `Site.master` for `<webopt:bundlereference>`).
- `<membership>`, `<profile>`, `<roleManager>` are all explicitly `<clear/>`'d — the legacy ASP.NET Membership/Role provider system is disabled; identity is 100% ASP.NET Identity (EF-backed) instead.
- `<sessionState mode="InProc" customProvider="DefaultSessionProvider">` with `System.Web.Providers.DefaultSessionStateProvider` — in-process session state, but configured against a SQL-backed provider (`connectionStringName="DefaultConnection"`) rather than the truly in-memory default, so it's ready to point at a real DB-backed session store without switching providers, just by having schema present.
- `<connectionStrings><add name="DefaultConnection" .../>` — LocalDB connection (`(LocalDb)\MSSQLLocalDB`), `AttachDbFilename=|DataDirectory|\...mdf`. `|DataDirectory|` resolves to `App_Data/` at runtime. The `.mdf` itself is **not** checked in — LocalDB creates it on first EF access (see §6). The GUID in the DB name (`bb90e8c5-...`) is a template artifact, not meaningful.
- `<entityFramework>` — `LocalDbConnectionFactory` default, targeting `mssqllocaldb`; SQL Server provider registered (`System.Data.Entity.SqlServer.SqlProviderServices`).
- `<system.codedom>` — swaps the `.cs` compiler to `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` (Roslyn) instead of the legacy `csc.exe` that ships with .NET Framework, so `App_Code`/code-behind compilation gets modern C# language support. This is why `AspNetWebForms/Bin/roslyn/` exists — it's the Roslyn compiler toolset copied in by that package, invoked by the ASP.NET runtime compiler on each dynamic compile.
- `<runtime><assemblyBinding>` — binding redirects forcing all transitive references (Antlr3, Newtonsoft.Json, WebGrease, EntityFramework, Microsoft.Owin*) up to one consistent version, standard NuGet-generated boilerplate for classic-style dependency resolution (no unifying `PackageReference` graph to do this automatically).

### `Web.Debug.config`
An XDT (XML Document Transform) overlay, applied by Visual Studio's Publish pipeline when publishing a "Debug"-configured deployment. Currently a near-no-op: it strips the `debug` attribute off `<compilation>`. (There is no `Web.Release.config` in the tree — only Debug is customized; a Release publish would use the base `Web.config` as-is, still with FriendlyUrls/whatever else unmodified.)

### `Account/Web.config`
A **folder-scoped** config override — ASP.NET Web.config nesting, where any `.config` inside a subfolder layers on top of the root one for requests under that path. Here it adds `<location path="Manage.aspx"><authorization><deny users="?"/></authorization>`, i.e. anonymous users are denied access to `Account/Manage.aspx` at the `system.web` authorization-module level (this is a `system.web` authorization check, running alongside — not the same mechanism as — the OWIN cookie auth that establishes `User.Identity`). Note only `Manage.aspx` is locked down this way; `Login`/`Register`/`RegisterExternalLogin` are open by design.

### `packages.config`
Full list of top-level NuGet dependencies and their runtime role:

| Package | Version | Role |
|---|---|---|
| `EntityFramework` | 6.2.0 | ORM for `ApplicationDbContext` (Code First, LocalDB) |
| `Microsoft.AspNet.Identity.Core` | 2.2.2 | `UserManager<T>`, `IdentityUser`, password hashing |
| `Microsoft.AspNet.Identity.EntityFramework` | 2.2.2 | `IdentityDbContext<T>`, `UserStore<T>` — EF-backed Identity storage |
| `Microsoft.AspNet.Identity.Owin` | 2.2.2 | Glue between ASP.NET Identity and the OWIN auth pipeline |
| `Microsoft.Owin*` (Host.SystemWeb, Security, Security.Cookies, Security.OAuth, Security.Facebook/Google/MicrosoftAccount/Twitter) | 4.0.0 | OWIN/Katana pipeline hosted inside `System.Web` (`Microsoft.Owin.Host.SystemWeb`), cookie auth middleware, and pre-wired (but disabled) external OAuth providers |
| `Owin` | 1.0 | OWIN interface contracts (`IAppBuilder`) |
| `Microsoft.AspNet.FriendlyUrls.Core` | 1.0.2 | Extensionless/clean URL routing + mobile view switching |
| `Microsoft.AspNet.Providers.Core` | 2.0.0 | SQL-backed session/membership provider infra (`System.Web.Providers`), used by the `DefaultSessionProvider` |
| `Microsoft.AspNet.Web.Optimization` / `.WebForms` | 1.1.3 | `BundleTable`/`BundleConfig`, `<webopt:bundlereference>` |
| `Microsoft.AspNet.ScriptManager.WebForms` / `.MSAjax` | 5.0.0 | `<asp:ScriptManager>` bundle registrations (`WebFormsBundle`, `MsAjaxBundle`) referenced in `Site.master` |
| `AspNet.ScriptManager.bootstrap` / `AspNet.ScriptManager.jQuery` | 3.3.7 / 3.3.1 | Registers Bootstrap/jQuery as named `ScriptManager` bundles (`bootstrap`, `jquery`) |
| `bootstrap` | 3.3.7 | CSS/JS framework (vendored under `Content/`, `Scripts/`) |
| `jQuery` | 3.3.1 | Vendored under `Scripts/` |
| `Modernizr` | 2.8.3 | Feature-detection JS, vendored, bundled via `~/bundles/modernizr` |
| `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` | 2.0.0 | Roslyn-based `.cs`/`.vb` compiler for dynamic compilation (see `Bin/roslyn/`) |
| `Microsoft.Web.Infrastructure` | 1.0.0.0 | Low-level ASP.NET pre-app-start hook plumbing (used internally by `OwinStartupAttribute` discovery, etc.) |
| `Newtonsoft.Json` | 11.0.1 | JSON serialization (OWIN/Identity dependency) |
| `WebGrease` / `Antlr` | 1.6.0 / 3.5.0.2 | JS/CSS minification engine used by `System.Web.Optimization` bundling, and its parser dependency |

## 3. Application lifecycle — `Global.asax`

A `<%@ Application %>` inline-script file (no code-behind), handling only `Application_Start`:

```csharp
RouteConfig.RegisterRoutes(RouteTable.Routes);
BundleConfig.RegisterBundles(BundleTable.Bundles);
```

This is the single process-wide entry point for classic `System.Web` app initialization — it runs once per app-domain (app pool) startup, before the first request is served. Note OWIN startup (`Startup.Configuration`) is a **separate, parallel** bootstrap path (§4) that Microsoft.Owin.Host.SystemWeb hooks into independently via `[assembly: OwinStartupAttribute]`; `Global.asax` does not call into it explicitly.

## 4. OWIN pipeline — `App_Code/Startup.cs` + `App_Code/Startup.Auth.cs`

`Startup` is a `partial class` split across two files (a common ASP.NET Identity template pattern separating "the entry point" from "the auth-specific config"):

- **`Startup.cs`**: `[assembly: OwinStartupAttribute(typeof(AspNetWebForms.Startup))]` is the discovery mechanism — `Microsoft.Owin.Host.SystemWeb` scans loaded assemblies for this attribute at app start and invokes `Configuration(IAppBuilder app)`, which is the OWIN pipeline's root composition method. Here it delegates immediately to `ConfigureAuth(app)`.
- **`Startup.Auth.cs`**: builds the auth middleware chain:
  - `app.UseCookieAuthentication(...)` — registers cookie-based auth with `AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie` and `LoginPath = "/Account/Login"`. This is what actually issues/reads the auth cookie and populates `HttpContext.Current.User` / `Context.GetOwinContext().Authentication` — i.e., the real authentication mechanism, since `system.web/authentication` is set to `None` (§2).
  - `app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie)` — a second, short-lived cookie used only during the external-login handshake (holds the claims from the external provider until `RegisterExternalLogin.aspx` links or creates a local account).
  - Four external OAuth providers (Microsoft Account, Twitter, Facebook, Google) are wired up in commented-out form, each needing a `clientId`/`clientSecret` (or `consumerKey`/`consumerSecret`) to activate. None are currently enabled — `OpenAuthProviders.ascx`'s `GetProviderNames()` will return an empty list until one is uncommented and configured, which is why `OpenAuthProviders.ascx`'s `EmptyDataTemplate` exists.

## 5. Identity & data layer — `App_Code/IdentityModels.cs`

Three types, all in the flat `AspNetWebForms` namespace:

- **`ApplicationUser : IdentityUser`** — no custom properties added; it's the stock Identity user shape (Id, UserName, PasswordHash, SecurityStamp, Claims, Logins, Roles — all inherited from `IdentityUser`/`IdentityDbContext` base classes in `Microsoft.AspNet.Identity.EntityFramework`).
- **`ApplicationDbContext : IdentityDbContext<ApplicationUser>`** — EF6 `DbContext` subclass; `IdentityDbContext<T>` already defines the `DbSet`s for Users/Roles/Claims/Logins under the hood. Constructed with connection string name `"DefaultConnection"` (matches `Web.config`). No custom `DbSet<T>` properties are added, so **there is no application data model yet** beyond the Identity schema itself — no business entities. EF6 is running in **Code First** mode with no explicit `Migrations/` folder present, meaning the database is created via `CreateDatabaseIfNotExists` (EF6's default initializer) against LocalDB the first time a context is used — not migration-tracked.
- **`UserManager : UserManager<ApplicationUser>`** — a thin subclass whose only job is to default-construct itself with `new UserStore<ApplicationUser>(new ApplicationDbContext())`, so call sites can just write `new UserManager()` instead of wiring up the store each time. Every `Account/*.aspx.cs` page does exactly that, per-request — there is no DI container or shared `UserManager` instance; each request builds its own `ApplicationDbContext`/`UserManager` (and implicitly opens its own DB connection).
- **`IdentityHelper`** (static, `#region Helpers`) — request-scoped auth utilities shared by all Account pages:
  - `SignIn(manager, user, isPersistent)` — signs out any lingering `ExternalCookie`, builds a `ClaimsIdentity` via `manager.CreateIdentity(...)`, and calls `authenticationManager.SignIn(...)` against the OWIN `ApplicationCookie`. This is the **only** path by which the auth cookie gets issued anywhere in the app.
  - `GetProviderNameFromRequest` / `GetExternalLoginRedirectUrl` — query-string plumbing (`providerName=...`) used to round-trip which external provider initiated a login through the `RegisterExternalLogin.aspx?providerName=X` redirect.
  - `IsLocalUrl` (private) — a hand-rolled open-redirect guard (checks the URL starts with `/` but not `//` or `\`, or starts with `~/`), used by:
  - `RedirectToReturnUrl` — safely redirects to a caller-supplied `ReturnUrl` query param only if it passes `IsLocalUrl`, else falls back to `~/`. Used after every successful login/registration/external-link flow.

## 6. Routing — `App_Code/RouteConfig.cs`

Single call: `routes.EnableFriendlyUrls(new FriendlyUrlSettings { AutoRedirectMode = RedirectMode.Permanent })`. `Microsoft.AspNet.FriendlyUrls` is a routing extension purpose-built for Web Forms that:
- Strips `.aspx` from URLs (`/About` resolves to `About.aspx`) and 301-redirects requests that still use the extension (`AutoRedirectMode = Permanent`).
- Registers the mobile-view-switch route (`AspNet.FriendlyUrls.SwitchView`, referenced by name in `ViewSwitcher.ascx.cs`, §8) and the device-detection logic that selects `Site.Mobile.master` vs `Site.master` per request/cookie.
- There is no other custom routing (no route-parameter patterns, no MVC-style routes) — this is purely file-based Web Forms routing with the extension stripped.

## 7. Bundling & minification

- **`App_Code/BundleConfig.cs`** — registers three `ScriptBundle`s against `System.Web.Optimization`'s `BundleTable`: `~/bundles/WebFormsJs` (the Web Forms postback/validation runtime scripts — `WebForms.js`, `WebUIValidation.js`, `MenuStandards.js`, `Focus.js`, `GridView.js`, `DetailsView.js`, `TreeView.js`, `WebParts.js`, all physically present under `Scripts/WebForms/`), `~/bundles/MsAjaxJs` (the Microsoft Ajax runtime, `Scripts/WebForms/MSAjax/*.js`, order-sensitive due to explicit inter-file dependencies), and `~/bundles/modernizr`.
- **`Bundle.config`** — a separate, declarative bundle definition (consumed by `Microsoft.AspNet.Web.Optimization.WebForms`'s `<webopt:bundlereference>` control) defining a **CSS** bundle `~/Content/css` from `bootstrap.css` + `Site.css`. This is referenced directly in `Site.master`'s `<head>` via `<webopt:bundlereference runat="server" path="~/Content/css" />` — a separate mechanism from the C#-registered script bundles above, both active simultaneously.
- At runtime, in Debug mode (`compilation debug="true"`) bundling serves files unminified/unbundled for easier debugging; in a Release publish it would minify+combine (via WebGrease/Antlr, per `packages.config`).
- **`Content/`**: vendored Bootstrap 3.3.7 (full + minified + sourcemaps) and a near-empty `Site.css` (29 lines — template placeholder, no real custom styling yet). **`fonts/`**: Bootstrap's Glyphicons webfont set (eot/svg/ttf/woff/woff2). **`Scripts/`**: jQuery 3.3.1 (full/slim/minified variants + intellisense stub), Modernizr, Bootstrap JS, and the full Web Forms/MSAjax runtime script set named above.

## 8. Master pages, layout, and view switching

### `Site.master` / `Site.master.cs` (desktop layout)
- Markup: standard Bootstrap navbar (brand, Home/About/Contact links) + an `<asp:LoginView>` that renders either a Register/Log in pair (`AnonymousTemplate`) or a "Hello, {username}!" link to `Account/Manage` plus an `<asp:LoginStatus>` logout button (`LoggedInTemplate`) — switched purely server-side based on `Context.User.Identity` at render time, no client-side auth-state logic. One `<asp:ContentPlaceHolder ID="MainContent">` is the single content injection point every page (`Default`, `About`, `Contact`, all `Account/*` pages) fills via `<asp:Content ContentPlaceHolderID="MainContent">`.
- A single `<asp:ScriptManager>` is declared with an explicit `<Scripts>` list of `<asp:ScriptReference>` entries — this is required by Web Forms for any page using `UpdatePanel`/partial postbacks or the Web Forms/MSAjax client runtime, and is what actually emits the `<script>` tags for the bundles registered in §7 (`MsAjaxBundle`, `jquery`, `bootstrap`, `WebForms.js` et al., `WebFormsBundle`).
- **Code-behind (`Site.master.cs`) implements manual anti-XSRF/CSRF protection**, since Web Forms' built-in `ViewStateUserKey` mechanism needs to be wired up explicitly:
  - `Page_Init`: reads (or creates) an `HttpOnly` cookie `__AntiXsrfToken` holding a GUID, and sets `Page.ViewStateUserKey` to that GUID — this ties the page's ViewState MAC to the per-browser token, so a ViewState blob can't be replayed cross-site.
  - `master_Page_PreLoad`: on first load (`!IsPostBack`), stashes the token and the current username into `ViewState`; on postback, re-validates both match, throwing `InvalidOperationException` if not (defends against session fixation / user-swap mid-session as well as XSRF).
  - `Unnamed_LoggingOut`: wired to `LoginStatus.OnLoggingOut`; calls `Context.GetOwinContext().Authentication.SignOut()` — i.e., logout goes through the OWIN cookie middleware, not `FormsAuthentication.SignOut()` (consistent with auth being OWIN-cookie-based throughout).

### `Site.Mobile.master` / `Site.Mobile.master.cs`
A minimal alternate layout (`<h1>Mobile Master Page</h1>`, one extra `FeaturedContent` placeholder besides `MainContent`) selected automatically by FriendlyUrls' device-detection convention (`Site.Mobile.master` is discovered by filename pattern relative to `Site.master`). Code-behind is an empty `Page_Load`. It embeds `ViewSwitcher.ascx` to let a mobile user force the desktop view.

### `ViewSwitcher.ascx` / `.ascx.cs`
A `UserControl` (Web Forms' component/partial-view mechanism — markup + code-behind pair, included into a page via `<%@ Register %>` + a custom tag, here `<friendlyUrls:ViewSwitcher>`). Logic: calls `WebFormsFriendlyUrlResolver.IsMobileView(...)` to determine the current view, then builds a URL to the named route `AspNet.FriendlyUrls.SwitchView` (registered by `RouteConfig`'s `EnableFriendlyUrls` call) carrying a `ReturnUrl` back to the current page — i.e., it's a thin UI wrapper around FriendlyUrls' built-in view-switch route, not custom switching logic. If that route isn't registered (Friendly URLs disabled), the control hides itself (`this.Visible = false`).

## 9. Content pages

`Default.aspx`, `About.aspx`, `Contact.aspx` — all trivial: `MasterPageFile="~/Site.Master"`, one `<asp:Content>` block of static markup (the default VS template's marketing copy/placeholder text — "Your application description page.", "Your contact page.", the ASP.NET jumbotron), and a 13-line code-behind with an **empty** `Page_Load`. No data binding, no server controls beyond the master page's, no business logic. These represent the "empty shell" state of the app — everything of substance so far is the Identity/auth scaffold.

## 10. Account / Identity UI (`Account/`)

All four pages share the same shape: `MasterPageFile="~/Site.master"`, Bootstrap `form-horizontal` markup with `asp:TextBox`/`asp:RequiredFieldValidator`/`asp:CompareValidator` (Web Forms' built-in declarative validation, evaluated both client-side via `WebUIValidation.js` and server-side via `Page.IsValid` before any handler logic runs), and code-behind that talks to `UserManager`/`IdentityHelper` only — no direct EF/DB code outside `IdentityModels.cs`.

- **`Login.aspx`/`.cs`**: `Async="true"` page (required because OWIN authentication calls are asynchronous under the hood even though this code-behind's `LogIn` handler is synchronous — Web Forms' async page infrastructure just needs to be opted into for pages that touch the OWIN context). `LogIn` handler: `manager.Find(username, password)` (validates credentials directly against the `UserManager`, no separate `SignInManager`), then `IdentityHelper.SignIn` + `RedirectToReturnUrl` on success, or sets an inline error message. Also embeds `Account/OpenAuthProviders.ascx` for social login and preserves `ReturnUrl` through to the Register link.
- **`Register.aspx`/`.cs`**: Username/Password/ConfirmPassword form; `CreateUser_Click` calls `manager.Create(user, password)`, signs in on success (`isPersistent: false`), otherwise surfaces `result.Errors.FirstOrDefault()`.
- **`Manage.aspx`/`.cs`**: The one page locked to authenticated users (`Account/Web.config`, §2). Conditionally renders either a "set password" form (for accounts with no local password — i.e., external-login-only accounts) or a "change password" form (`HasPassword` checks `user.PasswordHash != null`), plus an `<asp:ListView>` of registered external logins bound via `SelectMethod="GetLogins" DeleteMethod="RemoveLogin"` (Web Forms' model-binding pattern — the ListView calls those code-behind methods directly by name/signature, no separate data-access layer). Success/error state round-trips through a `?m=` query-string flag (`ChangePwdSuccess`/`SetPwdSuccess`/`RemoveLoginSuccess`) read back on the next `Page_Load`. `CanRemoveExternalLogins` gates whether the "Remove" button shows per login (must have >1 login or a password set, so the user can't lock themselves out).
- **`RegisterExternalLogin.aspx`/`.cs`**: The landing page after an external OAuth round-trip. `Page_Load` (non-postback) reads `IdentityHelper.GetProviderNameFromRequest`, pulls `GetExternalLoginInfo()` off the OWIN context; if a user already exists with that external login it signs straight in; if the current user is authenticated (linking flow) it re-validates the XSRF dictionary entry set in `OpenAuthProviders.ascx.cs` before calling `AddLogin`; otherwise it pre-fills the username field (`loginInfo.DefaultUserName`) so the user can confirm/pick a username, and `LogIn_Click`/`CreateAndLoginUser` creates the local `ApplicationUser`, links the external login, and signs in.
- **`OpenAuthProviders.ascx`/`.cs`** (shared user control, embedded in both `Login.aspx` and `Manage.aspx`): `GetProviderNames()` lists whatever's registered via `app.UseXAuthentication` in `Startup.Auth.cs` (currently none — see §4). On postback with a `provider` form value, it issues an OWIN `Authentication.Challenge(...)` targeting `RegisterExternalLogin.aspx` as the callback, setting HTTP 401 to trigger the OWIN challenge redirect (`Response.StatusCode = 401; Response.End();` — this is the standard OWIN "challenge" pattern under `System.Web`, since OWIN middleware intercepts the 401 on the way out rather than the handler redirecting directly). When used from `Manage.aspx` it XSRF-tags the challenge with the current user's ID (`IdentityHelper.XsrfKey`) so the subsequent `AddLogin` can be verified as originating from that authenticated user.

## 11. What's *not* here

Worth noting explicitly since it shapes any porting estimate: no custom business entities/`DbSet`s beyond Identity, no Web API/service endpoints, no client-side SPA framework or build step (no `package.json`/webpack/npm — all JS is vendored static files), no unit/integration tests, no CI config, no custom middleware beyond auth, no logging framework wired in, no App_Data content (DB is created fresh per-environment by EF6 on first run), and only one (disabled-by-default) external-auth surface. The functional surface area is entirely: static content pages + ASP.NET Identity's stock local/external auth flow.
