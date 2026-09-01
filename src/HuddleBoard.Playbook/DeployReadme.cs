namespace HuddleBoard.Playbook;

/// <summary>
/// The deploy notes that ship inside <c>dist/deploy/</c>: how to get the app
/// onto a host, and onto a coach's tablet.
/// </summary>
internal static class DeployReadme
{
    /// <summary>The notes, stamped with the build version.</summary>
    public static string For(string version) =>
        Template.Replace("__VERSION__", version, StringComparison.Ordinal);

    private const string Template = """
        # Huddle Board — deploy

        Version **__VERSION__**. Static files only: no server code, no build step, no dependencies.

        ## Azure App Service

        Drop the contents of this folder at the site root (`/site/wwwroot` on Windows,
        `/home/site/wwwroot` on Linux). Any of these work:

        * **Zip deploy** — `az webapp deploy --resource-group <rg> --name <app> --src-path deploy.zip --type zip`
        * **FTPS** — upload the six files
        * **GitHub Actions** — point `azure/webapps-deploy` at this folder

        ## Any static host works — the config for each is included

        Whatever you use, only two things actually matter: serve `.webmanifest` as
        `application/manifest+json`, and send `Cache-Control: no-cache` for
        `index.html` and `sw.js`. Miss the second one and tablets will never see a new
        build. The right file for your host is already in this folder:

        | host | file it reads | notes |
        |---|---|---|
        | Azure App Service (Windows) | `web.config` | drop-in |
        | Azure Static Web Apps | `staticwebapp.config.json` | free tier, good fit |
        | Netlify / Cloudflare Pages | `_headers` | drag and drop this folder |
        | GitHub Pages | none needed | headers are fine by default |
        | Azure App Service (Linux) | neither | set the two headers in your container |

        The extra config files are inert on hosts that do not read them.

        ## What will NOT work

        **OneDrive, SharePoint, Dropbox, Google Drive.** They serve a viewer page and
        then a tokenised redirect to a download host, so there is no stable origin,
        relative paths like `./sw.js` never resolve, and a service worker cannot
        register. Sharing `HuddleBoard.html` through them is fine — that is file
        distribution, not hosting.

        HTTPS is required for the service worker. App Service and every host above
        give you that for free on their default hostname.

        HTTPS is required for the service worker. App Service gives you that on the
        default `*.azurewebsites.net` hostname.

        ## Getting it onto a coach's tablet — and losing the address bar

        A web page cannot hide Chrome's address bar in an ordinary tab. Full screen
        comes from **installing** it:

        1. Open the URL in Chrome (must be **https**, not a local file).
        2. Menu → **Install app** (or **Add to Home screen**).
        3. Launch it from the new home-screen icon — *not* from a Chrome tab.

        It then opens with no browser UI at all, locked to landscape, and works with
        the radio off.

        If the menu offers only a plain bookmark-style shortcut and not **Install
        app**, the install criteria are not being met. Check, in order: the page is on
        https, `manifest.webmanifest` returns HTTP 200 with type
        `application/manifest+json`, and `sw.js` registered (DevTools → Application →
        Service Workers). A local `file://` copy can never be installed.

        **Meanwhile there is a Full screen button** on the deck screen whenever the app
        is running in a tab. It uses the Fullscreen API, which works in a tab and even
        from a local file, and it also locks the orientation — Android only permits
        orientation lock in fullscreen or an installed app. The button hides itself
        once the app is installed, because then it is already full screen.

        ## Shipping an update

        Deploy new files. Next time a coach opens the app it notices the new build,
        downloads it in the background, and shows **Update ready** on the deck screen.
        It swaps only when they tap it — never mid-play. The version is shown at the
        bottom of the Settings screen.

        ## Running it without a server

        `HuddleBoard.html` is the whole app in one file. Copy it to a tablet and open
        it from Files. No install, no network, no updates. Note that Android may block
        storage on `file://` URLs, in which case deck and settings will not persist —
        that is the main reason to prefer the hosted copy.

        ## Files

        | file | what it is |
        |---|---|
        | `index.html` | the entire app — markup, styles, script, all 14 plays |
        | `sw.js` | offline cache + update-on-tap |
        | `manifest.webmanifest` | name, icons, landscape, standalone launch |
        | `icon-*.png` | home screen icons |
        | `web.config` | IIS MIME types and cache headers |

        """;
}
