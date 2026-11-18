using System.Collections.Generic;

namespace TexasHoldem;

public class Deck
{
    private List<Card> cards;

    public Deck()
    {
        cards = new List<Card>();

        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                cards.Add(new Card(rank, suit));
            }
        }
    }

    public void Shuffle()
    {
        Random random = new Random();

        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);

            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    public Card Deal()
    {
        Card top = cards[0];
        cards.RemoveAt(0);
        return top;
    }
}
