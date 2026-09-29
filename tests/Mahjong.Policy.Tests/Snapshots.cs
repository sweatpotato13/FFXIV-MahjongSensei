using Mahjong.Engine;

namespace Mahjong.Policy.Tests;

/// <summary>Helpers for building <see cref="StateSnapshot"/> instances in tests.</summary>
internal static class Snapshots
{
    /// <summary>Build a snapshot for a closed 14-tile hand belonging to the self seat.</summary>
    public static StateSnapshot Closed14(
        string handNotation,
        ActionFlags legalActions = ActionFlags.Discard,
        int ourSeat = 0,
        IReadOnlyList<Tile>? dora = null)
    {
        var tiles = Tiles.Parse(handNotation);
        if (tiles.Length != 14)
            throw new ArgumentException($"closed14 expects 14 tiles, got {tiles.Length}");

        var legal = new LegalActions(legalActions, [], [], [], []);
        var seats = new SeatView[4];
        for (int i = 0; i < 4; i++)
            seats[i] = new SeatView([], [], [], false, -1, false, false);

        return StateSnapshot.Empty with
        {
            Hand = tiles,
            OurSeat = ourSeat,
            RoundWind = 0,
            DoraIndicators = dora ?? [],
            Seats = seats,
            Legal = legal,
        };
    }

    /// <summary>Snapshot for a hand that has called melds; <paramref name="concealedNotation"/> is the concealed part only.</summary>
    public static StateSnapshot WithMelds(
        string concealedNotation,
        IReadOnlyList<Meld> melds,
        ActionFlags legalActions = ActionFlags.Discard,
        int ourSeat = 0)
    {
        var seats = new SeatView[4];
        for (int i = 0; i < 4; i++)
            seats[i] = new SeatView([], [], [], false, -1, false, false);

        return StateSnapshot.Empty with
        {
            Hand = Tiles.Parse(concealedNotation),
            OurMelds = melds,
            OurSeat = ourSeat,
            RoundWind = 0,
            DoraIndicators = [],
            // Keep tiles in the wall so haitei/houtei don't hand the hand a free yaku.
            WallRemaining = 30,
            Seats = seats,
            Legal = new LegalActions(legalActions, [], [], [], []),
        };
    }

    public static StateSnapshot WithLegal(this StateSnapshot s, ActionFlags flags) =>
        s with { Legal = s.Legal with { Flags = flags } };
}
