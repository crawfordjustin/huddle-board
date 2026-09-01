namespace HuddleBoard.Tests;

/// <summary>
/// Library names fit at the size the library is now and at the size it might
/// reach, across all five tablet shapes.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class LibraryFitChecks(AppFixture app)
{
    private const string Clipped =
        "els => els.filter(e => e.scrollWidth > e.clientWidth + 1).map(e=>e.textContent.trim())";

    public static TheoryData<int, string, int, int> Cases
    {
        get
        {
            var data = new TheoryData<int, string, int, int>();
            foreach (var count in new[] { 14, 100 })
            {
                foreach (var s in AppFixture.Sizes)
                    data.Add(count, s.Label, s.Width, s.Height);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task NoLibraryNameIsClipped(int plays, string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height), settle: 300);

        if (plays != 14)
            await AppFixture.InjectPlaysAsync(page, plays);

        await page.EvaluateAsync("renderLibrary()");
        await page.WaitForTimeoutAsync(700);

        var clipped = await page.EvalOnSelectorAllAsync<string[]>(".lname b", Clipped);
        await page.CloseAsync();

        Assert.True(clipped.Length == 0,
            $"{plays} plays at {label}: " + string.Join(", ", clipped.Take(4)));
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
