using HuddleBoard.Playbook;

namespace HuddleBoard.Tests;

/// <summary>
/// A copy of the built site, served, that a check can ship a new build to.
/// </summary>
/// <remarks>
/// The update checks fake a deploy by rebuilding with a new version. Doing that
/// in <c>dist/</c> changed the build under every other check, so they had to
/// run one at a time and put the real build back after every test. A copy of
/// its own lets a check deploy as often as it likes while the rest of the
/// suite keeps reading the build the fixture made.
/// </remarks>
public sealed class PrivateBuild : IDisposable
{
    private readonly string _dist = Directory.CreateTempSubdirectory("hb-build-").FullName;

    /// <param name="mount">Where the site is served; see <see cref="StaticSite"/>.</param>
    public PrivateBuild(string mount = "")
    {
        var deploy = Directory.CreateDirectory(Path.Combine(_dist, "deploy")).FullName;
        File.Copy(Path.Combine(Workspace.Dist, "proto_data.json"), Path.Combine(_dist, "proto_data.json"));
        foreach (var file in Directory.GetFiles(Workspace.Deploy))
            File.Copy(file, Path.Combine(deploy, Path.GetFileName(file)));
        Site = new StaticSite(deploy, mount);
    }

    /// <summary>The copy, served: the fixture's build until <see cref="Ship"/>.</summary>
    public StaticSite Site { get; }

    /// <summary>Rebuilds this copy as <paramref name="version"/>: a new deploy.</summary>
    public int Ship(string version) => AppBuilder.Run(version, TextWriter.Null, _dist);

    public void Dispose()
    {
        Site.Dispose();
        try
        {
            Directory.Delete(_dist, recursive: true);
        }
        catch (IOException)
        {
            // the browser can still be holding a file; it is a temp folder
        }
    }
}
