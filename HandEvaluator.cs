using System.Collections.Generic;

namespace TexasHoldem;

public enum HandCategory
{
    HighCard,
    Pair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
    RoyalFlush
}

public class HandStrength
{
    public HandCategory Category { get; }
    public int[] Kickers { get; }

    public HandStrength(HandCategory category, int[] kickers)
    {
        Category = category;
        Kickers = kickers;
    }

    public int Compare(HandStrength other)
    {
        if (Category > other.Category)
        {
            return 1;
        }

        if (Category < other.Category)
        {
            return -1;
        }

        int count = Kickers.Length;

        if (other.Kickers.Length < count)
        {
            count = other.Kickers.Length;
        }

        for (int i = 0; i < count; i++)
        {
            if (Kickers[i] > other.Kickers[i])
            {
                return 1;
            }

            if (Kickers[i] < other.Kickers[i])
            {
                return -1;
            }
        }

        return 0;
    }
}

public class HandEvaluator
{
    public HandStrength Evaluate(List<Card> cards)
    {
        int[] counts = CountRanks(cards);
        bool flush = IsFlush(cards);
        bool straight = IsStraight(counts);

        if (flush && IsRoyal(counts))
        {
            return new HandStrength(HandCategory.RoyalFlush, new int[] { });
        }

        if (flush && straight)
        {
            return new HandStrength(HandCategory.StraightFlush, new int[] { StraightHigh(counts) });
        }

        if (HasCount(counts, 4))
        {
            return new HandStrength(HandCategory.FourOfAKind, Join(RanksWithCount(counts, 4), RanksWithCount(counts, 1)));
        }

        if (HasCount(counts, 3) && HasCount(counts, 2))
        {
            return new HandStrength(HandCategory.FullHouse, Join(RanksWithCount(counts, 3), RanksWithCount(counts, 2)));
        }

        if (flush)
        {
            return new HandStrength(HandCategory.Flush, RanksWithCount(counts, 1));
        }

        if (straight)
        {
            return new HandStrength(HandCategory.Straight, new int[] { StraightHigh(counts) });
        }

        if (HasCount(counts, 3))
        {
            return new HandStrength(HandCategory.ThreeOfAKind, Join(RanksWithCount(counts, 3), RanksWithCount(counts, 1)));
        }

        if (CountPairs(counts) == 2)
        {
            return new HandStrength(HandCategory.TwoPair, Join(RanksWithCount(counts, 2), RanksWithCount(counts, 1)));
        }

        if (CountPairs(counts) == 1)
        {
            return new HandStrength(HandCategory.Pair, Join(RanksWithCount(counts, 2), RanksWithCount(counts, 1)));
        }

        return new HandStrength(HandCategory.HighCard, RanksWithCount(counts, 1));
    }

    public HandStrength BestOfSeven(List<Card> cards)
    {
        HandStrength best = new HandStrength(HandCategory.HighCard, new int[] { 0 });

        for (int skipA = 0; skipA < cards.Count; skipA++)
        {
            for (int skipB = skipA + 1; skipB < cards.Count; skipB++)
            {
                List<Card> five = new List<Card>();

                for (int i = 0; i < cards.Count; i++)
                {
                    if (i != skipA && i != skipB)
                    {
                        five.Add(cards[i]);
                    }
                }

                HandStrength strength = Evaluate(five);

                if (strength.Compare(best) > 0)
                {
                    best = strength;
                }
            }
        }

        return best;
    }

    public string Describe(HandCategory category)
    {
        if (category == HandCategory.RoyalFlush)
        {
            return "Royal flush";
        }

        if (category == HandCategory.StraightFlush)
        {
            return "Straight flush";
        }

        if (category == HandCategory.FourOfAKind)
        {
            return "Four of a kind";
        }

        if (category == HandCategory.FullHouse)
        {
            return "Full house";
        }

        if (category == HandCategory.Flush)
        {
            return "Flush";
        }

        if (category == HandCategory.Straight)
        {
            return "Straight";
        }

        if (category == HandCategory.ThreeOfAKind)
        {
            return "Three of a kind";
        }

        if (category == HandCategory.TwoPair)
        {
            return "Two pair";
        }

        if (category == HandCategory.Pair)
        {
            return "Pair";
        }

        return "High card";
    }

    private int[] RanksWithCount(int[] counts, int wanted)
    {
        List<int> ranks = new List<int>();

        for (int rank = 14; rank >= 2; rank--)
        {
            if (counts[rank] == wanted)
            {
                ranks.Add(rank);
            }
        }

        return ranks.ToArray();
    }

    private int[] Join(int[] first, int[] second)
    {
        int[] combined = new int[first.Length + second.Length];

        for (int i = 0; i < first.Length; i++)
        {
            combined[i] = first[i];
        }

        for (int i = 0; i < second.Length; i++)
        {
            combined[first.Length + i] = second[i];
        }

        return combined;
    }

    private int StraightHigh(int[] counts)
    {
        if (IsWheel(counts))
        {
            return (int)Rank.Five;
        }

        int lowest = 0;

        for (int rank = 2; rank <= 14; rank++)
        {
            if (counts[rank] > 0)
            {
                lowest = rank;
                break;
            }
        }

        return lowest + 4;
    }

    private int[] CountRanks(List<Card> cards)
    {
        int[] counts = new int[15];

        foreach (Card card in cards)
        {
            int rankNumber = (int)card.Rank;
            counts[rankNumber] += 1;
        }

        return counts;
    }

    private bool IsFlush(List<Card> cards)
    {
        Suit first = cards[0].Suit;

        for (int i = 1; i < cards.Count; i++)
        {
            if (cards[i].Suit != first)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsStraight(int[] counts)
    {
        if (IsWheel(counts))
        {
            return true;
        }

        int lowest = 0;

        for (int rank = 2; rank <= 14; rank++)
        {
            if (counts[rank] > 0)
            {
                lowest = rank;
                break;
            }
        }

        for (int step = 0; step < 5; step++)
        {
            int rank = lowest + step;

            if (rank > 14 || counts[rank] != 1)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsWheel(int[] counts)
    {
        return counts[(int)Rank.Ace] == 1
            && counts[(int)Rank.Two] == 1
            && counts[(int)Rank.Three] == 1
            && counts[(int)Rank.Four] == 1
            && counts[(int)Rank.Five] == 1;
    }

    private bool IsRoyal(int[] counts)
    {
        return counts[(int)Rank.Ten] == 1
            && counts[(int)Rank.Jack] == 1
            && counts[(int)Rank.Queen] == 1
            && counts[(int)Rank.King] == 1
            && counts[(int)Rank.Ace] == 1;
    }

    private bool HasCount(int[] counts, int wanted)
    {
        for (int rank = 2; rank <= 14; rank++)
        {
            if (counts[rank] == wanted)
            {
                return true;
            }
        }

        return false;
    }

    private int CountPairs(int[] counts)
    {
        int pairs = 0;

        for (int rank = 2; rank <= 14; rank++)
        {
            if (counts[rank] == 2)
            {
                pairs = pairs + 1;
            }
        }

        return pairs;
    }
}
