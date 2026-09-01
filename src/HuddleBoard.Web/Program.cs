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

// Development serves dist/deploy straight from the repo, so rebuilding the app
// and refreshing the tab is the whole loop. A published site has the same files
// copied into wwwroot.
static string ResolveSiteRoot(WebApplication app)
{
    string deploy;
    try
    {
        deploy = Workspace.Deploy;
    }
    catch (InvalidOperationException)
    {
        // published: there is no repository above us, so the files are in wwwroot
        return Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    }

    if (!Pipeline.IsBuilt)
    {
        app.Logger.LogInformation("no build in dist/ yet — building it");
        if (Pipeline.Build(output: TextWriter.Null) != 0)
            throw new InvalidOperationException("the build failed — run `check` to see why");
    }

    return deploy;
}
