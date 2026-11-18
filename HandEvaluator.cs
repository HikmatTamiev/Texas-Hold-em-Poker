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

public class HandEvaluator
{
    public HandCategory Evaluate(List<Card> cards)
    {
        int[] counts = CountRanks(cards);
        bool flush = IsFlush(cards);
        bool straight = IsStraight(counts);

        if (flush && IsRoyal(counts))
        {
            return HandCategory.RoyalFlush;
        }

        if (flush && straight)
        {
            return HandCategory.StraightFlush;
        }

        if (HasCount(counts, 4))
        {
            return HandCategory.FourOfAKind;
        }

        if (HasCount(counts, 3) && HasCount(counts, 2))
        {
            return HandCategory.FullHouse;
        }

        if (flush)
        {
            return HandCategory.Flush;
        }

        if (straight)
        {
            return HandCategory.Straight;
        }

        if (HasCount(counts, 3))
        {
            return HandCategory.ThreeOfAKind;
        }

        if (CountPairs(counts) == 2)
        {
            return HandCategory.TwoPair;
        }

        if (CountPairs(counts) == 1)
        {
            return HandCategory.Pair;
        }

        return HandCategory.HighCard;
    }

    public HandCategory BestOfSeven(List<Card> cards)
    {
        HandCategory best = HandCategory.HighCard;

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

                HandCategory category = Evaluate(five);

                if (category > best)
                {
                    best = category;
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
