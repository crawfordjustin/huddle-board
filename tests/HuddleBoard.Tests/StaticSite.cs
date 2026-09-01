using System.Net;
using System.Text;

namespace HuddleBoard.Tests;

/// <summary>
/// Serves a directory over http so the service-worker checks can run — a
/// service worker will not register on file://.
/// </summary>
/// <remarks>
/// Port 0 lets the OS pick, which is what stops two checks running back to back
/// from fighting over a fixed port.
/// </remarks>
public sealed class StaticSite : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _root;
    private readonly CancellationTokenSource _stopping = new();

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".json"] = "application/json",
        [".webmanifest"] = "application/manifest+json",
        [".png"] = "image/png",
        [".md"] = "text/markdown; charset=utf-8",
    };

    public StaticSite(string root)
    {
        _root = root;
        Origin = $"http://127.0.0.1:{FreePort()}";
        _listener.Prefixes.Add(Origin + "/");
        _listener.Start();
        _ = Task.Run(ServeAsync);
    }

    /// <summary>Where the site is, for example <c>http://127.0.0.1:51234</c>.</summary>
    public string Origin { get; }

    private static int FreePort()
    {
        using var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private async Task ServeAsync()
    {
        while (!_stopping.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch (Exception) when (_stopping.IsCancellationRequested)
            {
                return;
            }
            catch (HttpListenerException)
            {
                return;
            }

            try
            {
                Respond(ctx);
            }
            catch (HttpListenerException)
            {
                // the browser hung up mid-response; nothing useful to do
            }
            finally
            {
                ctx.Response.Close();
            }
        }
    }

    private void Respond(HttpListenerContext ctx)
    {
        var relative = Uri.UnescapeDataString(ctx.Request.Url?.AbsolutePath ?? "/").TrimStart('/');
        if (relative.Length == 0)
            relative = "index.html";

        var path = Path.GetFullPath(Path.Combine(_root, relative));
        if (!path.StartsWith(Path.GetFullPath(_root), StringComparison.Ordinal) || !File.Exists(path))
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.OutputStream.Write(Encoding.UTF8.GetBytes("not found"));
            return;
        }

        var bytes = File.ReadAllBytes(path);
        ctx.Response.ContentType =
            ContentTypes.GetValueOrDefault(Path.GetExtension(path), "application/octet-stream");
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes);
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _listener.Close();
        _stopping.Dispose();
    }
}
