namespace HuddleBoard.Playbook.Print;

/// <summary>
/// The three printable documents, as HTML. Printing them to PDF needs a
/// browser, so that step lives in the build tool rather than here.
/// </summary>
public static class PrintDocuments
{
    /// <summary>The full playbook: one page per play, plus how the system works.</summary>
    public static string Playbook() => PlaybookDocument.Build();

    /// <summary>7x5 inch cards for the huddle, two per Letter page.</summary>
    public static string Cards() => FieldCards.Build();

    /// <summary>Rotation, game sheet and ball touches.</summary>
    public static string Rotation() => RotationSheet.Build();
}
