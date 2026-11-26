namespace TexasHoldem;

public class Player
{
    public string Name { get; }
    public int Chips { get; private set; }

    public Player(string name, int chips)
    {
        Name = name;
        Chips = chips;
    }

    public void Pay(int amount)
    {
        Chips -= amount;
    }

    public int PayUpTo(int amount)
    {
        int paid = amount;

        if (paid > Chips)
        {
            paid = Chips;
        }

        Chips -= paid;
        return paid;
    }

    public void Receive(int amount)
    {
        Chips += amount;
    }
}
