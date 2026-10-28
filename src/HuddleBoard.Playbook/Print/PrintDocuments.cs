namespace HuddleBoard.Playbook.Print;

/// <summary>
/// The three printable documents for one format, as HTML. Printing them to PDF needs a
/// browser, so that step lives in the build tool rather than here.
/// </summary>
public static class PrintDocuments
{
    /// <summary>The full playbook: one page per play, plus how the system works.</summary>
    public static string Playbook(Format? f = null) => PlaybookDocument.Build(f ?? Formats.Six);

    /// <summary>7x5 inch cards for the huddle, two per Letter page.</summary>
    public static string Cards(Format? f = null) => FieldCards.Build(f ?? Formats.Six);

    /// <summary>Rotation, game sheet and ball touches.</summary>
    public static string Rotation(Format? f = null) => RotationSheet.Build(f ?? Formats.Six);
}
