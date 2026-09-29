namespace Mahjong.Plugin.Game.Tests;

public class LabelMatcherTests
{
    private static readonly LayoutLabels Kr = new(
        Pon: new[] { "Pon", "펑" },
        Chi: new[] { "Chi", "치" },
        Kan: new[] { "Kan", "깡" },
        Riichi: new[] { "Riichi", "리치" },
        Tsumo: new[] { "Tsumo", "쯔모" },
        Ron: new[] { "Ron", "론" },
        Pass: new[] { "Pass", "패스" });

    [Theory]
    [InlineData("펑", CallLabel.Pon)]
    [InlineData("치", CallLabel.Chi)]
    [InlineData("깡", CallLabel.Kan)]
    [InlineData("리치", CallLabel.Riichi)]
    [InlineData("쯔모", CallLabel.Tsumo)]
    [InlineData("론", CallLabel.Ron)]
    [InlineData("패스", CallLabel.Pass)]
    public void Korean_labels_resolve(string raw, CallLabel expected)
        => Assert.Equal(expected, LabelMatcher.Match(raw, Kr));

    [Theory]
    [InlineData("Pon", CallLabel.Pon)]
    [InlineData("Riichi", CallLabel.Riichi)]
    [InlineData("Pass", CallLabel.Pass)]
    public void English_labels_still_resolve_from_the_same_profile(string raw, CallLabel expected)
        => Assert.Equal(expected, LabelMatcher.Match(raw, Kr));

    /// <summary>"리치" contains "치"; a naive containment scan would report Chi and the bot would never declare riichi.</summary>
    [Fact]
    public void Riichi_is_not_swallowed_by_the_chi_alias()
        => Assert.Equal(CallLabel.Riichi, LabelMatcher.Match("리치", Kr));

    [Fact]
    public void Riichi_wins_containment_even_with_an_icon_payload_attached()
        => Assert.Equal(CallLabel.Riichi, LabelMatcher.Match("\u0002\u001a\u0002\u0002\u0003 리치", Kr));

    /// <summary>The KR prompts render an icon glyph before the word; the payload survives a raw UTF-8 decode as control chars / U+FFFD.</summary>
    [Theory]
    [InlineData("\u0002\u0012\u0002\u0059\u0003 펑", CallLabel.Pon)]
    [InlineData("\uFFFD\uFFFD 패스", CallLabel.Pass)]
    [InlineData("  치  ", CallLabel.Chi)]
    [InlineData("\uE040펑", CallLabel.Pon)]
    public void Icon_payload_residue_is_stripped_before_matching(string raw, CallLabel expected)
        => Assert.Equal(expected, LabelMatcher.Match(raw, Kr));

    /// <summary>Kan subtypes (안깡 / 가깡, Ankan / Shouminkan) fall out of containment for free.</summary>
    [Theory]
    [InlineData("안깡")]
    [InlineData("가깡")]
    public void Kan_subtypes_match_the_base_alias(string raw)
        => Assert.Equal(CallLabel.Kan, LabelMatcher.Match(raw, Kr));

    [Fact]
    public void Null_profile_falls_back_to_english_defaults()
    {
        Assert.Equal(CallLabel.Pon, LabelMatcher.Match("Pon", null));
        Assert.Equal(CallLabel.None, LabelMatcher.Match("펑", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\u0002\u0003")]
    [InlineData("Discard")]
    public void Unrelated_or_empty_input_matches_nothing(string raw)
        => Assert.Equal(CallLabel.None, LabelMatcher.Match(raw, Kr));

    [Fact]
    public void Null_input_matches_nothing()
        => Assert.Equal(CallLabel.None, LabelMatcher.Match(null, Kr));

    [Fact]
    public void Shipped_profiles_carry_both_locales()
    {
        var profile = JsonLayoutProfileLoader.Parse("""
        {
          "name": "Emj", "addonName": "Emj", "tileTextureBase": 76041,
          "labels": { "pon": ["Pon", "펑"], "riichi": ["Riichi", "리치"] },
          "offsets": { "selfScore": "0x0500", "shimochaScore": "0x07E0", "toimenScore": "0x0AC0",
            "kamichaScore": "0x0DA0", "selfDiscardCountByte": "0x04FE", "shimochaDiscardCountByte": "0x07DE",
            "toimenDiscardCountByte": "0x0ABE", "kamichaDiscardCountByte": "0x0D9E",
            "handArrayStart": "0x0DB8", "doraIndicator": "0x0FD8" },
          "nodeIds": { "callModalHost": 104, "callModalShell": 3, "meldContainer": 61 },
          "atkValues": { "stateCode": 0, "wallCount": 1, "chiClaimedTile": 19 },
          "stateCodes": { "ourTurnDiscard": 30, "callPrompt": 15, "callPromptList": 28,
            "selfDeclareList": 6, "postDrawIdle": 5 },
          "limits": { "handSize": 14, "wallInitial": 70, "scoreSanityMax": 200000,
            "discardCountSanityMax": 40, "maxAkadoraSlots": 3 }
        }
        """);

        Assert.NotNull(profile.Labels);
        Assert.Equal(CallLabel.Pon, LabelMatcher.Match("펑", profile.Labels));
        Assert.Equal(CallLabel.Riichi, LabelMatcher.Match("리치", profile.Labels));
    }

    /// <summary>A profile written before localisation must keep behaving exactly as it did.</summary>
    [Fact]
    public void Profile_without_a_labels_block_keeps_english_behaviour()
    {
        var profile = JsonLayoutProfileLoader.Parse("""
        {
          "name": "Emj", "addonName": "Emj", "tileTextureBase": 76041,
          "offsets": { "selfScore": "0x0500", "shimochaScore": "0x07E0", "toimenScore": "0x0AC0",
            "kamichaScore": "0x0DA0", "selfDiscardCountByte": "0x04FE", "shimochaDiscardCountByte": "0x07DE",
            "toimenDiscardCountByte": "0x0ABE", "kamichaDiscardCountByte": "0x0D9E",
            "handArrayStart": "0x0DB8", "doraIndicator": "0x0FD8" },
          "nodeIds": { "callModalHost": 104, "callModalShell": 3, "meldContainer": 61 },
          "atkValues": { "stateCode": 0, "wallCount": 1, "chiClaimedTile": 19 },
          "stateCodes": { "ourTurnDiscard": 30, "callPrompt": 15, "callPromptList": 28,
            "selfDeclareList": 6, "postDrawIdle": 5 },
          "limits": { "handSize": 14, "wallInitial": 70, "scoreSanityMax": 200000,
            "discardCountSanityMax": 40, "maxAkadoraSlots": 3 }
        }
        """);

        Assert.Null(profile.Labels);
        Assert.Equal(CallLabel.Pon, LabelMatcher.Match("Pon", profile.Labels));
    }
}
