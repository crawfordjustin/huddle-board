using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Build;

/// <summary>Regenerates the screenshots the README uses.</summary>
internal static class Screenshots
{
    public static async Task<int> RunAsync(TextWriter? output = null)
    {
        var o = output ?? Console.Out;
        var dir = Workspace.Ensure(Path.Combine(Workspace.Root, "docs"));

        // Always rebuild, not just when dist/ is empty. A screenshot of the
        // previous build is worse than no screenshot: it looks current.
        if (Pipeline.Build(output: o) != 0)
            return 1;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 },
        });

        await page.GotoAsync(new Uri(Path.Combine(Workspace.Dist, "HuddleBoard.html")).AbsoluteUri);
        await page.WaitForTimeoutAsync(600);
        await Shoot("intro.png");

        // a fresh tablet's first START is the tutorial, on its way to the deck
        await page.ClickAsync("#start");
        await page.WaitForTimeoutAsync(600);
        await Shoot("tutorial.png");

        await page.ClickAsync("#tourskip");
        await page.WaitForTimeoutAsync(600);
        await Shoot("deck.png");

        await page.EvaluateAsync("openPlay('p_18')");
        await page.WaitForTimeoutAsync(700);
        await Shoot("play.png");

        await page.EvaluateAsync("renderLibrary()");
        await page.WaitForTimeoutAsync(700);
        await Shoot("library.png");

        await browser.CloseAsync();
        return 0;

        async Task Shoot(string name)
        {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(dir, name) });
            o.WriteLine("wrote docs/{0}", name);
        }
    }
}
