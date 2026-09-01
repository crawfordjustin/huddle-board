namespace HuddleBoard.Playbook;

/// <summary>
/// The build, in the order the steps have to happen. Both the command line and
/// the web host call this, so there is one definition of what "build" means.
/// </summary>
public static class Pipeline
{
    /// <summary>
    /// data -&gt; <c>dist/proto_data.json</c> -&gt; <c>dist/{HuddleBoard.html,
    /// deploy/, zip}</c>. Returns a process exit code.
    /// </summary>
    public static int Build(string? version = null, TextWriter? output = null)
    {
        var o = output ?? Console.Out;

        var code = ProtoExporter.Run(o);
        if (code != 0)
            return code;

        // icons change about once a year; only draw them when they are missing
        if (!File.Exists(Path.Combine(Workspace.Deploy, "icon-192.png")))
        {
            code = IconRenderer.Run(o);
            if (code != 0)
                return code;
        }

        return AppBuilder.Run(version, o);
    }

    /// <summary>True once there is a site in <c>dist/deploy/</c> to serve.</summary>
    public static bool IsBuilt => File.Exists(Path.Combine(Workspace.Deploy, "index.html"));
}
