using System.Globalization;
using System.IO.Compression;

namespace HuddleBoard.Playbook;

/// <summary>
/// Builds every shipping form of Huddle Board from one source file.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><c>HuddleBoard.html</c> — standalone; works from file://, no server,
///   no updates.</item>
///   <item><c>deploy/</c> — static site for Azure App Service; installs to the
///   home screen, caches offline, updates from the URL.</item>
///   <item><c>huddle_artifact.html</c> — body-only form for publishing as a
///   Claude artifact.</item>
/// </list>
/// </remarks>
public static class AppBuilder
{
    private const string Head = """
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover,user-scalable=no">
        <meta name="mobile-web-app-capable" content="yes">
        <meta name="apple-mobile-web-app-capable" content="yes">
        <meta name="theme-color" content="#1E2521">
        <link rel="manifest" href="./manifest.webmanifest">
        <link rel="icon" href="./icon-192.png">
        <link rel="apple-touch-icon" href="./icon-192.png">
        </head>
        <body>

        """;

    /// <summary>
    /// The service worker. Cache-first for our own files so the app opens with
    /// the radio off. A new build lands as a waiting worker; the page shows
    /// "Update ready" and only swaps when the coach taps it, so an update can
    /// never interrupt a live play.
    /// </summary>
    private const string ServiceWorker = """
        /* Huddle Board service worker — version __VERSION__
           Cache-first for our own files so the app opens with the radio off. A new
           build lands as a waiting worker; the page shows "Update ready" and only
           swaps when the coach taps it, so an update can never interrupt a live play. */
        const VERSION = "__VERSION__";
        const CACHE = "huddle-" + VERSION;
        const ASSETS = ["./", "./index.html", "./manifest.webmanifest",
                        "./icon-192.png", "./icon-512.png", "./icon-maskable-512.png"];

        self.addEventListener("install", e => {
          e.waitUntil(caches.open(CACHE).then(c => c.addAll(ASSETS)));
        });

        self.addEventListener("activate", e => {
          e.waitUntil(
            caches.keys()
              .then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
              .then(() => self.clients.claim())
          );
        });

        self.addEventListener("message", e => {
          if (e.data && e.data.type === "SKIP_WAITING") self.skipWaiting();
        });

        self.addEventListener("fetch", e => {
          const req = e.request;
          if (req.method !== "GET") return;
          if (new URL(req.url).origin !== location.origin) return;   // fonts go to the network
          e.respondWith(
            caches.match(req).then(hit => hit || fetch(req).then(res => {
              const copy = res.clone();
              caches.open(CACHE).then(c => c.put(req, copy));
              return res;
            }).catch(() => caches.match("./index.html")))
          );
        });

        """;

    /// <summary>
    /// Azure App Service (Windows / IIS). Two things matter: the manifest needs
    /// its MIME type, and index.html + sw.js must NOT be cached by the browser
    /// or tablets will never see a new build.
    /// </summary>
    private const string WebConfig = """
        <?xml version="1.0" encoding="utf-8"?>
        <!-- Azure App Service (Windows / IIS). Two things matter here: the manifest
             needs its MIME type, and index.html + sw.js must NOT be cached by the
             browser or tablets will never see a new build. -->
        <configuration>
          <system.webServer>
            <staticContent>
              <remove fileExtension=".webmanifest" />
              <mimeMap fileExtension=".webmanifest" mimeType="application/manifest+json" />
              <remove fileExtension=".json" />
              <mimeMap fileExtension=".json" mimeType="application/json" />
            </staticContent>
            <defaultDocument>
              <files>
                <clear />
                <add value="index.html" />
              </files>
            </defaultDocument>
            <httpProtocol>
              <customHeaders>
                <add name="X-Content-Type-Options" value="nosniff" />
              </customHeaders>
            </httpProtocol>
          </system.webServer>
          <location path="index.html">
            <system.webServer><httpProtocol><customHeaders>
              <add name="Cache-Control" value="no-cache, must-revalidate" />
            </customHeaders></httpProtocol></system.webServer>
          </location>
          <location path="sw.js">
            <system.webServer><httpProtocol><customHeaders>
              <add name="Cache-Control" value="no-cache, must-revalidate" />
            </customHeaders></httpProtocol></system.webServer>
          </location>
          <location path="manifest.webmanifest">
            <system.webServer><httpProtocol><customHeaders>
              <add name="Cache-Control" value="no-cache, must-revalidate" />
            </customHeaders></httpProtocol></system.webServer>
          </location>
        </configuration>

        """;

    /// <summary>Netlify / Cloudflare Pages — the same two rules as IIS.</summary>
    private const string Headers = """
        /sw.js
          Cache-Control: no-cache, must-revalidate
        /index.html
          Cache-Control: no-cache, must-revalidate
        /manifest.webmanifest
          Content-Type: application/manifest+json
          Cache-Control: no-cache, must-revalidate

        """;

    private static string Manifest()
    {
        var j = new JsonWriter(indent: 2);
        j.StartObject();
        j.Pair("name", "Huddle Board");
        j.Pair("short_name", "Huddle");
        j.Pair("description", "Sideline play tool for 8U flag football.");
        j.Pair("start_url", "./");
        j.Pair("scope", "./");
        j.Pair("display", "fullscreen");
        j.Pair("display_override", ["fullscreen", "standalone", "minimal-ui"]);
        j.Pair("orientation", "landscape");
        j.Pair("background_color", "#1E2521");
        j.Pair("theme_color", "#1E2521");
        j.Key("icons").StartArray();
        foreach (var (src, sizes, maskable) in new[]
                 {
                     ("./icon-192.png", "192x192", false),
                     ("./icon-512.png", "512x512", false),
                     ("./icon-maskable-512.png", "512x512", true),
                 })
        {
            j.StartObject();
            j.Pair("src", src);
            j.Pair("sizes", sizes);
            j.Pair("type", "image/png");
            if (maskable)
                j.Pair("purpose", "maskable");
            j.EndObject();
        }

        j.EndArray();
        j.EndObject();
        return j.ToString();
    }

    private static string StaticWebApp()
    {
        var j = new JsonWriter(indent: 2);
        j.StartObject();
        j.Key("mimeTypes").StartObject();
        j.Pair(".webmanifest", "application/manifest+json");
        j.Pair(".json", "application/json");
        j.EndObject();
        j.Key("routes").StartArray();
        foreach (var route in new[] { "/sw.js", "/index.html", "/manifest.webmanifest" })
        {
            j.StartObject();
            j.Pair("route", route);
            j.Key("headers").StartObject();
            j.Pair("Cache-Control", "no-cache, must-revalidate");
            j.EndObject();
            j.EndObject();
        }

        j.EndArray();
        j.Key("navigationFallback").StartObject();
        j.Pair("rewrite", "/index.html");
        j.EndObject();
        j.EndObject();
        return j.ToString();
    }

    /// <summary>
    /// Builds <c>dist/</c> from <c>dist/proto_data.json</c> and
    /// <c>huddle_src.html</c>. Returns a process exit code.
    /// </summary>
    /// <param name="version">
    /// Overrides the stamped version. Falls back to <c>HB_VERSION</c>, then to
    /// the current time — the checks fake a new deploy by passing one.
    /// </param>
    public static int Run(string? version = null, TextWriter? output = null)
    {
        var o = output ?? Console.Out;
        var dist = Workspace.Ensure(Workspace.Dist);
        var deploy = Workspace.Ensure(Workspace.Deploy);

        version = version
            ?? Environment.GetEnvironmentVariable("HB_VERSION").NullIfBlank()
            ?? DateTime.Now.ToString("yyyy.MM.dd-HHmm", CultureInfo.InvariantCulture);

        var dataPath = Path.Combine(dist, "proto_data.json");
        if (!File.Exists(dataPath))
        {
            o.WriteLine($"no {dataPath} — export the data first");
            return 1;
        }

        var data = Read(dataPath);
        var src = Read(Workspace.Source)
            .Replace("__DATA__", data, StringComparison.Ordinal)
            .Replace("__VERSION__", version, StringComparison.Ordinal);

        var page = Head + src + "\n</body>\n</html>\n";

        Workspace.WriteText(Path.Combine(dist, "huddle_artifact.html"), src);
        Workspace.WriteText(Path.Combine(dist, "HuddleBoard.html"), page);
        Workspace.WriteText(Path.Combine(deploy, "index.html"), page);
        Workspace.WriteText(Path.Combine(deploy, "manifest.webmanifest"), Manifest());
        Workspace.WriteText(Path.Combine(deploy, "sw.js"),
            ServiceWorker.Replace("__VERSION__", version, StringComparison.Ordinal));
        Workspace.WriteText(Path.Combine(deploy, "web.config"), WebConfig);
        Workspace.WriteText(Path.Combine(deploy, "staticwebapp.config.json"), StaticWebApp());
        Workspace.WriteText(Path.Combine(deploy, "_headers"), Headers);
        Workspace.WriteText(Path.Combine(deploy, "README.md"), DeployReadme.For(version));

        var zipPath = Path.Combine(dist, "HuddleBoard-deploy.zip");
        var files = Directory.GetFiles(deploy).Select(Path.GetFileName)
            .OfType<string>().Order(StringComparer.Ordinal).ToList();
        File.Delete(zipPath);
        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var name in files)
                zip.CreateEntryFromFile(Path.Combine(deploy, name), name, CompressionLevel.Optimal);
        }

        o.WriteLine("version {0}", version);
        o.WriteLine("standalone  HuddleBoard.html      {0,6} KB", Kb(Path.Combine(dist, "HuddleBoard.html")));
        o.WriteLine("deploy zip  HuddleBoard-deploy.zip {0,6} KB", Kb(zipPath));
        foreach (var name in files)
        {
            o.WriteLine("   deploy/{0,-24} {1,7} bytes",
                name, new FileInfo(Path.Combine(deploy, name)).Length);
        }

        return 0;
    }

    /// <summary>
    /// Reads text and normalises line endings, so a CRLF checkout produces the
    /// same bytes as an LF one.
    /// </summary>
    private static string Read(string path) =>
        File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Kb(string path) =>
        (new FileInfo(path).Length / 1024.0).ToString("0.0", CultureInfo.InvariantCulture);

    private static string? NullIfBlank(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
