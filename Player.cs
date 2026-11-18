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

    public void Receive(int amount)
    {
        Chips += amount;
    }
}
