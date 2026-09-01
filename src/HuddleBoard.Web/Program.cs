using HuddleBoard.Playbook;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

// Serves the built app. There is no server code in Huddle Board — this host
// exists so that F5 gives you the real thing over http, which is the only way
// the service worker, the install prompt and browser storage work at all. A
// file:// copy cannot do any of it.
//
// Two response rules are load-bearing, and they are the same two the generated
// web.config applies on Azure App Service:
//   1. .webmanifest must be served as application/manifest+json, or Chrome will
//      not offer to install the app.
//   2. index.html and sw.js must not be cached, or tablets never see a new
//      build.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var root = ResolveSiteRoot(app);
app.Logger.LogInformation("serving {Root}", root);

var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".webmanifest"] = "application/manifest+json";

var files = new PhysicalFileProvider(root);
var staticFiles = new StaticFileOptions
{
    FileProvider = files,
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = ctx =>
    {
        var headers = ctx.Context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        if (ctx.File.Name is "index.html" or "sw.js" or "manifest.webmanifest")
            headers.CacheControl = "no-cache, must-revalidate";
    },
};

app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
app.UseStaticFiles(staticFiles);

app.Run();

// Running from the repo serves dist/deploy, and rebuilds it first — every time,
// not just when it is missing. F5 has to show the source you have right now; a
// host that serves the previous build after you edit a play is worse than no
// host at all, because it lies quietly. The build takes a few hundred
// milliseconds, which is not worth being clever about.
//
// A published site has no repository above it, so the same files sit in wwwroot
// and are served as they were staged at publish time.
static string ResolveSiteRoot(WebApplication app)
{
    string deploy;
    try
    {
        deploy = Workspace.Deploy;
    }
    catch (InvalidOperationException)
    {
        return Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    }

    if (Pipeline.Build(output: TextWriter.Null) != 0)
    {
        throw new InvalidOperationException(
            "the play library failed its checks, so the site was not rebuilt — "
            + "run `check` to see what is wrong");
    }

    return deploy;
}
