# Plan: a real website at `/`, the app at `/app/`

## Context

Today the Azure App Service *is* the app: CI zip-deploys `dist/deploy/` (a flat
static site with an IIS `web.config`) to the site root, so every visitor lands
straight in Huddle Board. The goal is a proper site at the root — home page,
marketing, about — that can grow APIs and a database later, with the app one
click away at `/app/`.

The site is pre-launch (no installed tablets in the wild), so no migration
bridge is needed beyond a kill-switch for the developer's own tablets. The
pages should be ASP.NET so APIs and a DB can follow.

### Why `/app/` on the same host, and what it means for people using the app

- **Saved data is safe.** Everything a coach keeps — deck, names, weeks, drawn
  plays, game log — is `localStorage`, which is keyed by *origin* (scheme +
  host), not path. `/app/` on the same host sees exactly the same storage. A
  subdomain would have been a new origin and every tablet would start empty.
- **The app does not change.** Every path it uses is already relative
  (`./sw.js`, `./manifest.webmanifest`, `start_url`/`scope` `"./"` —
  `AppBuilder.cs`), so the same files work under a subfolder. The service
  worker scopes itself to `/app/`, the installed icon launches `/app/`
  directly (never the marketing page), offline works as before, and the
  "Update ready" flow is untouched.
- **What users notice:** the address to install from becomes `…/app/`; anyone
  who bookmarked the root lands on the home page and taps *Open Huddle Board*.
  `dist/HuddleBoard.html` (the one-file offline copy) is unaffected.
- **One new operational cost:** an ASP.NET site on App Service can cold-start
  after idling, where a static site never did. Turn on **Always On** (Basic
  tier or above) so the first open before a game is not the slow one. Once the
  service worker has the app cached, a cold server no longer matters to it.

## Approach

`src/HuddleBoard.Web` is already an ASP.NET Core host that serves the built app
and stages it into `wwwroot` on publish. It becomes the real site:

### 1. Razor Pages for the site — `src/HuddleBoard.Web`
- `builder.Services.AddRazorPages()` / `app.MapRazorPages()`.
- `Pages/_ViewStart.cshtml`, `Pages/Shared/_Layout.cshtml` (header with
  *Open Huddle Board* → `/app/`), `Pages/Index.cshtml` (home), `Pages/About.cshtml`.
- Site CSS/images in the project's `wwwroot/` (`css/site.css`), served by the
  normal `UseStaticFiles()`. Reuse the app's palette (`--oob`, `--green`,
  Barlow Condensed) so the two read as one product.
- Leave an obvious place for later: a commented `app.MapGroup("/api")` and no
  DB yet — nothing is added that has no caller.

### 2. Serve the app under `/app/` — `Program.cs`
- One `PhysicalFileProvider` for the app: `Workspace.Deploy` when running from
  the repo (rebuilt every F5, as now — `ResolveSiteRoot`), `wwwroot/app` when
  published.
- `UseDefaultFiles` + `UseStaticFiles` with `RequestPath = "/app"` and that
  provider, keeping the existing `OnPrepareResponse` rules (manifest MIME type;
  `no-cache, must-revalidate` on `index.html`, `sw.js`, `manifest.webmanifest`).
- **Redirect `/app` → `/app/` (301).** Load-bearing: without the trailing slash
  `./sw.js` resolves to `/sw.js` and the app installs nothing.
- **Root `/sw.js` kill-switch** (static file in `wwwroot/`, served no-cache):
  on install `skipWaiting`, on activate delete every `huddle-*` cache and
  `self.registration.unregister()`. Any browser that ever ran the app at `/`
  (the developer's tablets) otherwise keeps serving the old app from cache at
  `/` forever, because a 404 on the update check keeps the old worker.

### 3. Publish stages the app into `wwwroot/app/` — `HuddleBoard.Web.csproj`
- In `BuildHuddleBoardSite`, change `RelativePath` to `wwwroot\app\%(Filename)%(Extension)`.
- Exclude the static-host config from staging (`web.config`,
  `staticwebapp.config.json`, `_headers`, `README.md`): a nested IIS
  `web.config` under an ASP.NET Core app is a conflict waiting to happen, and
  ASP.NET now applies those rules itself. `dist/deploy/` and
  `HuddleBoard-deploy.zip` stay exactly as they are — still a complete static
  site for anyone hosting the app alone.

### 4. CI deploys the ASP.NET app — `.github/workflows/deploy.yml`
- Keep `check` as the gate. Replace *Build the site* + zip deploy with one
  step that exports `HB_VERSION` and runs
  `dotnet publish src/HuddleBoard.Web -c Release -o publish` (the publish
  target runs `build`, inheriting `HB_VERSION` from the same step), then
  `azure/webapps-deploy@v3` with `package: publish`.
- OIDC login, environment, concurrency: unchanged.
- **One-time Azure setting, before the first deploy:** the app's stack must be
  .NET 10 on Windows (`az webapp config set -g rg-default -n huddleboard
  --net-framework-version v10.0`) and Always On enabled. If .NET 10 is not
  offered on the plan, publish self-contained instead
  (`-r win-x64 --self-contained`) — larger, no runtime dependency.

### 5. Docs
- `CLAUDE.md`: solution overview (Web is now the site, not only a dev host),
  the `/app/` rule and why (same origin = same storage; trailing-slash
  redirect), Deploy / pipeline section (publish, not zip; Always On; stack).
- `DeployReadme.cs`: install address is `…/app/`.

## Verification
- `dotnet run --project src/HuddleBoard.Build -- check` (deploy gate).
- F5 / `dotnet run --project src/HuddleBoard.Web`: `/` and `/about` render;
  *Open Huddle Board* lands on `/app/`; `/app` redirects to `/app/`;
  DevTools → Application shows the worker scoped to `/app/`, the manifest
  installable, data from the old root visible (same origin).
- Kill-switch: in a browser profile that has the old root worker, load `/`
  once and confirm the root registration and `huddle-*` caches are gone.
- `dotnet publish src/HuddleBoard.Web -c Release -o publish` and inspect:
  `wwwroot/app/index.html`, `sw.js`, manifest, icons present; no nested
  `web.config`.
- Tests: give `tests/HuddleBoard.Tests/StaticSite.cs` an optional mount path
  and run `PwaChecks` once against the app served at `/app/` (install offer,
  update walk-through) — the existing root-served runs stay as the static-host
  guarantee. Then `dotnet test`.
- After the first CI deploy: the live `/` shows the home page, `/app/` installs
  and opens offline on a tablet.
