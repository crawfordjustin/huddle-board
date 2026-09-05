using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>One of the five tablet shapes worth caring about.</summary>
public sealed record Viewport(string Label, int Width, int Height)
{
    public override string ToString() => $"{Label} {Width}x{Height}";
}

/// <summary>
/// Shared plumbing for the verification suite.
/// </summary>
/// <remarks>
/// Every check drives a real Chromium against a real build. There are no unit
/// tests on the UI on purpose: almost every bug this project has actually had
/// was a layout or timing bug that only shows up once the thing is on screen at
/// a particular size.
/// </remarks>
public sealed class AppFixture : IAsyncLifetime
{
    /// <summary>
    /// Two landscape, two portrait, and the short landscape that catches
    /// anything relying on vertical room.
    /// </summary>
    public static readonly Viewport[] Sizes =
    [
        new("landscape 16:10", 1600, 1000),
        new("landscape 4:3", 1280, 960),
        new("portrait 10in", 1200, 1920),
        new("portrait small", 800, 1280),
        new("landscape small", 1024, 600),
    ];

    /// <summary>The same five shapes, for [MemberData] sweeps.</summary>
    public static TheoryData<string, int, int> AllSizes
    {
        get
        {
            var data = new TheoryData<string, int, int>();
            foreach (var s in Sizes)
                data.Add(s.Label, s.Width, s.Height);
            return data;
        }
    }

    public IPlaywright Playwright { get; private set; } = null!;

    public IBrowser Browser { get; private set; } = null!;

    /// <summary>file:// URI of the standalone build.</summary>
    public string AppUri { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (Pipeline.Build(output: TextWriter.Null) != 0)
            throw new InvalidOperationException("the build failed — run `check` to see why");

        var standalone = Path.Combine(Workspace.Dist, "HuddleBoard.html");
        AppUri = new Uri(standalone).AbsoluteUri;

        if (Microsoft.Playwright.Program.Main(["install", "chromium"]) != 0)
            throw new InvalidOperationException("could not install Chromium for Playwright");

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync();
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
        Playwright.Dispose();
    }

    /// <summary>
    /// The first START on a tablet goes through the tutorial, and every check
    /// in the suite expects START to land on the deck. So a tablet is a tablet
    /// that has been through the slides unless a check says otherwise — this
    /// writes the same flag the slides write, before the app boots.
    /// <see cref="TutorialChecks"/> is what opens a tablet without it.
    /// </summary>
    public const string SeenTour = """try { localStorage.setItem("hb.tour", "1"); } catch(e){}""";

    public static Task MarkTourSeenAsync(IBrowserContext context) => context.AddInitScriptAsync(SeenTour);

    public static Task MarkTourSeenAsync(IPage page) => page.AddInitScriptAsync(SeenTour);

    /// <summary>
    /// A new page with the app loaded and the intro dismissed, sitting on the
    /// deck — which is where a coach is within one tap of launching, and where
    /// every check below this one expects to start.
    /// </summary>
    /// <param name="intro">
    /// Leave the intro screen up instead. Only the checks on the intro itself
    /// want this.
    /// </param>
    /// <param name="fresh">
    /// A tablet on its first open, which has not seen the tutorial. With
    /// <paramref name="intro"/> left false, START then lands on the slides
    /// rather than the deck, so this waits for the tour instead.
    /// </param>
    public async Task<(IPage Page, List<string> Errors)> OpenAppAsync(
        Viewport size, int settle = 400, bool intro = false, bool fresh = false)
    {
        var page = await Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = size.Width, Height = size.Height },
        });
        if (!fresh)
            await MarkTourSeenAsync(page);

        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);
        await page.GotoAsync(AppUri);
        await page.WaitForTimeoutAsync(settle);

        if (!intro)
        {
            await page.ClickAsync("#start");
            await page.WaitForSelectorAsync(fresh ? ".tour" : ".deck");
            await page.WaitForTimeoutAsync(settle);
        }

        return (page, errors);
    }

    /// <summary>
    /// Clones the real plays up to <paramref name="count"/> so the UI can be
    /// judged at a size the library will not reach for a while.
    /// </summary>
    public static async Task InjectPlaysAsync(IPage page, int count) =>
        await page.EvaluateAsync(InjectPlays, count);

    private const string InjectPlays = """
        (n) => {
          const base = DATA.plays.slice();
          const out = [];
          for (let i = 0; i < n; i++){
            const b = JSON.parse(JSON.stringify(base[i % base.length]));
            b.id = "s_" + i; b.num = i + 1;
            const suffix = " " + (Math.floor(i / base.length) + 1);
            b.coachName += suffix; b.kidName += suffix;
            out.push(b);
          }
          DATA.plays = out;
          return JSON.stringify(DATA).length;
        }
        """;
}

/// <summary>
/// Everything shares one browser and one build, and runs in sequence — two
/// checks racing each other over the same dist/ is not a real signal.
/// </summary>
[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "huddle board app";
}
