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
    private Deck deck = new Deck();
    private Player you;
    private Player opponent;
    private List<Card> holeCards = new List<Card>();
    private List<Card> opponentHoleCards = new List<Card>();
    private List<Card> boardCards = new List<Card>();
    private int pot;
    private int street;
    private bool hasActed;
    private bool facingBet;
    private int facingBetAmount;
    private bool youPostSmallBlind = true;
    private Random random = new Random();

    public MainWindow()
    {
        InitializeComponent();

        you = new Player("You", 1000);
        opponent = new Player("Opponent", 1000);
        StartHand();
    }

    private void NewHand_Click(object sender, RoutedEventArgs e)
    {
        StartHand();
    }

    private void StartHand()
    {
        int smallBlind = 10;
        int bigBlind = 20;
        Player smallBlindPlayer;
        Player bigBlindPlayer;
        string blindText;

        if (youPostSmallBlind)
        {
            smallBlindPlayer = you;
            bigBlindPlayer = opponent;
            blindText = "You post the small blind, 10. Opponent posts 20.";
        }
        else
        {
            smallBlindPlayer = opponent;
            bigBlindPlayer = you;
            blindText = "Opponent posts the small blind, 10. You post 20.";
        }

        if (smallBlindPlayer.Chips < smallBlind || bigBlindPlayer.Chips < bigBlind)
        {
            HandNameText.Text = "Not enough chips for another hand.";
            return;
        }

        HolePanel.Children.Clear();
        BoardPanel.Children.Clear();
        OpponentPanel.Children.Clear();

        smallBlindPlayer.Pay(smallBlind);
        bigBlindPlayer.Pay(bigBlind);
        pot = smallBlind + bigBlind;
        ShowChips();

        deck = new Deck();
        deck.Shuffle();
        boardCards = new List<Card>();

        holeCards = TakeCards(2);
        ShowCards(holeCards, HolePanel);
        opponentHoleCards = TakeCards(2);
        ShowCardBacks(2, OpponentPanel);

        street = 0;
        hasActed = false;
        facingBet = false;
        facingBetAmount = 0;
        youPostSmallBlind = !youPostSmallBlind;
        HandNameText.Text = blindText + " Check or bet, then deal the flop.";
    }

    private void Fold_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3)
        {
            return;
        }

        int won = pot;
        opponent.Receive(won);
        pot = 0;
        street = 3;
        hasActed = true;
        facingBet = false;
        facingBetAmount = 0;
        ShowChips();
        HandNameText.Text = "You folded. Opponent wins " + won + ".";
    }

    private void Check_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3 || hasActed || facingBet)
        {
            return;
        }

        int bet = 20;

        if (opponent.Chips < bet)
        {
            bet = opponent.Chips;
        }

        bool opponentBets = bet > 0 && OpponentChoosesBet(bet);

        if (opponentBets)
        {
            int paid = opponent.PayUpTo(bet);
            pot = pot + paid;
            facingBet = true;
            facingBetAmount = paid;
            hasActed = false;
            ShowChips();
            HandNameText.Text = "You checked. Opponent bets " + paid + ". Call, raise, or fold.";
            return;
        }

        hasActed = true;
        HandNameText.Text = "You checked. Opponent checks. Deal the next cards.";
    }

    private void Call_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3 || !facingBet)
        {
            return;
        }

        int bet = facingBetAmount;
        int paid = you.PayUpTo(bet);
        pot = pot + paid;

        int unmatched = bet - paid;
        if (unmatched > 0)
        {
            opponent.Receive(unmatched);
            pot = pot - unmatched;
        }

        facingBet = false;
        facingBetAmount = 0;
        hasActed = true;
        ShowChips();

        if (paid < bet)
        {
            HandNameText.Text = "You are all in for " + paid + ".";
            FinishAllIn();
            return;
        }

        HandNameText.Text = "You called " + paid + ". Deal the next cards.";

        if (you.Chips == 0 || opponent.Chips == 0)
        {
            FinishAllIn();
        }
    }

    private void Raise_Click(object sender, RoutedEventArgs e)
    {
        int raiseAmount = 20;

        if (street == 3 || !facingBet)
        {
            return;
        }

        if (you.Chips <= facingBetAmount)
        {
            Call_Click(sender, e);
            return;
        }

        int total = facingBetAmount + raiseAmount;

        if (you.Chips < total)
        {
            total = you.Chips;
            raiseAmount = total - facingBetAmount;
        }

        you.Pay(total);
        pot = pot + total;
        facingBet = false;
        facingBetAmount = 0;

        if (OpponentChoosesFold())
        {
            int won = pot;
            you.Receive(won);
            pot = 0;
            street = 3;
            hasActed = true;
            RevealOpponentCards();
            ShowChips();
            HandNameText.Text = "You raised. Opponent folds. You win " + won + ".";
            return;
        }

        int opponentPaid = opponent.PayUpTo(raiseAmount);
        pot = pot + opponentPaid;

        int unmatched = raiseAmount - opponentPaid;
        if (unmatched > 0)
        {
            you.Receive(unmatched);
            pot = pot - unmatched;
        }

        hasActed = true;
        ShowChips();
        HandNameText.Text = "You raised. Opponent calls " + opponentPaid + ".";

        if (you.Chips == 0 || opponent.Chips == 0)
        {
            FinishAllIn();
            return;
        }

        HandNameText.Text = HandNameText.Text + " Deal the next cards.";
    }

    private void Bet_Click(object sender, RoutedEventArgs e)
    {
        int bet = 20;

        if (street == 3 || hasActed || facingBet)
        {
            return;
        }

        if (you.Chips == 0)
        {
            HandNameText.Text = "Not enough chips.";
            return;
        }

        if (you.Chips < bet)
        {
            bet = you.Chips;
        }

        you.Pay(bet);
        pot = pot + bet;

        if (OpponentChoosesFold())
        {
            int won = pot;
            you.Receive(won);
            pot = 0;
            street = 3;
            hasActed = true;
            facingBet = false;
            RevealOpponentCards();
            ShowChips();
            HandNameText.Text = "You bet " + bet + ". Opponent folds. You win " + won + ".";
            return;
        }

        int opponentPaid = opponent.PayUpTo(bet);
        pot = pot + opponentPaid;

        int unmatched = bet - opponentPaid;
        if (unmatched > 0)
        {
            you.Receive(unmatched);
            pot = pot - unmatched;
        }

        hasActed = true;
        ShowChips();
        HandNameText.Text = "You bet " + bet + ". Opponent calls " + opponentPaid + ".";

        if (you.Chips == 0 || opponent.Chips == 0)
        {
            FinishAllIn();
            return;
        }

        HandNameText.Text = HandNameText.Text + " Deal the next cards.";
    }

    private void FinishAllIn()
    {
        while (street < 3)
        {
            hasActed = true;
            facingBet = false;
            DealNext_Click(this, new RoutedEventArgs());
        }
    }

    private void DealNext_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3 || !hasActed || facingBet)
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
        RevealOpponentCards();

        List<Card> yourSeven = new List<Card>();
        yourSeven.AddRange(holeCards);
        yourSeven.AddRange(boardCards);

        List<Card> opponentSeven = new List<Card>();
        opponentSeven.AddRange(opponentHoleCards);
        opponentSeven.AddRange(boardCards);

        HandEvaluator evaluator = new HandEvaluator();
        HandStrength yourHand = evaluator.BestHand(yourSeven);
        HandStrength opponentHand = evaluator.BestHand(opponentSeven);
        int comparison = yourHand.Compare(opponentHand);

        if (comparison > 0)
        {
            int won = pot;
            you.Receive(won);
            pot = 0;
            ShowChips();
            HandNameText.Text = "You win " + won + " with " + evaluator.Describe(yourHand.Category) + ".";
        }
        else if (comparison < 0)
        {
            int won = pot;
            opponent.Receive(won);
            pot = 0;
            ShowChips();
            HandNameText.Text = "Opponent wins " + won + " with " + evaluator.Describe(opponentHand.Category) + ".";
        }
        else
        {
            int share = pot / 2;
            opponent.Receive(share);
            you.Receive(pot - share);
            pot = 0;
            ShowChips();
            HandNameText.Text = "Both have " + evaluator.Describe(yourHand.Category) + ". The pot is split.";
        }
    }

    private HandStrength OpponentStrength()
    {
        List<Card> cards = new List<Card>();
        cards.AddRange(opponentHoleCards);
        cards.AddRange(boardCards);

        if (cards.Count < 5)
        {
            int first = (int)opponentHoleCards[0].Rank;
            int second = (int)opponentHoleCards[1].Rank;

            if (first < second)
            {
                int held = first;
                first = second;
                second = held;
            }

            if (first == second)
            {
                return new HandStrength(HandCategory.Pair, new int[] { first });
            }

            return new HandStrength(HandCategory.HighCard, new int[] { first, second });
        }

        HandEvaluator evaluator = new HandEvaluator();
        return evaluator.BestHand(cards);
    }

    private bool OpponentChoosesBet(int bet)
    {
        if (opponent.Chips < bet)
        {
            return false;
        }

        HandStrength strength = OpponentStrength();

        if (strength.Category >= HandCategory.Pair)
        {
            return true;
        }

        if (strength.Kickers.Length > 0 && strength.Kickers[0] >= 13)
        {
            return random.Next(2) == 0;
        }

        return false;
    }

    private bool OpponentChoosesFold()
    {
        HandStrength strength = OpponentStrength();

        if (strength.Category >= HandCategory.Pair)
        {
            return false;
        }

        if (strength.Kickers.Length > 0 && strength.Kickers[0] >= 12)
        {
            return false;
        }

        return true;
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

    private void ShowCardBacks(int count, StackPanel panel)
    {
        for (int i = 0; i < count; i++)
        {
            panel.Children.Add(CreateCardBack());
        }
    }

    private void RevealOpponentCards()
    {
        OpponentPanel.Children.Clear();
        ShowCards(opponentHoleCards, OpponentPanel);
    }

    private void ShowChips()
    {
        ChipText.Text = you.Name + ": " + you.Chips + " chips"
            + "\n" + opponent.Name + ": " + opponent.Chips + " chips"
            + "\nPot: " + pot;
    }

    private Border CreateCardBack()
    {
        TextBlock label = new TextBlock();
        label.Text = "?";
        label.FontSize = 36;
        label.Foreground = Brushes.White;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;

        Border back = new Border();
        back.Width = 72;
        back.Height = 104;
        back.Margin = new Thickness(4);
        back.Background = new SolidColorBrush(Color.FromRgb(20, 60, 140));
        back.BorderBrush = Brushes.White;
        back.BorderThickness = new Thickness(2);
        back.CornerRadius = new CornerRadius(8);
        back.Child = label;

        return back;
    }

    private Border CreateCardFace(Card card)
    {
        TextBlock label = new TextBlock();
        label.Text = RankLabel(card.Rank) + "\n" + SuitSymbol(card.Suit);
        label.FontSize = 22;
        label.Foreground = SuitBrush(card.Suit);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;

        Border face = new Border();
        face.Width = 72;
        face.Height = 104;
        face.Margin = new Thickness(4);
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