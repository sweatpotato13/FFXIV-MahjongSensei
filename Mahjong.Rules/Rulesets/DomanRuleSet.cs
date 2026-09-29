using Mahjong.Rules.Scoring;

namespace Mahjong.Rules.Rulesets;

/// <summary>
/// FFXIV Doman: standard riichi scoring with a one-yaku minimum.
///
/// <para><see cref="MinHan"/> is 1, not 2. Both official rule guides state the only
/// requirement as "a winning hand must contain at least one yaku", and both list
/// riichi, ippatsu, yakuhai, tanyao, pinfu, iipeiko, rinshan and chankan at one han
/// apiece -- every one of them a legal win on its own. A two-han minimum is a house
/// rule that neither guide mentions.</para>
///
/// <para>MinHan still guards the yakuless case that issue #51 hit, because a hand the
/// scorer values at zero han is below 1 just as it was below 2.</para>
/// </summary>
public sealed class DomanRuleSet : IRuleSet
{
    private readonly RiichiRuleSet riichi = new();

    public string Name => "Doman";

    public IReadOnlyList<IYakuRule> YakuRules => riichi.YakuRules;
    public IScoringRule ScoringRule => riichi.ScoringRule;
    public IDoraRule DoraRule => riichi.DoraRule;
    public IFuRule FuRule => riichi.FuRule;

    public bool AllowsRedDora => false;
    public bool AllowsKuitan => true;
    public int MinHan => 1;
    public int KazoeThreshold => ScoringConstants.KazoeYakumanHan;
    public int MaxYakuman => 2;
}
