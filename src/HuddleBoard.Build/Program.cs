using HuddleBoard.Build;
using HuddleBoard.Playbook;

// The build tool. This is what `make` used to be.
//
//   dotnet run --project src/HuddleBoard.Build -- build    data -> dist/
//   dotnet run --project src/HuddleBoard.Build -- check    fast play-library check
//   dotnet run --project src/HuddleBoard.Build -- icons    regenerate app icons
//   dotnet run --project src/HuddleBoard.Build -- print    the paper playbook, as PDFs
//   dotnet run --project src/HuddleBoard.Build -- shots    regenerate the README screenshots
//
// In Visual Studio, set HuddleBoard.Build as the startup project and pick the
// launch profile for the verb you want.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var verb = args.Length > 0 ? args[0].ToLowerInvariant() : "build";

try
{
    return verb switch
    {
        "build" => Build(),
        "check" => PlayChecker.Run(),
        "export" => ProtoExporter.Run(),
        "icons" => IconRenderer.Run(),
        "print" => await PrintPipeline.RunAsync(),
        "shots" => await Screenshots.RunAsync(),
        "clean" => Clean(),
        _ => Usage(verb),
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

// data -> dist/proto_data.json -> dist/{HuddleBoard.html, deploy/, zip}
static int Build()
{
    var code = ProtoExporter.Run();
    if (code != 0)
        return code;

    if (!File.Exists(Path.Combine(Workspace.Deploy, "icon-192.png")))
    {
        code = IconRenderer.Run();
        if (code != 0)
            return code;
    }

    return AppBuilder.Run();
}

static int Clean()
{
    if (Directory.Exists(Workspace.Dist))
        Directory.Delete(Workspace.Dist, recursive: true);
    Console.WriteLine("removed dist/");
    return 0;
}

static int Usage(string verb)
{
    Console.Error.WriteLine($"unknown target '{verb}' — try build, check, export, icons, print, shots, clean");
    return 2;
}
