namespace Mahjong.Engine;

public readonly record struct ShantenResult(
    int Standard,
    int Chiitoitsu,
    int Kokushi)
{
    public int Min => Math.Min(Standard, Math.Min(Chiitoitsu, Kokushi));
    public bool IsTenpai => Min == 0;
    public bool IsAgari => Min <= -1;
}

public static class ShantenCalculator
{
    private const int Agari = -1;

    public static ShantenResult Compute(Hand hand)
    {
        int meldCount = hand.OpenMelds.Count;
        var counts = hand.CloneCounts();

        int std = Standard(counts, meldCount);
        int ci = meldCount == 0 ? Chiitoitsu(counts) : 8;
        int ko = meldCount == 0 ? Kokushi(counts) : 8;

        return new ShantenResult(std, ci, ko);
    }

    /// <summary>6 − pairs + max(0, 7 − distinct). Closed-hand only.</summary>
    public static int Chiitoitsu(ReadOnlySpan<int> counts)
    {
        int pairs = 0, distinct = 0;
        for (int i = 0; i < Tile.Count34; i++)
        {
            if (counts[i] > 0)
                distinct++;
            if (counts[i] >= 2)
                pairs++;
        }
        return 6 - pairs + Math.Max(0, 7 - distinct);
    }

    /// <summary>13 − distinct_terminal_honor − (hasPair ? 1 : 0).</summary>
    public static int Kokushi(ReadOnlySpan<int> counts)
    {
        ReadOnlySpan<int> yaochuu = [0, 8, 9, 17, 18, 26, 27, 28, 29, 30, 31, 32, 33];
        int distinct = 0;
        bool hasPair = false;
        foreach (int idx in yaochuu)
        {
            if (counts[idx] >= 1)
                distinct++;
            if (counts[idx] >= 2)
                hasPair = true;
        }
        return 13 - distinct - (hasPair ? 1 : 0);
    }

    public static int Standard(int[] counts, int meldsAlreadyCalled = 0)
    {
        // The block formula below assumes every unfinished block can still be drawn
        // into. That assumption only breaks when the hand already holds all four
        // copies of a tile: 5555788889999m decomposes as 555+888+999+789 with a lone
        // 5m and scores as a tanki tenpai, but the fifth 5m does not exist. Route
        // those hands through the exact search -- they are rare enough that the cost
        // does not matter, and every other hand keeps the fast path.
        for (int i = 0; i < Tile.Count34; i++)
        {
            if (counts[i] >= 4)
                return Math.Max(Exact(counts, meldsAlreadyCalled), Agari);
        }

        const int BlocksNeeded = 4;
        int calledSets = meldsAlreadyCalled;

        int best = 8;

        for (int i = 0; i < Tile.Count34; i++)
        {
            if (counts[i] < 2)
                continue;
            counts[i] -= 2;
            var (sets, partials) = Decompose(counts);
            counts[i] += 2;

            int totalSets = sets + calledSets;
            int useful = Math.Min(partials, BlocksNeeded - totalSets);
            int s = 8 - 2 * totalSets - useful - 1;
            if (s < best)
                best = s;
        }

        {
            var (sets, partials) = Decompose(counts);
            int totalSets = sets + calledSets;
            int useful = Math.Min(partials, BlocksNeeded - totalSets);
            int s = 8 - 2 * totalSets - useful;
            if (s < best)
                best = s;
        }

        return Math.Max(best, Agari);
    }

    /// <summary>
    /// Shanten straight from the definition: the fewest tiles that still have to be
    /// acquired to reach some complete hand, minus one. Searches over target hands
    /// instead of over decompositions of the current one, so the four-copies-per-tile
    /// ceiling holds by construction and no completion can be assumed that no longer
    /// exists.
    ///
    /// <para>Iterative deepening on the tile budget: almost every hand answers at a
    /// budget of two or three, and a budget that small kills most of the tree at the
    /// first group placement. A plain best-first search has nothing to prune against
    /// until it has built a whole target hand, which is orders of magnitude slower.</para>
    /// </summary>
    private static int Exact(int[] hand, int meldsAlreadyCalled)
    {
        int setsNeeded = 4 - meldsAlreadyCalled;
        var used = new int[Tile.Count34];
        int maxBudget = 2 + (3 * setsNeeded);

        for (int budget = 0; budget <= maxBudget; budget++)
        {
            if (Build(hand, used, pos: 0, sets: 0, setsNeeded: setsNeeded,
                      pairUsed: false, budget: budget))
            {
                return budget - 1;
            }
        }

        return 8;
    }

    /// <summary>True when a complete target hand is reachable within <paramref name="budget"/> extra tiles.</summary>
    private static bool Build(
        int[] hand, int[] used, int pos, int sets, int setsNeeded, bool pairUsed, int budget)
    {
        if (sets == setsNeeded && pairUsed)
            return true;
        if (budget < 0 || pos >= Tile.Count34)
            return false;

        if (sets < setsNeeded && used[pos] + 3 <= 4)
        {
            int d = Extra(hand, used, pos, 3);
            if (d <= budget)
            {
                used[pos] += 3;
                bool ok = Build(hand, used, pos, sets + 1, setsNeeded, pairUsed, budget - d);
                used[pos] -= 3;
                if (ok)
                    return true;
            }
        }

        bool canRun = pos < TileIds.HonorStart && (pos % TileIds.SuitSize) <= 6
                      && used[pos] < 4 && used[pos + 1] < 4 && used[pos + 2] < 4;
        if (sets < setsNeeded && canRun)
        {
            int d = Extra(hand, used, pos, 1)
                  + Extra(hand, used, pos + 1, 1)
                  + Extra(hand, used, pos + 2, 1);
            if (d <= budget)
            {
                used[pos]++;
                used[pos + 1]++;
                used[pos + 2]++;
                bool ok = Build(hand, used, pos, sets + 1, setsNeeded, pairUsed, budget - d);
                used[pos]--;
                used[pos + 1]--;
                used[pos + 2]--;
                if (ok)
                    return true;
            }
        }

        if (!pairUsed && used[pos] + 2 <= 4)
        {
            int d = Extra(hand, used, pos, 2);
            if (d <= budget)
            {
                used[pos] += 2;
                bool ok = Build(hand, used, pos, sets, setsNeeded, true, budget - d);
                used[pos] -= 2;
                if (ok)
                    return true;
            }
        }

        // Leave this tile out of the target hand entirely.
        return Build(hand, used, pos + 1, sets, setsNeeded, pairUsed, budget);
    }

    /// <summary>Tiles that must still be drawn when the target takes <paramref name="k"/> more copies of <paramref name="tile"/>.</summary>
    private static int Extra(int[] hand, int[] used, int tile, int k)
    {
        int before = Math.Max(0, used[tile] - hand[tile]);
        int after = Math.Max(0, used[tile] + k - hand[tile]);
        return after - before;
    }

    private readonly record struct Decomp(int Sets, int Partials)
    {
        public int Score => Sets * 2 + Partials;
        public bool Dominates(Decomp other) =>
            Sets >= other.Sets && Partials >= other.Partials;
    }

    /// <summary>Maximizes (2*sets + partials); ties prefer more sets.</summary>
    private static Decomp Decompose(int[] counts)
    {
        var best = new Decomp(0, 0);
        Scan(counts, 0, 0, 0, ref best);
        return best;
    }

    private static void Scan(int[] counts, int pos, int sets, int partials, ref Decomp best)
    {
        while (pos < Tile.Count34 && counts[pos] == 0)
            pos++;
        if (pos >= Tile.Count34)
        {
            var cand = new Decomp(sets, partials);
            if (cand.Score > best.Score ||
                (cand.Score == best.Score && cand.Sets > best.Sets))
            {
                best = cand;
            }
            return;
        }

        bool isHonor = pos >= 27;
        bool canRun = !isHonor && (pos % 9) <= 6
                      && counts[pos + 1] > 0 && counts[pos + 2] > 0;
        bool canKanchan = !isHonor && (pos % 9) <= 6 && counts[pos + 2] > 0;
        bool canRyanmen = !isHonor && (pos % 9) <= 7 && counts[pos + 1] > 0;

        if (counts[pos] >= 3)
        {
            counts[pos] -= 3;
            Scan(counts, pos, sets + 1, partials, ref best);
            counts[pos] += 3;
        }

        if (canRun)
        {
            counts[pos]--;
            counts[pos + 1]--;
            counts[pos + 2]--;
            Scan(counts, pos, sets + 1, partials, ref best);
            counts[pos]++;
            counts[pos + 1]++;
            counts[pos + 2]++;
        }

        if (counts[pos] >= 2)
        {
            counts[pos] -= 2;
            Scan(counts, pos, sets, partials + 1, ref best);
            counts[pos] += 2;
        }

        if (canRyanmen)
        {
            counts[pos]--;
            counts[pos + 1]--;
            Scan(counts, pos, sets, partials + 1, ref best);
            counts[pos]++;
            counts[pos + 1]++;
        }

        if (canKanchan)
        {
            counts[pos]--;
            counts[pos + 2]--;
            Scan(counts, pos, sets, partials + 1, ref best);
            counts[pos]++;
            counts[pos + 2]++;
        }

        int save = counts[pos];
        counts[pos] = 0;
        Scan(counts, pos + 1, sets, partials, ref best);
        counts[pos] = save;
    }
}
