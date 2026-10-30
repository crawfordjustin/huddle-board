using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// A coach's notes on a play: written from the play screen, read there and in
/// the library preview, kept through an update and through Start over, and
/// cleared from the one Setup row that offers it.
/// </summary>
/// <remarks>
/// Hosted rather than standalone, for the same reason as RenameChecks: the
/// point is storage, and the update is a real one taken through the service worker.
/// </remarks>
public sealed class NoteChecks(AppFixture app) : IDisposable
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    /// <summary>Play 1 ships in the default deck.</summary>
    private const string Tile = ".tile[data-id=\"p_01\"]";

    private const string Row = ".lrow[data-id=\"p_01\"]";

    /// <summary>Markup and a line break, because a note is typed and a few lines long.</summary>
    private const string Typed = "<b>ORANGE</b> sells the fake & holds\nOnly inside their 10";

    private readonly PrivateBuild _build = new();

    public void Dispose() => _build.Dispose();

    [Fact]
    public async Task NotesAreWrittenOnThePlaySurviveAnUpdateAndStartOverAndClearOnDemand()
    {
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Desk.Width, Height = Desk.Height },
        });
        await AppFixture.MarkTourSeenAsync(context);
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        await page.GotoAsync(_build.Site.Origin + "/index.html");
        await page.WaitForTimeoutAsync(1500);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");

        async Task OpenPlayAsync()
        {
            await page.ClickAsync(Tile);
            await page.WaitForSelectorAsync("#notes");
            await page.WaitForTimeoutAsync(300);
        }

        // a play with no note offers to add one, and the box opens ready to type
        await OpenPlayAsync();
        Assert.Equal("NOTES\nADD", (await page.InnerTextAsync("#notes")).Trim().ToUpperInvariant());
        Assert.Equal(0, await page.Locator("#notes.has").CountAsync());
        await page.ClickAsync("#notes");
        await page.WaitForSelectorAsync("#ntsheet");
        Assert.Equal("nt-text", await page.EvaluateAsync<string>("document.activeElement.id"));
        await page.FillAsync("#nt-text", Typed);
        await page.ClickAsync("#nt-save");
        await page.WaitForTimeoutAsync(300);

        Assert.Equal(0, await page.Locator("#ntsheet").CountAsync());
        Assert.Equal(1, await page.Locator("#notes.has").CountAsync());
        Assert.Equal("NOTES\nREAD", (await page.InnerTextAsync("#notes")).Trim().ToUpperInvariant());

        // with a note it opens to read: the text as text, and no keyboard thrown up over it
        await page.ClickAsync("#notes");
        await page.WaitForSelectorAsync("#ntsheet");
        Assert.Equal(Typed, await page.InputValueAsync("#nt-text"));
        Assert.NotEqual("nt-text", await page.EvaluateAsync<string>("document.activeElement.id"));
        Assert.Equal(0, await page.Locator("#ntsheet .sheet b:has-text('ORANGE')").CountAsync());
        await page.ClickAsync("#nt-cancel");

        // a kid's steps take the rail, and the coach's notes wait
        await page.EvaluateAsync("soloOn(Object.keys(S.play.assign)[0])");
        await page.WaitForTimeoutAsync(300);
        Assert.True(await page.EvaluateAsync<bool>("!!S.steps"), "no steps to hold the rail");
        Assert.True(await page.Locator("#notes").IsHiddenAsync(), "Notes stayed on a kid's steps");
        await page.ClickAsync("#back");
        await page.WaitForSelectorAsync(Tile);

        // the library preview shows it, as text
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForSelectorAsync(Row);
        await page.ClickAsync(Row + " .lthumb");
        await page.WaitForSelectorAsync("#prevsheet .prevnote");
        Assert.StartsWith("<b>ORANGE</b> sells the fake", (await page.InnerTextAsync(".prevnote")).Trim());
        Assert.Equal(0, await page.Locator(".prevnote b").CountAsync());
        await page.ClickAsync("#prev-close");
        await page.ClickAsync("#done");
        await page.WaitForSelectorAsync(Tile);

        // ship a new build and take it. The note is the coach's, not the build's.
        var installed = await page.EvaluateAsync<string>("APP_BUILD");
        Assert.Equal(0, _build.Ship("9.9.9-notes"));
        await page.EvaluateAsync("navigator.serviceWorker.getRegistration().then(r => r.update())");
        await page.WaitForTimeoutAsync(2500);
        await page.ClickAsync("#upd");
        await page.WaitForTimeoutAsync(2500);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        Assert.NotEqual(installed, await page.EvaluateAsync<string>("APP_BUILD"));
        await OpenPlayAsync();
        Assert.Equal(1, await page.Locator("#notes.has").CountAsync());
        await page.ClickAsync("#back");
        await page.WaitForSelectorAsync(Tile);

        // Start over puts back what shipped, and a note is not that
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await AppFixture.SetupTabAsync(page, "tablet");
        await page.ClickAsync("#allreset");
        await page.ClickAsync("#allreset");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(Typed, await page.EvaluateAsync<string>("notes.p_01"));

        // Clear all is the one thing that removes them, and only on the second tap
        await page.EvaluateAsync("renderSettings('yours')");
        await page.WaitForSelectorAsync("#ntclear");
        Assert.Contains("1 play in", await page.InnerTextAsync(".setrows"));
        await page.ClickAsync("#ntclear");
        await page.WaitForTimeoutAsync(200);
        Assert.Equal("SURE?", (await page.InnerTextAsync("#ntclear")).Trim().ToUpperInvariant());
        Assert.Equal(1, await page.EvaluateAsync<int>("notedCount()"));
        await page.ClickAsync("#ntclear");
        await page.WaitForTimeoutAsync(300);
        Assert.True(await page.Locator("#ntclear").IsDisabledAsync(), "Clear all is live with nothing left");

        // and it reached storage
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(1000);
        Assert.Equal(0, await page.EvaluateAsync<int>("notedCount()"));

        await context.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Emptying the box takes the note off; a drawn play takes its note with it
    /// when it is deleted; a hidden play keeps its note.
    /// </summary>
    [Fact]
    public async Task ANoteGoesWithItsPlayAndAnEmptyBoxIsNoNote()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);

        await page.EvaluateAsync("notes = {}; setNote('p_01', '  \\n\\n  '); ");
        Assert.Equal(0, await page.EvaluateAsync<int>("notedCount()"));

        await page.EvaluateAsync("setNote('p_01', 'a\\n\\n\\n\\nb   '); setHidden(['p_01']);");
        Assert.Equal("a\n\nb", await page.EvaluateAsync<string>("notes.p_01"));
        await page.EvaluateAsync("setHidden([]);");

        await page.EvaluateAsync("setNote('p_01', '')");
        Assert.Equal(0, await page.EvaluateAsync<int>("notedCount()"));

        // a deleted drawn play leaves no note behind it, in memory or in storage
        await page.EvaluateAsync("setNote('u_gone0000', 'x'); deleteDesign('u_gone0000'); loadNotes();");
        Assert.Equal(0, await page.EvaluateAsync<int>("notedCount()"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
