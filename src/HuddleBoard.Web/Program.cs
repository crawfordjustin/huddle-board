using HuddleBoard.Playbook;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

// The Huddle Board website. The pages at the root — home, about — are Razor
// Pages; the app itself is the built static site, mounted at /app/.
//
// The app lives under /app/ on the same host, never on a subdomain, because
// everything a coach keeps is localStorage and localStorage is keyed by origin.
// Same host, same storage: a tablet that used the app at the root still has its
// deck at /app/.
//
// Three response rules are load-bearing for the app, and the first two are the
// same two the generated web.config applies when dist/deploy is hosted alone:
//   1. .webmanifest must be served as application/manifest+json, or Chrome will
//      not offer to install the app.
//   2. index.html and sw.js must not be cached, or tablets never see a new
//      build.
//   3. /app must redirect to /app/. Every path the app uses is relative, so
//      without the trailing slash ./sw.js resolves to /sw.js and nothing
//      installs.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
var app = builder.Build();

var appRoot = ResolveAppRoot(app);
app.Logger.LogInformation("serving the app from {Root}", appRoot);

var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".webmanifest"] = "application/manifest+json";
contentTypes.Mappings[".vtt"] = "text/vtt";

var appFiles = new PhysicalFileProvider(appRoot);

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.Value is "/app")
    {
        ctx.Response.Redirect("/app/" + ctx.Request.QueryString, permanent: true);
        return;
    }
    await next();
});

app.UseDefaultFiles(new DefaultFilesOptions { RequestPath = "/app", FileProvider = appFiles });
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/app",
    FileProvider = appFiles,
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = ctx =>
    {
        var headers = ctx.Context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        if (ctx.File.Name is "index.html" or "sw.js" or "manifest.webmanifest")
            headers.CacheControl = "no-cache, must-revalidate";
    },
});

// The site's own files. /sw.js here is not the app's worker but the one that
// removes it: see wwwroot/sw.js. It has to be revalidated on every check for the
// same reason the app's has to.
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = ctx =>
    {
        var headers = ctx.Context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        if (ctx.File.Name is "sw.js")
            headers.CacheControl = "no-cache, must-revalidate";
    },
});

app.MapRazorPages();

// The Play Store app is a Trusted Web Activity: Chrome opening /app/ with no
// address bar. Chrome hides the bar only once this file, at the root of the
// origin and not under /app/, names the Android package and the certificate
// that signed it. It is served from here rather than from wwwroot because
// static files skip anything under a dot-folder. The fingerprint is Play App
// Signing's SHA-256 (Play Console → App integrity → Play app signing), which is
// public. A sideloaded APK is signed with the upload key instead, so it shows
// the address bar unless that key's fingerprint is added here too.
app.MapGet("/.well-known/assetlinks.json", () => Results.Json(new[]
{
    new
    {
        relation = new[] { "delegate_permission/common.handle_all_urls" },
        target = new
        {
            @namespace = "android_app",
            package_name = "org.kidsflaghuddle.huddleboard",
            sha256_cert_fingerprints = new[]
            {
                // Play App Signing: every copy installed from the Play Store.
                "8E:D5:C7:D1:F0:37:FA:0D:00:05:DE:1E:9D:94:7D:9C:EC:B7:45:13:E0:61:A2:66:1F:09:37:6F:D3:43:69:49",
            },
        },
    },
}));

// When the site grows an API, it goes here, under one prefix the app's paths
// and the pages can never collide with:
// var api = app.MapGroup("/api");

app.Run();

// Running from the repo serves dist/deploy, and rebuilds it first — every time,
// not just when it is missing. F5 has to show the source you have right now; a
// host that serves the previous build after you edit a play is worse than no
// host at all, because it lies quietly. The build takes a few hundred
// milliseconds, which is not worth being clever about.
//
// A published site has no repository above it, so the same files sit in
// wwwroot/app and are served as they were staged at publish time.
static string ResolveAppRoot(WebApplication app)
{
    string deploy;
    try
    {
        deploy = Workspace.Deploy;
    }
    catch (InvalidOperationException)
    {
        return Path.Combine(app.Environment.ContentRootPath, "wwwroot", "app");
    }

    if (Pipeline.Build(output: TextWriter.Null) != 0)
    {
        throw new InvalidOperationException(
            "the play library failed its checks, so the site was not rebuilt — "
            + "run `check` to see what is wrong");
    }

    return deploy;
}
