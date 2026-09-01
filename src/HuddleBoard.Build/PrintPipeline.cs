using HuddleBoard.Playbook;
using HuddleBoard.Playbook.Print;

using Microsoft.Playwright;

namespace HuddleBoard.Build;

/// <summary>
/// The paper fallbacks: the playbook, the field cards and the rotation sheet.
/// Each is generated as HTML and then printed to PDF by a real Chromium, which
/// is the same engine the tablet runs — so what you see is what prints.
/// </summary>
internal static class PrintPipeline
{
    private sealed record Sheet(string Name, string Html, string Pdf, bool Landscape);

    public static async Task<int> RunAsync(TextWriter? output = null)
    {
        var o = output ?? Console.Out;
        var dir = Workspace.Ensure(Workspace.Print);

        Sheet[] sheets =
        [
            new("playbook.html", PrintDocuments.Playbook(), "8U-Flag-Football-Playbook.pdf", false),
            new("cards.html", PrintDocuments.Cards(), "8U-Field-Cards.pdf", true),
            new("rotation.html", PrintDocuments.Rotation(), "8U-Rotation-Sheet.pdf", true),
        ];

        foreach (var sheet in sheets)
        {
            Workspace.WriteText(Path.Combine(dir, sheet.Name), sheet.Html);
            o.WriteLine("wrote {0}", sheet.Name);
        }

        EnsureBrowser(o);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        foreach (var sheet in sheets)
        {
            var source = new Uri(Path.Combine(dir, sheet.Name)).AbsoluteUri;
            await page.GotoAsync(source, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.PdfAsync(new PagePdfOptions
            {
                Path = Path.Combine(dir, sheet.Pdf),
                Format = "Letter",
                Landscape = sheet.Landscape,
                PrintBackground = true,
                Margin = new Margin { Top = "0", Bottom = "0", Left = "0", Right = "0" },
            });
            o.WriteLine("wrote {0}", sheet.Pdf);
        }

        await browser.CloseAsync();
        return 0;
    }

    /// <summary>
    /// Downloads Chromium the first time. Playwright ships the driver in the
    /// package but not the browser, and the download is a few hundred megabytes.
    /// </summary>
    private static void EnsureBrowser(TextWriter o)
    {
        var code = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (code != 0)
            throw new InvalidOperationException("could not install Chromium for Playwright");
        o.WriteLine("chromium ready");
    }
}
