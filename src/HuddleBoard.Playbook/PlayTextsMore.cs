namespace HuddleBoard.Playbook;

/// <summary>
/// Spot-language text for plays 15-24. Same rule as the first fourteen: every
/// job in a call strip is one of the nine shapes, and anybody the coach does not
/// name runs GO. Sides are BLUE and ORANGE in the spoken text; the labels stay
/// LEFT/RIGHT because that is what the formation map is keyed on.
/// </summary>
/// <remarks>Text for a new play goes here. See CLAUDE.md, "Adding a play".</remarks>
public static partial class PlayTexts
{
    private static readonly IReadOnlyDictionary<int, PlayText> Recent = new Dictionary<int, PlayText>
    {
        [15] = new(
            Calls:
            [
                new("BOTH WIDES", "SIT at 5"),
                new("SLOT LEFT / SLOT RIGHT", "SIT at 5"),
                new("SNAPPER", "SIT at 4"),
            ],
            Assign:
            [
                new("WIDE LEFT / SLOT LEFT / SLOT RIGHT / WIDE RIGHT",
                    "SIT. Sprint 5 yards, stop, turn around and face the thrower with your hands up. Do not keep drifting — stop means stop."),
                new("SNAPPER",
                    "SIT. Snap it, run 4 yards, turn around. You are the safety valve if everybody else is covered."),
            ],
            Mistake: "Drifting upfield after the turn. If he keeps floating, the throw goes behind him.",
            Notes:
            [
                "Call this when you are out of ideas or the kids are rattled. Nobody can run it wrong.",
                "Everybody is 5 yards apart before the snap and still 5 yards apart after it, so two kids cannot collide even if they both go the wrong way.",
                "Best first install in the no-run zone: it teaches SIT to all five at once.",
            ]),
        [16] = new(
            Calls:
            [
                new("SLOT RIGHT", "IN at 6"),
                new("WIDE RIGHT", "OUT at 3"),
                new("BACK", "SWING to blue"),
                new("SNAPPER", "SIT at 4"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT RIGHT",
                    "IN. Run 6 yards, plant your outside foot, and cut flat across the middle. You are the first look — expect it."),
                new("WIDE RIGHT",
                    "OUT. Run only 3 yards, then break straight for the orange sideline. Stay shallow. You are pulling the defender down and away from the slot."),
                new("BACK",
                    "SWING. Loop out of the backfield toward the blue sideline and look back. You are the late outlet."),
                new("SNAPPER",
                    "SIT. Snap, 4 yards, turn around."),
                new("WIDE BLUE",
                    "GO. Sprint straight downfield and take the backside defender with you."),
            ],
            Mistake: "The slot rounding his break. Plant and cut flat, or the defender stays on top of him.",
            Notes:
            [
                "Two routes on the same side, 3 yards apart in depth. They never cross, so they never rub — that is what keeps it legal.",
                "One defender has to choose. Whichever way he goes, throw the other one.",
                "When the defense starts jumping the shallow route, that is the week to call Fireworks.",
            ]),
        [17] = new(
            Calls:
            [
                new("TIGHT LEFT", "SWING wide — look back NOW"),
                new("SNAPPER", "SIT at 4"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("TIGHT LEFT",
                    "SWING. Open toward the blue sideline and get wide fast, staying level with the line. Look back for the ball immediately — it is coming before you are set."),
                new("SNAPPER",
                    "SIT. Snap, 4 yards, turn around."),
                new("WIDE LEFT / SLOT LEFT / WIDE RIGHT",
                    "GO. Sprint straight downfield and take your defender away from the catch."),
            ],
            Mistake: "Throwing it backwards. The catch has to be in front of the thrower, or a drop is a live ball.",
            Notes:
            [
                "The whole play is over in two seconds. That is exactly why it beats a rusher who has to start 7 yards away.",
                "Coach the catch point, not the route: he must be even with or in front of the thrower when the ball arrives.",
                "If the defense starts flying to the sideline, the three GO routes behind it are wide open. That is the same picture as Elevator.",
            ]),
        [18] = new(
            Calls:
            [
                new("BOTH WIDES", "GO up the sideline"),
                new("SLOT LEFT / SLOT RIGHT", "GO up your lane"),
                new("SNAPPER", "GO 8 yards"),
            ],
            Assign:
            [
                new("WIDE LEFT / WIDE RIGHT",
                    "GO. Sprint straight up your sideline. Do NOT drift inside — the whole play is five straight lines."),
                new("SLOT LEFT / SLOT RIGHT",
                    "GO. Sprint straight up your own lane, splitting the middle of the field."),
                new("SNAPPER",
                    "GO. Snap it and sprint 8 yards straight up the middle."),
            ],
            Mistake: "Drifting together downfield. They start 5 yards apart and they have to finish 5 yards apart.",
            Notes:
            [
                "Every kid already knows this one, because it IS the default rule: if the coach did not give you a job, run GO. Call it when the huddle is falling apart.",
                "Five lanes, five yards apart, nobody crosses anybody. The safest deep call in the book.",
                "Best right after a run has worked — the defense creeps up and there is nobody home.",
            ]),
        [19] = new(
            Calls:
            [
                new("TIGHT LEFT", "OUT at 3 — the LOW"),
                new("WIDE LEFT", "OUT at 8 — the HIGH"),
                new("SLOT LEFT", "GO — outside step first"),
                new("SNAPPER", "SIT at 5"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("TIGHT LEFT",
                    "OUT. Run 3 yards and break for the blue sideline. You are the LOW one, and the first look."),
                new("WIDE LEFT",
                    "OUT. Run 8 yards and break for the blue sideline. You are the HIGH one."),
                new("SLOT LEFT",
                    "GO. Take one step toward the sideline, then sprint straight up. You are clearing the window the low route breaks into."),
                new("SNAPPER",
                    "SIT. Snap, 5 yards, turn around."),
                new("WIDE RIGHT",
                    "GO. Sprint downfield on the far side and take your man out of the picture."),
            ],
            Mistake: "Both receivers breaking at the same depth. Stacked up, it is dead — and it looks like a screen.",
            Notes:
            [
                "Five yards between the two break points is what makes this safe with 8-year-olds. Do not let them creep together.",
                "Teach the read out loud on the sideline: 'if he comes up, throw over him.' One defender cannot have both.",
            ]),
        [20] = new(
            Calls:
            [
                new("SLOT RIGHT", "POST at 8 — the deep one"),
                new("WIDE RIGHT", "POST at 4 — stay UNDER him"),
                new("WIDE LEFT", "OUT at 5"),
                new("BACK", "SWING to blue"),
                new("SNAPPER", "SIT at 3"),
            ],
            Assign:
            [
                new("SLOT RIGHT",
                    "POST. Run 8 yards, then break at an angle for the deep middle. You are the deep one — run past everybody."),
                new("WIDE RIGHT",
                    "POST. Run only 4 yards, then break inside. Stay UNDERNEATH him. If you end up side by side you have run it wrong."),
                new("WIDE LEFT",
                    "OUT. Run 5 yards and break for the blue sideline. You are the answer if they take both posts away."),
                new("BACK",
                    "SWING. Loop out of the backfield toward the blue sideline and look back."),
                new("SNAPPER",
                    "SIT. Snap, 3 yards, turn around."),
            ],
            Mistake: "Both posts breaking at the same yard line. The stagger is the entire play.",
            Notes:
            [
                "One deep defender cannot take both. Throw over him or in front of him — the thrower's only job is to pick which.",
                "The two break 4 yards apart in depth and finish 5 yards apart. Drill the stagger at walking pace before you ever run it live.",
                "Call it after Slingshot has pulled the defense up.",
            ]),
        [21] = new(
            Calls:
            [
                new("BACK", "wait one count, then CARRY"),
                new("WIDE LEFT / SLOT RIGHT", "OUT at 6 — sell it"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK",
                    "CARRY. Take a small step back like it is a pass. Count one. THEN take the ball and run straight up past the snapper's right hip."),
                new("THROWER",
                    "Catch the snap and take one step back like you are going to throw. Let the rusher come at you. Then hand it off with both hands."),
                new("WIDE LEFT / SLOT RIGHT",
                    "OUT. Run 6 yards and break for the sideline. Sell it — if you jog, the defenders never leave and there is no hole."),
                new("WIDE RIGHT",
                    "GO. Sprint straight downfield."),
                new("SNAPPER",
                    "GO. Snap, then release to the blue side and stay out of the running lane."),
            ],
            Mistake: "Handing it off too early. Let the rusher commit upfield first — count one, then give it.",
            Notes:
            [
                "The rusher has to start 7 yards back, so he arrives at full speed. This play uses that against him: he runs himself right out of it.",
                "Only works if the receivers sell the routes. That is the coaching point, not the handoff.",
                "Call it the down after a pass, never the down after a run.",
            ]),
        [22] = new(
            Calls:
            [
                new("SLOT RIGHT", "MOTION → CARRY to blue, then hand it off"),
                new("SLOT LEFT", "take it back and CARRY to orange"),
                new("WIDE RIGHT", "IN at 5 — clear the sideline"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT RIGHT",
                    "MOTION, then CARRY. Start moving before the snap. Take the ball going toward blue, run two steps, then hand it to the Slot Blue coming the other way. Keep running after you give it up — sell it."),
                new("SLOT LEFT",
                    "CARRY. Let him get past you first, THEN take the ball with both hands. Run all the way to the orange sideline before you turn up."),
                new("WIDE RIGHT",
                    "IN. Break inside at 5 yards. You are clearing the orange sideline — without you, you are standing exactly where the ball is going."),
                new("WIDE LEFT",
                    "GO. Sprint downfield and take the corner with you."),
                new("SNAPPER",
                    "GO. Snap and release up the middle, away from both handoffs."),
            ],
            Mistake: "The second runner leaving before he has the ball. Take it first, then go.",
            Notes:
            [
                "Only call this after Rocket has worked. A reverse is the punishment for chasing — with nothing to chase it is just a slow run.",
                "Two handoffs means two chances to put it on the ground. Walk through it ten times before you run it.",
                "This is the one play in the book where a kid runs backwards across the formation. Practise the spacing so the two runners pass each other cleanly.",
            ]),
        [23] = new(
            Calls:
            [
                new("BACK", "SWING to orange — look back"),
                new("WIDE RIGHT", "CORNER at 4"),
                new("TIGHT RIGHT", "GO into the end zone"),
                new("SNAPPER", "SIT at 3"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK",
                    "SWING. Loop out of the backfield toward the orange sideline. Stay behind everybody, look back, CATCH IT, and only then turn upfield."),
                new("WIDE RIGHT",
                    "CORNER. Run 4 yards, then break at an angle for the corner of the end zone. You are dragging the corner defender out of the way."),
                new("TIGHT RIGHT",
                    "GO. Sprint straight into the end zone and take the middle defender with you."),
                new("SNAPPER",
                    "SIT. Snap and drift to the blue side. Stay out of the throwing lane."),
                new("WIDE LEFT",
                    "GO. Sprint into the end zone on the far side."),
            ],
            Mistake: "The Back turning upfield before he catches it. Catch first, then turn.",
            Notes:
            [
                "Inside the 5, every defender's eyes go to the end zone. The flat is the emptiest grass on the field.",
                "Runs out of the same formation as Rainbow, so the defense sees the same picture and gets the opposite answer.",
                "If they finally cover the flat, the corner route behind it is a touchdown.",
            ]),
        [24] = new(
            Calls:
            [
                new("TIGHT LEFT", "SIT at 5"),
                new("WIDE LEFT", "CORNER at 5"),
                new("SLOT LEFT", "SWING to blue"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("TIGHT LEFT",
                    "SIT. Run 5 yards, drift a step toward the sideline, and sit down facing the thrower. First look — expect the ball."),
                new("WIDE LEFT",
                    "CORNER. Run 5 yards, then break at an angle for the deep corner."),
                new("SLOT LEFT",
                    "SWING. Loop behind the line toward the blue sideline and look back. You are the checkdown if the first two are covered."),
                new("WIDE RIGHT",
                    "GO. Sprint downfield on the far side."),
                new("SNAPPER",
                    "GO. Snap and release straight up the middle."),
            ],
            Mistake: "The sit drifting toward the sideline. Settle in the window and stay there.",
            Notes:
            [
                "Three levels on one side of the field: deep, medium, and behind the line. Whichever the defense takes away, the other two are open.",
                "The most grown-up concept in the book, and it still only uses CORNER, SIT and SWING. No new vocabulary for the kids to learn.",
                "Read it low to high: sit first, corner if he is covered, swing if they both are.",
            ]),
    };
}
