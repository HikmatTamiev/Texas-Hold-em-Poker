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
    public MainWindow()
    {
        InitializeComponent();

        Player you = new Player("You", 1000);
        Player opponent = new Player("Opponent", 1000);

        int smallBlind = 10;
        int bigBlind = 20;

        you.Pay(smallBlind);
        opponent.Pay(bigBlind);

        int pot = smallBlind + bigBlind;
        ChipText.Text = you.Name + ": " + you.Chips + " chips"
            + "\n" + opponent.Name + ": " + opponent.Chips + " chips"
            + "\nPot: " + pot;

        Deck deck = new Deck();
        deck.Shuffle();

        List<Card> holeCards = DealTo(deck, 2, HolePanel);
        List<Card> boardCards = DealTo(deck, 5, BoardPanel);

        List<Card> allCards = new List<Card>();
        allCards.AddRange(holeCards);
        allCards.AddRange(boardCards);

        HandEvaluator evaluator = new HandEvaluator();
        HandCategory category = evaluator.BestOfSeven(allCards);
        HandNameText.Text = evaluator.Describe(category);
    }

    private List<Card> DealTo(Deck deck, int count, StackPanel panel)
    {
        List<Card> dealt = new List<Card>();

        for (int i = 0; i < count; i++)
        {
            Card card = deck.Deal();
            dealt.Add(card);
            panel.Children.Add(CreateCardFace(card));
        }

        return dealt;
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