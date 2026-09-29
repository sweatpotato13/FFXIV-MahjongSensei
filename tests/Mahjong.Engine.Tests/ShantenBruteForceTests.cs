namespace Mahjong.Engine.Tests;

/// <summary>
/// Exhaustive cross-check of <see cref="ShantenCalculator.Standard"/> against the textbook
/// definition of shanten: the minimum number of tiles you must still acquire to reach some
/// complete 14-tile hand, minus one.
///
/// <para>The production algorithm takes a shortcut -- it picks the single decomposition that
/// maximises <c>2*sets + partials</c> and applies the block formula to that one. That is a
/// heuristic: a decomposition with a lower score can yield a lower shanten once the
/// "at most 4 blocks besides the pair" cap bites. This test enumerates every complete hand
/// instead, so it cannot inherit that assumption.</para>
///
/// <para>Scoped to a single suit, where sequence/triplet ambiguity is at its worst, which
/// makes full enumeration of both the hand space and the target space tractable.</para>
/// </summary>
public class ShantenBruteForceTests
{
    private const int Types = 9;

    /// <summary>Every legal 14-tile winning hand (4 sets + a pair) inside one suit.</summary>
    private static List<int[]> CompleteHands()
    {
        var sets = new List<int[]>();
        for (int i = 0; i < Types; i++)
        {
            var t = new int[Types];
            t[i] = 3;
            sets.Add(t);
        }
        for (int i = 0; i + 2 < Types; i++)
        {
            var r = new int[Types];
            r[i] = r[i + 1] = r[i + 2] = 1;
            sets.Add(r);
        }

        var result = new List<int[]>();
        var acc = new int[Types];

        void Recurse(int start, int remaining)
        {
            if (remaining == 0)
            {
                for (int p = 0; p < Types; p++)
                {
                    if (acc[p] + 2 > 4)
                        continue;
                    var hand = (int[])acc.Clone();
                    hand[p] += 2;
                    result.Add(hand);
                }
                return;
            }

            for (int s = start; s < sets.Count; s++)
            {
                var set = sets[s];
                bool ok = true;
                for (int i = 0; i < Types; i++)
                {
                    if (acc[i] + set[i] > 4) { ok = false; break; }
                }
                if (!ok)
                    continue;
                for (int i = 0; i < Types; i++) acc[i] += set[i];
                Recurse(s, remaining - 1);
                for (int i = 0; i < Types; i++) acc[i] -= set[i];
            }
        }

        Recurse(0, 4);
        return result;
    }

    /// <summary>Textbook shanten: min over complete hands of (tiles still needed) - 1.</summary>
    private static int Reference(int[] hand, List<int[]> complete)
    {
        int best = int.MaxValue;
        foreach (var w in complete)
        {
            int needed = 0;
            for (int i = 0; i < Types; i++)
            {
                int d = w[i] - hand[i];
                if (d > 0)
                    needed += d;
            }
            if (needed < best)
                best = needed;
        }
        return best - 1;
    }

    private static IEnumerable<int[]> AllHands(int tileCount)
    {
        var counts = new int[Types];

        IEnumerable<int[]> Recurse(int pos, int remaining)
        {
            if (pos == Types)
            {
                if (remaining == 0)
                    yield return (int[])counts.Clone();
                yield break;
            }
            for (int c = 0; c <= Math.Min(4, remaining); c++)
            {
                counts[pos] = c;
                foreach (var h in Recurse(pos + 1, remaining - c))
                    yield return h;
            }
            counts[pos] = 0;
        }

        return Recurse(0, tileCount);
    }

    [Fact]
    public void Standard_matches_brute_force_for_every_single_suit_13_tile_hand()
    {
        var complete = CompleteHands();
        Assert.NotEmpty(complete);

        int checkedHands = 0;
        int total = 0, under = 0, withQuad = 0;
        var failures = new List<string>();

        foreach (var hand in AllHands(13))
        {
            int expected = Reference(hand, complete);
            var counts34 = new int[Tile.Count34];
            Array.Copy(hand, counts34, Types);

            int actual = ShantenCalculator.Standard(counts34, 0);
            checkedHands++;

            if (actual != expected)
            {
                total++;
                if (actual < expected) under++;
                bool hasQuad = false;
                for (int i = 0; i < Types; i++) if (hand[i] == 4) hasQuad = true;
                if (hasQuad) withQuad++;
                if (failures.Count < 6)
                    failures.Add($"{Describe(hand)}: expected {expected}, got {actual}");
            }
        }

        Assert.True(
            total == 0,
            $"MISMATCHES {total}/{checkedHands} ({100.0 * total / checkedHands:F3}%) | "
            + $"understated={under} | involving-a-4-of-a-kind={withQuad} | "
            + string.Join(" ; ", failures));
    }

    private static string Describe(int[] hand)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < Types; i++)
            sb.Append(new string((char)('1' + i), hand[i]));
        sb.Append('m');
        return sb.ToString();
    }
}
