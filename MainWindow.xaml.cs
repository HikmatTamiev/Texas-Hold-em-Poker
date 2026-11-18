using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TexasHoldem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Deck deck;
    private Player you;
    private Player opponent;
    private List<Card> holeCards;
    private List<Card> opponentHoleCards;
    private List<Card> boardCards;
    private int pot;
    private int street;
    private bool hasActed;

    public MainWindow()
    {
        InitializeComponent();

        you = new Player("You", 1000);
        opponent = new Player("Opponent", 1000);

        int smallBlind = 10;
        int bigBlind = 20;
        you.Pay(smallBlind);
        opponent.Pay(bigBlind);
        pot = smallBlind + bigBlind;
        ShowChips();

        deck = new Deck();
        deck.Shuffle();
        boardCards = new List<Card>();

        holeCards = TakeCards(2);
        ShowCards(holeCards, HolePanel);
        opponentHoleCards = TakeCards(2);

        street = 0;
        HandNameText.Text = "Preflop. Check or bet, then deal the flop.";
    }

    private void Check_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3 || hasActed)
        {
            return;
        }

        hasActed = true;
        HandNameText.Text = "You checked. Deal the next cards.";
    }

    private void Bet_Click(object sender, RoutedEventArgs e)
    {
        int bet = 20;

        if (street == 3 || hasActed)
        {
            return;
        }

        if (you.Chips < bet || opponent.Chips < bet)
        {
            HandNameText.Text = "Not enough chips.";
            return;
        }

        you.Pay(bet);
        opponent.Pay(bet);
        pot = pot + bet + bet;
        hasActed = true;
        ShowChips();
        HandNameText.Text = "You bet 20. Opponent calls. Deal the next cards.";
    }

    private void DealNext_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3)
        {
            return;
        }

        hasActed = false;

        if (street == 0)
        {
            List<Card> flop = TakeCards(3);
            boardCards.AddRange(flop);
            ShowCards(flop, BoardPanel);
            street = 1;
            HandNameText.Text = "Flop. Check or bet.";
            return;
        }

        if (street == 1)
        {
            List<Card> turn = TakeCards(1);
            boardCards.AddRange(turn);
            ShowCards(turn, BoardPanel);
            street = 2;
            HandNameText.Text = "Turn. Check or bet.";
            return;
        }

        List<Card> river = TakeCards(1);
        boardCards.AddRange(river);
        ShowCards(river, BoardPanel);
        street = 3;
        Showdown();
    }

    private void Showdown()
    {
        ShowCards(opponentHoleCards, OpponentPanel);

        List<Card> yourSeven = new List<Card>();
        yourSeven.AddRange(holeCards);
        yourSeven.AddRange(boardCards);

        List<Card> opponentSeven = new List<Card>();
        opponentSeven.AddRange(opponentHoleCards);
        opponentSeven.AddRange(boardCards);

        HandEvaluator evaluator = new HandEvaluator();
        HandCategory yourHand = evaluator.BestOfSeven(yourSeven);
        HandCategory opponentHand = evaluator.BestOfSeven(opponentSeven);

        if (yourHand > opponentHand)
        {
            HandNameText.Text = "You win with " + evaluator.Describe(yourHand) + ".";
        }
        else if (opponentHand > yourHand)
        {
            HandNameText.Text = "Opponent wins with " + evaluator.Describe(opponentHand) + ".";
        }
        else
        {
            HandNameText.Text = "Both have " + evaluator.Describe(yourHand) + ". The winner is not decided yet.";
        }
    }

    private List<Card> TakeCards(int count)
    {
        List<Card> dealt = new List<Card>();

        for (int i = 0; i < count; i++)
        {
            dealt.Add(deck.Deal());
        }

        return dealt;
    }

    private void ShowCards(List<Card> cards, StackPanel panel)
    {
        foreach (Card card in cards)
        {
            panel.Children.Add(CreateCardFace(card));
        }
    }

    private void ShowChips()
    {
        ChipText.Text = you.Name + ": " + you.Chips + " chips"
            + "\n" + opponent.Name + ": " + opponent.Chips + " chips"
            + "\nPot: " + pot;
    }

    private Border CreateCardFace(Card card)
    {
        TextBlock label = new TextBlock();
        label.Text = RankLabel(card.Rank) + "\n" + SuitSymbol(card.Suit);
        label.FontSize = 28;
        label.Foreground = SuitBrush(card.Suit);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;

        Border face = new Border();
        face.Width = 90;
        face.Height = 130;
        face.Margin = new Thickness(8);
        face.Background = Brushes.White;
        face.BorderBrush = Brushes.Black;
        face.BorderThickness = new Thickness(2);
        face.CornerRadius = new CornerRadius(8);
        face.Child = label;

        return face;
    }

    private string RankLabel(Rank rank)
    {
        if (rank == Rank.Ace)
        {
            return "A";
        }

        if (rank == Rank.King)
        {
            return "K";
        }

        if (rank == Rank.Queen)
        {
            return "Q";
        }

        if (rank == Rank.Jack)
        {
            return "J";
        }

        return ((int)rank).ToString();
    }

    private string SuitSymbol(Suit suit)
    {
        if (suit == Suit.Clubs)
        {
            return "♣";
        }

        if (suit == Suit.Diamonds)
        {
            return "♦";
        }

        if (suit == Suit.Hearts)
        {
            return "♥";
        }

        return "♠";
    }

    private Brush SuitBrush(Suit suit)
    {
        if (suit == Suit.Hearts || suit == Suit.Diamonds)
        {
            return Brushes.Red;
        }

        return Brushes.Black;
    }
}