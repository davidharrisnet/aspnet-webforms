# Core Enterprise Component Set

This project (`asp_analysis` branch) started as the bare Visual Studio 2017 ASP.NET Web Forms template — see [ASP.md](ASP.md) for a full survey of that starting state. That template only exercises the Identity/auth slice of Web Forms; a real enterprise app touches a much wider surface. This file documents the additional component types added on top of it, one representative example of each, as a testbed for scoping a Java 21 / Angular migration.

Everything below targets .NET Framework 4.7.2 / classic Web Forms unchanged — see [CLAUDE.md](CLAUDE.md).

## What was added, and why each matters for the migration

### 1. Real business data model — `App_Code/Models/Customer.cs`, `Order.cs`, `CustomerDto.cs`

The starting template had no `DbSet<T>` beyond ASP.NET Identity's own schema. Added:
- `Customer` / `Order` — EF6 Code First entities with a 1:many relationship (`Customer.Orders` / `Order.Customer`, FK via `[ForeignKey("Customer")]`), registered as new `DbSet<Customer>`/`DbSet<Order>` on `ApplicationDbContext` (`App_Code/IdentityModels.cs`).
- `CustomerDto` — a flat projection with no navigation properties, used by the Web API layer (see §6) specifically to avoid JSON.NET circular-reference errors and EF's lazy-load-after-context-disposal trap when serializing entities directly. Worth carrying this DTO/projection habit into the Java rewrite — it's the same shape problem there (JPA entity graphs vs. what a REST layer should actually expose).
- `App_Code/Migrations/Configuration.cs` — the C# half of EF6 Code First Migrations (`DbMigrationsConfiguration<ApplicationDbContext>`, `AutomaticMigrationsEnabled = false`, a `Seed()` method). **The actual migration snapshot files are not included** — EF6's `Enable-Migrations`/`Add-Migration` tooling only runs inside Visual Studio's Package Manager Console (or `migrate.exe` wired to a real build), and the `.resx` a real migration carries embeds a compressed snapshot of the compiled EF model that can't be hand-authored correctly outside that tooling. Until someone runs that from VS, the app keeps using EF6's default `CreateDatabaseIfNotExists` initializer, which builds the schema straight from the current model on first access — functionally fine for this scaffold, but worth flagging: a real enterprise app almost always has actual migration history, which is itself a migration-planning artifact (it's effectively the audit trail of schema evolution you'd want to replay in Flyway/Liquibase on the Java side).

### 2. GridView with paging, sorting, inline editing, delete — `Customers.aspx`

Bound to an `ObjectDataSource` (`TypeName="AspNetWebForms.Data.CustomerRepository"`) rather than a `SqlDataSource` — the more common enterprise 3-tier pattern, since it keeps SQL out of the markup and goes through a plain C# "business object" (`App_Code/Data/CustomerRepository.cs`) instead.

The notable thing to carry into migration planning: **zero code-behind is involved in paging, sorting, editing, or deleting.** `Customers.aspx.cs` is an empty `Page_Load`. The GridView's `AllowPaging`/`AllowSorting`/`CommandField` attributes, combined with `ObjectDataSource`'s `SelectMethod`/`SortParameterName`/`UpdateMethod`/`DeleteMethod`, wire the entire CRUD grid up declaratively at the markup level, with ASP.NET's data-binding runtime doing parameter-matching by name (bound field names ↔ method parameter names, `DataKeyNames` ↔ key parameters) via reflection. This declarative-binding-to-a-plain-method-by-name-and-signature model has **no direct equivalent** in Angular (which is explicit/imperative — you'd write the paging/sorting/edit-state logic by hand against a REST endpoint) — it's one of the highest-cost-to-replicate patterns in the whole codebase, precisely because there's no code here to "port," only markup-driven runtime wiring to reconstruct as real logic.

Also added: an **Export CSV** link to the generic handler (§5).

### 3. DetailsView master-detail — `CustomerDetails.aspx`

Reached via `?CustomerId=5` from the `Customers.aspx` grid (bookmarkable-URL style master-detail, the more common enterprise pattern vs. `PreviousPageType` cross-page postback). `ObjectDataSource`'s `<asp:QueryStringParameter>` feeds the query string value straight into `CustomerRepository.GetCustomerById(int customerId)`'s parameter by name. Below the `DetailsView` (customer fields), a second `ObjectDataSource`/`GridView` pair (`OrderRepository.GetOrdersByCustomer`) renders that customer's order history — the master-detail relationship is expressed purely as two independently-bound, page-shared query-string-driven data sources, not an explicit parent/child object graph walked in code.

### 4. UpdatePanel + Timer — `Default.aspx`

A live server-time widget on the home page: `<asp:Timer Interval="5000">` fires an async postback every 5 seconds, and `<asp:UpdatePanel UpdateMode="Always">` re-renders just its `ContentTemplate` in response — "AJAX without writing JavaScript," using the `ScriptManager` already declared in `Site.master`. The `Tick` handler in `Default.aspx.cs` is intentionally empty; the `<%: DateTime.Now.ToString("T") %>` inline expression re-evaluates on every partial render, so simply wiring the event is enough. This whole mechanism — page-lifecycle-driven partial re-rendering triggered by a server-side timer, with the client-side plumbing auto-generated — is another pattern with no Angular equivalent; Angular would just poll or hold a WebSocket/interval on the client.

### 5. Generic handler (`.ashx`) — `Handlers/CustomerExport.ashx`

`CustomerExport.ashx` is a one-line directive (`<%@ WebHandler Language="C#" Class="AspNetWebForms.Handlers.CustomerExportHandler" %>`) pointing at a class living in `App_Code/Handlers/CustomerExportHandler.cs` — the standard non-WAP pattern (App_Code classes don't need a `CodeBehind` attribute; the class is already compiled into the shared App_Code assembly). `CustomerExportHandler : IHttpHandler` writes a CSV of all customers directly to the response stream — no page lifecycle, no master page, no ViewState. This is the classic mechanism for file downloads/exports and lightweight raw-HTTP endpoints in Web Forms apps, and maps fairly directly onto a plain servlet/controller endpoint in Java — one of the lower-friction pieces to migrate.

### 6. ASP.NET Web API 2 controller — `App_Code/Controllers/CustomersApiController.cs`

Reachable at `GET /api/customers` and `GET /api/customers/{id}`, running in the same app pool/IIS pipeline as the Web Forms pages but through an entirely separate routing/dispatch layer (`System.Web.Http`, not `System.Web.UI`).

This required adding three new NuGet packages not in the original template — **`Microsoft.AspNet.WebApi.Core`, `.Client`, `.WebHost`, all pinned to 5.2.9** (the last pre-OWIN-pipeline "classic WebHost" release line, matching how this app is already wired — cookie auth via `Microsoft.Owin.Host.SystemWeb`, not a `Microsoft.Owin.Host.HttpListener`/pure-OWIN app). Since this is a classic `packages.config` Web Site project with no NuGet-restore build step, the packages were resolved by hand: downloaded from `api.nuget.org`, the `net45` `lib/` DLLs (`System.Web.Http.dll`, `System.Web.Http.WebHost.dll`, `System.Net.Http.Formatting.dll`) copied straight into `AspNetWebForms/Bin/` (any DLL dropped in `Bin/` is automatically referenced by all dynamically-compiled code — no `.csproj` reference list to edit), and `packages.config` updated to record them. All three resolved to assembly version `5.2.9.0` exactly matching the package version, and nothing else in the project references a conflicting version, so **no new assembly binding redirects were needed** — `Newtonsoft.Json`'s existing redirect (to `11.0.0.0`) already satisfies Web API's `>= 6.0.4.0` requirement.

Wiring: `App_Code/WebApiConfig.cs` (`HttpConfiguration` route registration, `api/{controller}/{id}`, plus `ReferenceLoopHandling.Ignore` as a defensive JSON.NET default) is invoked from `Global.asax`'s `Application_Start` via `GlobalConfiguration.Configure(WebApiConfig.Register)` — the standard non-OWIN WebHost bootstrap, which also transparently registers the bridging route into `System.Web.Routing.RouteTable.Routes` so IIS's extensionless-URL handling (already relied on by FriendlyUrls, see [ASP.md](ASP.md) §6) picks up `/api/*` requests with no further `Web.config` handler mapping needed.

For the migration analysis: this is the one piece of the app that's already shaped like what a Java/Spring or Angular team would recognize — a REST controller returning DTOs. It's a natural seam to peel off first, or to model the target Java API surface after directly.

### 7. Custom `IHttpModule` — `App_Code/Modules/RequestLoggingModule.cs`

Registered in `Web.config` (`<system.webServer><modules><add name="RequestLoggingModule" type="AspNetWebForms.Modules.RequestLoggingModule"/>`), hooking `BeginRequest`/`EndRequest` on every request in the pipeline — static files, `.aspx`, `.ashx`, and the new Web API routes alike, since a module sits below all of them. Logs method/path/status/elapsed-ms via `Trace.WriteLine`. This is the cross-cutting-concern mechanism in classic ASP.NET; its nearest Java analogue is a servlet `Filter` (or a Spring `HandlerInterceptor`), and its nearest Angular-side analogue (for HTTP-level concerns specifically) is an `HttpInterceptor` — but note an `IHttpModule` runs server-side across *all* content types, which is a broader net than either single Java/Angular equivalent alone.

### 8. Role-based authorization — `Account/RoleManagement.aspx`, `ApplicationRoleManager`

The base template's `Web.config` explicitly disables the legacy `roleManager` provider (`<roleManager><providers><clear/></providers></roleManager>`), consistent with auth here running entirely through OWIN cookies + ASP.NET Identity rather than classic `FormsAuthentication`. So roles are built the Identity way: `ApplicationRoleManager : RoleManager<IdentityRole>` (`App_Code/IdentityModels.cs`), backed by the `AspNetRoles`/`AspNetUserRoles` tables `IdentityDbContext<T>` already defines — not the legacy static `Roles` class.

`Account/RoleManagement.aspx` demonstrates both authorization flavors side by side:
- **Declarative/config-based** — `Account/Web.config` gets a new `<location path="RoleManagement.aspx"><authorization><deny users="?"/></authorization></location>` block (same mechanism already used for `Manage.aspx`). Note this only requires *sign-in*, not the `Admin` role specifically — a deliberate simplification to sidestep the classic "who admins the first admin" bootstrap problem (a folder locked to `<allow roles="Admin">` from the start is unreachable by anyone until an Admin already exists; a real deployment would instead seed the `Admin` role via the EF Migrations `Seed()` method from §1 and lock the folder down properly from day one).
- **Imperative/code-based** — inside the page, an `AdminPanel` placeholder's `Visible` is set from `User.IsInRole("Admin")` in code-behind — the same check `UrlAuthorizationModule` performs internally for `<allow roles="...">`, just invoked directly.

The page itself lets any signed-in user create a role and self-assign it (for demo purposes only — a real admin console would never allow self-service role grants), and re-signs the user in after a role change (`IdentityHelper.SignIn`) since the auth cookie's role claims are fixed at sign-in time and won't reflect a mid-session role change otherwise. This "claims are stale until re-issued" behavior is a real gotcha worth carrying into the migration notes — a Java/Spring Security + JWT setup has the same class of problem (a token's claims are fixed at issuance) and needs the same kind of explicit refresh-or-short-expiry handling.

### 9. Custom error handling — `Error.aspx`, `Global.asax` `Application_Error`, `Web.config` `customErrors`

`Web.config` gains `<customErrors mode="RemoteOnly" defaultRedirect="~/Error.aspx"><error statusCode="404" redirect="~/Error.aspx"/></customErrors>`, and `Global.asax` gains an `Application_Error` handler that pulls the exception via `Server.GetLastError()` and logs it (`Trace.WriteLine` here as a stand-in — a real deployment would wire this to ELMAH, Serilog, Application Insights, etc.). `Error.aspx` is the plain user-facing page both paths land on. This is the app-wide safety net sitting above any single page or handler's own error handling — the nearest Java equivalent is a global `@ControllerAdvice`/`ExceptionHandler` (Spring) or a servlet-container `<error-page>` mapping.

### 10. `OutputCache` + `Session` + `Substitution` (donut caching) — `Dashboard.aspx`

Deliberately combines all three to show a real interaction, not just each in isolation:
- `<%@ OutputCache Duration="30" VaryByParam="None" %>` caches the **entire rendered page** for 30 seconds. The KPI summary (`ReportingRepository.GetSummary()` — customer count, order count, total revenue, computed via `App_Code/Data/ReportingRepository.cs`) is exactly the kind of aggregate query OutputCache exists to shield the database from — and critically, **`Page_Load` (and the whole page lifecycle) simply does not run at all on a cache hit**; the cached HTML bytes are served directly by the OutputCache module.
- Because of that, a naive `Session`-driven "visit counter" written in `Page_Load` would only actually increment once every cache window, not on every real visit — a genuine, easy-to-miss gotcha in production Web Forms apps.
- The fix demonstrated here is `<asp:Substitution runat="server" MethodName="GetVisitCountFragment" />` — "donut caching." A `Substitution` control's callback (`public static string GetVisitCountFragment(HttpContext context)` in `Dashboard.aspx.cs`) runs on **every** request regardless of cache state, and its return value is spliced into the otherwise-cached HTML afterward. The session-visit-count increment lives inside that callback specifically so it stays accurate even while the rest of the page is served from cache.

This whole interaction — full-page cache plus a live "hole" punched through it — is a distinctly ASP.NET-shaped mechanism. The Java/Spring equivalent would combine a `@Cacheable`-style response cache with either an ESI (Edge Side Includes) fragment or a client-side fetch for the live piece; the Angular equivalent for the live piece is simply a separate API call rendered client-side. Worth flagging explicitly in migration scoping: OutputCache + Substitution interactions like this one won't have a single 1:1 target pattern — they'll need to be redesigned, not transliterated.

## Not included (out of scope for this pass)

Per the earlier discussion, the following were scoped to a "comprehensive" tier and deliberately left out here to keep this pass focused: site navigation (`Web.sitemap`/`SiteMapDataSource`/`Menu`/`TreeView`), nested master pages and `App_Themes`, a second general-purpose `UserControl` beyond the template's `ViewSwitcher`/`OpenAuthProviders`, the MS Chart control, export-to-Excel/PDF, and resource localization (`App_GlobalResources`/`.resx`). (A `Repeater` did end up in scope incidentally — it's used for the role list in `Account/RoleManagement.aspx`.)

## Verification

The full site — original template plus everything above — precompiles cleanly with `aspnet_compiler.exe` (`App_Code.dll` plus one assembly per page/master/handler, exit code 0), confirming all new C# compiles and every markup-to-code-behind/`ObjectDataSource`-method/`Substitution`-callback binding resolves correctly. This was not run inside IIS/IIS Express, so it does not confirm runtime behavior (e.g. actual DB access against LocalDB) — only that the app is well-formed and would load.
