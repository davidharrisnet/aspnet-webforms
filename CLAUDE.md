# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Framework version — do not change

This project must remain on .NET Framework 4.7.2 (classic ASP.NET Web Forms, `System.Web`), developed with Visual Studio 2017 — this is intentional, not a stale/legacy setup to be modernized. Do not port it to .NET Core/.NET 5+, do not migrate it to an SDK-style project or `PackageReference`, do not upgrade `targetFramework` in `Web.config` or the `TargetFrameworkMoniker` in the `.sln`, and do not suggest upgrading the Visual Studio version or tooling, unless explicitly asked. Web Forms itself has no path onto modern .NET, so any such change would be a full rewrite, not an upgrade.

## Project type

This is a stock ASP.NET Web Forms **Web Site** project (not a Web Application Project) targeting .NET Framework 4.7.2, scaffolded from the "Individual User Accounts" Visual Studio template. There is no `.csproj` — `AspNetWebForms.sln` references the `AspNetWebForms\` folder directly as a "Web Site" project (`ProjectTypeGuid` `E24C65DC-...`). Code under `App_Code/` and the `.aspx.cs` code-behind files are compiled dynamically by the ASP.NET runtime / `aspnet_compiler`, not by MSBuild against a project file.

Currently the site is unmodified scaffold: `Default.aspx`, `About.aspx`, `Contact.aspx`, and the `Account/*` Identity pages (Login, Register, Manage, external login) have no custom logic beyond what the template generates.

## Build, run, and test

There is no `dotnet` CLI tooling and no test project in this repo (Web Site projects predate the modern SDK-style tooling). Development is done through Visual Studio / IIS Express:

- Open `AspNetWebForms.sln` in Visual Studio and run (F5) — this hosts the site via IIS Express and compiles it in-place.
- To compile the site outside the IDE, use `aspnet_compiler.exe` (from the .NET Framework tools) against the `AspNetWebForms\` folder — there is no `msbuild`-able project file to build directly.
- NuGet packages are restored via `packages.config` (classic packages.config style, not `PackageReference`) into the top-level `packages/` folder, which is checked into source control as pre-restored binaries under `AspNetWebForms/Bin/`.
- There are no automated tests, linter, or formatter configured in this repo.

## Architecture

- **Routing**: `App_Code/RouteConfig.cs` enables ASP.NET Friendly URLs (`Microsoft.AspNet.FriendlyUrls`) with permanent auto-redirects, so `.aspx` extensions are optional in URLs.
- **Bundling/minification**: `App_Code/BundleConfig.cs` + `Bundle.config` configure `System.Web.Optimization` bundles for script/CSS.
- **Startup/OWIN**: `App_Code/Startup.cs` is the OWIN entry point (`[assembly: OwinStartup]`), which calls `ConfigureAuth` in `App_Code/Startup.Auth.cs`. Authentication is cookie-based (`Microsoft.Owin.Security.Cookies`) with the login path set to `/Account/Login`. External login providers (Microsoft, Twitter, Facebook, Google) are wired up but commented out — enabling one requires filling in a client ID/secret.
- **Identity/EF**: `App_Code/IdentityModels.cs` defines `ApplicationUser : IdentityUser`, `ApplicationDbContext : IdentityDbContext<ApplicationUser>` (EF6, connection string `DefaultConnection`), and a `UserManager` wrapper. `IdentityHelper` centralizes sign-in and safe local-redirect logic used by the `Account/*` pages.
- **Master pages**: `Site.master` is the main layout (Bootstrap-based); `Site.Mobile.master` is a mobile-specific layout selected via `ViewSwitcher.ascx`, which lets users toggle between desktop/mobile views (stored via a cookie, `Microsoft.AspNet.Providers`).
- **Database**: LocalDB via the `DefaultConnection` connection string in `Web.config`, targeting an `.mdf` file under `App_Data` (created on first run, not checked in). EF6 migrations are not currently configured (no `Migrations/` folder).
- **Config layering**: `Web.config` is the base config; `Web.Debug.config` applies XDT transforms for Debug builds. `Account/Web.config` layers auth restrictions (`<deny users="?" />`) specifically over the `Account/` folder.

## Conventions to note

- Namespaces are all `AspNetWebForms` (flat, no sub-namespacing by feature).
- Code-behind files pair 1:1 with their `.aspx`/`.ascx`/`.master` markup files; there is no code-behind/markup separation beyond that.
- `packages/` (NuGet cache) is committed to the repo — treat it as vendored, not something to hand-edit.
