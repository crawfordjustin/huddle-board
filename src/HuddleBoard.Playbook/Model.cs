namespace HuddleBoard.Playbook;

/// <summary>How a drawn segment behaves. The ball moves on handoff; motion
/// happens before the snap and is a head start.</summary>
public enum PathType
{
    Route,
    Run,
    Handoff,
    Motion,
    Fake,
}

/// <summary>How a route is finished on the diagram.</summary>
public enum EndStyle
{
    /// <summary>Keep going — an arrowhead.</summary>
    Arrow,

    /// <summary>Stop and turn around — a bar across the end of the stem.</summary>
    Bar,
}

/// <summary>One drawn segment: who runs it, what kind of segment it is, and the
/// points it passes through in yards.</summary>
/// <remarks>
/// A <see cref="PathType.Handoff"/> names who takes the ball in <paramref name="To"/>.
/// It starts in the giver's hands and ends in the receiver's, so a play can hand
/// the ball on more than once — a reverse is two of these in a row — and the
/// checker can hold that the two kids are actually in the same place at the same
/// moment. On a pass play a handoff is a fake and the ball stays with the thrower.
/// </remarks>
public sealed record PathSeg(
    string Who,
    PathType Type,
    IReadOnlyList<Pt> Pts,
    EndStyle? End = null,
    bool? Delay = null,
    string? To = null);

/// <summary>One line of coaching for one spot, or a slash-separated group of
/// spots ("WIDE LEFT / SLOT RIGHT").</summary>
public sealed record Assignment(string Who, string Text);

/// <summary>A play as geometry: where everybody starts and what they run.</summary>
public sealed record Play(
    int Num,
    string Name,
    string Formation,
    string Category,
    string Tagline,
    string Mistake,
    IReadOnlyList<PathSeg> Paths,
    IReadOnlyList<Assignment> Assign,
    IReadOnlyList<string> Notes);

/// <summary>One row of the call strip: the spot, and the job in shape language.</summary>
public sealed record Call(string Label, string Job);

/// <summary>What the tablet actually says to a seven-year-old, in spot language.</summary>
public sealed record PlayText(
    IReadOnlyList<Call> Calls,
    IReadOnlyList<Assignment> Assign,
    string Mistake,
    IReadOnlyList<string> Notes);

/// <summary>The short diagram tag and the full spoken name for a geometry key.</summary>
public sealed record SpotLabel(string Tag, string Name);

/// <summary>A spot on the field and how to explain where it is.</summary>
public sealed record SpotDef(string Tag, string Name, string Where);

/// <summary>One of the nine shapes. There is no tenth — see CLAUDE.md.</summary>
public sealed record Shape(string Name, string Teach, IReadOnlyList<Pt> Pts, EndStyle End);
