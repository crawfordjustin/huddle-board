using HuddleBoard.Playbook;

namespace HuddleBoard.Tests;

/// <summary>
/// The licence travels with the thing being licensed.
/// </summary>
/// <remarks>
/// A coach hands another coach one HTML file over Bluetooth, or a zip lands on
/// somebody's static host, and neither of those carries the repository with it.
/// Both shipping forms have to say what they are on their own.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class LicenceChecks(AppFixture app)
{
    private const string Grant =
        "Permission is hereby granted, free of charge, to any person obtaining a";

    [Fact]
    public void TheRepositoryCarriesAnUnmodifiedMitLicence()
    {
        var text = File.ReadAllText(Path.Combine(Workspace.Root, "LICENSE"));

        Assert.StartsWith("MIT License", text, StringComparison.Ordinal);
        Assert.Contains("Copyright (c) 2026 Justin Crawford", text, StringComparison.Ordinal);
        Assert.Contains(Grant, text, StringComparison.Ordinal);
        Assert.Contains("shall be included in all", text, StringComparison.Ordinal);
        Assert.Contains("WITHOUT WARRANTY OF ANY KIND", text, StringComparison.Ordinal);

        // MIT and nothing bolted onto it. An extra clause is a different licence
        // wearing the name, which is worse than picking a different licence.
        foreach (var word in new[] { "non-commercial", "noncommercial", "share-alike",
                                     "ShareAlike", "patent", "trademark", "Additional" })
        {
            Assert.DoesNotContain(word, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void BothShippingFormsCarryIt()
    {
        var standalone = File.ReadAllText(Path.Combine(Workspace.Dist, "HuddleBoard.html"));
        var hosted = File.ReadAllText(Path.Combine(Workspace.Deploy, "index.html"));

        foreach (var (name, page) in new[] { ("HuddleBoard.html", standalone), ("index.html", hosted) })
        {
            Assert.Contains("MIT licence", page, StringComparison.Ordinal);
            Assert.Contains(Grant, page, StringComparison.Ordinal);
            Assert.Contains("github.com/crawfordjustin/huddle-board", page, StringComparison.Ordinal);
            Assert.DoesNotContain("__LICENCE__", page, StringComparison.Ordinal);

            // ...and it sits after the doctype. A comment ahead of it is legal,
            // but content before the doctype is the classic route into quirks
            // mode, and quirks mode would take the whole layout with it.
            Assert.StartsWith("<!doctype html>", page, StringComparison.Ordinal);
        }

        Assert.True(File.Exists(Path.Combine(Workspace.Deploy, "LICENSE.txt")),
            "the deploy folder — and so the zip — ships without a licence");
    }

    /// <summary>The rendered page is in standards mode, licence comment and all.</summary>
    [Fact]
    public async Task TheShippedPageIsNotInQuirksMode()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], intro: true);

        Assert.Equal("CSS1Compat", await page.EvaluateAsync<string>("document.compatMode"));
        Assert.True(await page.EvaluateAsync<bool>("!!document.doctype"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
