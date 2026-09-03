namespace HuddleBoard.Tests;

/// <summary>
/// The library screen: filters, search, the empty state, and the fact that
/// adding a play does not silently drop the filter you were using.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class LibraryChecks(AppFixture app)
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    [Fact]
    public async Task FiltersAndSearchNarrowTheList()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForTimeoutAsync(500);

        async Task<int> Rows() =>
            await page.EvaluateAsync<int>("() => document.querySelectorAll('.lrow').length");

        var all = await Rows();
        Assert.Equal(HuddleBoard.Playbook.PlayLibrary.All.Count, all);

        await page.ClickAsync("[data-kind=\"run\"]");
        await page.WaitForTimeoutAsync(300);
        var runs = await Rows();
        Assert.InRange(runs, 1, all - 1);
        Assert.True(
            await page.EvalOnSelectorAllAsync<bool>(".lkind", "e=>e.every(x=>x.textContent=='Run')"),
            "the Run filter let a non-run through");

        await page.ClickAsync("[data-kind=\"pass\"]");
        await page.WaitForTimeoutAsync(300);
        Assert.InRange(await Rows(), 1, all - 1);

        await page.ClickAsync("[data-kind=\"all\"]");
        await page.ClickAsync("[data-cat=\"GOAL LINE\"]");
        await page.WaitForTimeoutAsync(300);
        var goalLine = await Rows();
        Assert.InRange(goalLine, 1, all - 1);

        await page.ClickAsync("[data-cat=\"all\"]");
        await page.WaitForTimeoutAsync(200);
        await page.FillAsync("#lq", "wheel");
        await page.WaitForTimeoutAsync(350);
        Assert.InRange(await Rows(), 1, all - 1);
        Assert.True(await page.EvaluateAsync<bool>("document.activeElement.id === 'lq'"),
            "typing in the search box lost focus, so the next keystroke goes nowhere");

        await page.FillAsync("#lq", "zzzz");
        await page.WaitForTimeoutAsync(350);
        Assert.Equal(0, await Rows());
        Assert.Equal(1, await page.Locator(".lempty").CountAsync());

        await page.ClickAsync("#lclear");
        await page.WaitForTimeoutAsync(350);
        Assert.Equal(all, await Rows());

        await page.ClickAsync("#lonly");
        await page.WaitForTimeoutAsync(300);
        Assert.InRange(await Rows(), 1, all - 1);

        // toggling a play while filtered must not drop the filter
        await page.ClickAsync("#lonly");
        await page.ClickAsync("[data-cat=\"RUN ZONE\"]");
        await page.WaitForTimeoutAsync(300);
        var before = await Rows();
        await page.EvalOnSelectorAsync(".lrow", "e=>e.click()");
        await page.WaitForTimeoutAsync(350);
        Assert.Equal(before, await Rows());
        Assert.True(
            await page.EvalOnSelectorAsync<bool>("[data-cat=\"RUN ZONE\"]",
                "e=>e.classList.contains('on')"),
            "adding a play while filtered dropped the filter");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A hundred plays is well past where the library sits today, and it is the
    /// size that found the <c>min-height: 0</c> bug that silently capped the
    /// list at about twenty-two.
    /// </summary>
    [Fact]
    public async Task TheLibraryStillWorksAtAHundredPlays()
    {
        var (page, errors) = await app.OpenAppAsync(Desk, settle: 300);
        await AppFixture.InjectPlaysAsync(page, 100);
        await page.EvaluateAsync("renderLibrary()");
        await page.WaitForTimeoutAsync(600);

        var rows = await page.EvaluateAsync<int>("() => document.querySelectorAll('.lrow').length");
        Assert.Equal(100, rows);

        await page.ClickAsync("[data-cat=\"GOAL LINE\"]");
        await page.WaitForTimeoutAsync(400);
        Assert.InRange(await page.EvaluateAsync<int>("() => document.querySelectorAll('.lrow').length"),
            1, 99);

        await page.ClickAsync("[data-cat=\"all\"]");
        await page.WaitForTimeoutAsync(300);
        var ms = await page.EvaluateAsync<double>("""
            () => {const t=performance.now();
                   document.querySelector('[data-kind="pass"]').click();
                   return performance.now()-t;}
            """);
        Assert.True(ms < 400, $"one filter tap took {ms:F0} ms at 100 plays");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
