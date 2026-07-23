using System;
using UnityEngine;

/// <summary>
/// Accent-driven theming for the cue (design handoff V2, finalized tokens).
/// One accent derives every tint; entry->arrival swaps accent AND orb palette.
/// Panels are a neutral charcoal that fits both accents.
/// </summary>
public class CueTheme : MonoBehaviour
{
    public enum Mode { Entry, Arrival }

    [SerializeField] private Mode mode = Mode.Entry;

    // accents (tokens)
    public static readonly Color EntryAccent = Hex("#4ea8ff");
    public static readonly Color ArrivalAccent = Hex("#6fe0a8");
    public static readonly Color EntryAc2 = Hex("#7cc4ff");
    public static readonly Color ArrivalAc2 = Hex("#a6f0d0");

    // orb gradient palettes (tokens)
    public static readonly Color EntryHi = Hex("#eaf3ff");
    public static readonly Color EntryMid = Hex("#4ea8ff");
    public static readonly Color EntryLo = Hex("#1b3a8f");
    public static readonly Color ArrivalHi = Hex("#e8fff5");
    public static readonly Color ArrivalMid = Hex("#6fe0a8");
    public static readonly Color ArrivalLo = Hex("#146b48");

    // text
    public static readonly Color TextPrimary = Hex("#eef2ff");
    public static readonly Color TextBody = new(226/255f, 232/255f, 255/255f, 0.90f);
    public static readonly Color TextSecondary = new(226/255f, 232/255f, 255/255f, 0.60f);
    public static readonly Color TextButtonSecondary = new(226/255f, 232/255f, 255/255f, 0.72f);
    public static readonly Color ButtonPrimaryText = Hex("#08101e");

    // panels — neutral charcoal (tokens: top #252727, bottom #171b1c, fill 0.80, border #c6cbdb @0.20)
    public static readonly Color PanelTop = RgbA("#252727", 0.80f);
    public static readonly Color PanelBottom = RgbA("#171b1c", 0.82f);
    public static readonly Color PanelBorder = RgbA("#c6cbdb", 0.20f);
    public const float PanelRadius = 25f; // token

    public event Action<CueTheme> Changed;

    public Mode CurrentMode
    {
        get => mode;
        set { mode = value; Changed?.Invoke(this); }
    }

    public Color Accent => mode == Mode.Entry ? EntryAccent : ArrivalAccent;
    public Color Ac2 => mode == Mode.Entry ? EntryAc2 : ArrivalAc2;
    public Color OrbHi => mode == Mode.Entry ? EntryHi : ArrivalHi;
    public Color OrbMid => mode == Mode.Entry ? EntryMid : ArrivalMid;
    public Color OrbLo => mode == Mode.Entry ? EntryLo : ArrivalLo;

    public Color AcSoft => WithAlpha(Accent, 0.18f);
    public Color AcMid => WithAlpha(Accent, 0.34f);
    public Color AcStrong => WithAlpha(Accent, 0.50f);
    public Color AcLine => WithAlpha(Accent, 0.62f);

    private static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

    private static Color RgbA(string hex, float a)
    {
        var c = Hex(hex);
        return new Color(c.r, c.g, c.b, a);
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
