using System.Text;

namespace Mahjong.Plugin.Game.Variants;

/// <summary>Which call a prompt button/list-item label denotes, independent of client language.</summary>
public enum CallLabel
{
    None = 0,
    Pon,
    Chi,
    Kan,
    Riichi,
    Tsumo,
    Ron,
    Pass,
}

/// <summary>
/// Maps a call-prompt label to a <see cref="CallLabel"/> without hardcoding any one client
/// language. Aliases come from the active <see cref="LayoutProfile"/> so a new locale is a
/// JSON edit, never a code change.
///
/// <para>Two normalisation problems make naive equality fail on non-English clients:
/// the game embeds SeString icon payloads in these labels (the KR riichi prompt renders as
/// an icon glyph followed by "리치"), and the payload bytes survive a raw UTF-8 decode as
/// control characters or U+FFFD. <see cref="Normalize"/> strips those before comparing.</para>
/// </summary>
public static class LabelMatcher
{
    /// <summary>
    /// Used when a profile omits <c>labels</c>. Keeps pre-localisation behaviour byte-identical
    /// for any layout file that hasn't been migrated.
    /// </summary>
    public static LayoutLabels Default { get; } = new(
        Pon: new[] { "Pon" },
        Chi: new[] { "Chi" },
        Kan: new[] { "Kan" },
        Riichi: new[] { "Riichi" },
        Tsumo: new[] { "Tsumo" },
        Ron: new[] { "Ron" },
        Pass: new[] { "Pass" });

    /// <summary>Strip SeString payload residue and collapse whitespace so "\u0002\u001a\u0002\u0002\u0003 리치" compares as "리치".</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            // Control chars: SeString payload framing (START_BYTE 0x02 ... END_BYTE 0x03).
            if (char.IsControl(ch))
                continue;
            // U+FFFD: payload bytes >= 0x80 that aren't valid UTF-8.
            if (ch == '\uFFFD')
                continue;
            // Private use area: gfdata icon glyphs.
            if (ch is >= '\uE000' and <= '\uF8FF')
                continue;
            if (char.IsWhiteSpace(ch))
            {
                if (sb.Length > 0 && sb[^1] != ' ')
                    sb.Append(' ');
                continue;
            }
            sb.Append(ch);
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Resolve a label. Exact matches are tried across every alias before any containment
    /// match, and containment prefers the longest alias — without that ordering the Korean
    /// riichi label "리치" would be swallowed by the Korean chi label "치", which it contains.
    /// </summary>
    public static CallLabel Match(string? raw, LayoutLabels? labels)
    {
        var s = Normalize(raw);
        if (s.Length == 0)
            return CallLabel.None;

        var set = labels ?? Default;

        foreach (var (kind, alias) in Enumerate(set))
        {
            if (string.Equals(s, alias, StringComparison.OrdinalIgnoreCase))
                return kind;
        }

        var best = CallLabel.None;
        int bestLen = 0;
        foreach (var (kind, alias) in Enumerate(set))
        {
            if (alias.Length <= bestLen)
                continue;
            if (s.Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                best = kind;
                bestLen = alias.Length;
            }
        }

        return best;
    }

    private static IEnumerable<(CallLabel Kind, string Alias)> Enumerate(LayoutLabels set)
    {
        foreach (var pair in new (CallLabel Kind, IReadOnlyList<string>? Aliases)[]
        {
            (CallLabel.Pon, set.Pon),
            (CallLabel.Chi, set.Chi),
            (CallLabel.Kan, set.Kan),
            (CallLabel.Riichi, set.Riichi),
            (CallLabel.Tsumo, set.Tsumo),
            (CallLabel.Ron, set.Ron),
            (CallLabel.Pass, set.Pass),
        })
        {
            if (pair.Aliases is null)
                continue;
            foreach (var alias in pair.Aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                    yield return (pair.Kind, alias);
            }
        }
    }
}
