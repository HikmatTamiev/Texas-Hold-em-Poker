using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TexasHoldem;

enum OpponentStyle
{
    Tight,
    Normal,
    Aggressive
}

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
    private bool youAreSmallBlindThisHand = true;
    private Random random = new Random();
    private Dictionary<string, BitmapImage> cardPictures = new Dictionary<string, BitmapImage>();
    private OpponentStyle opponentStyle = OpponentStyle.Normal;

    public MainWindow()
    {
        InitializeComponent();

        you = new Player("You", 1000);
        opponent = new Player("Opponent", 1000);
        StartHand();
        HistoryText.Text = "Last hand: none yet.";
    }

    private void NewHand_Click(object sender, RoutedEventArgs e)
    {
        StartHand();
    }

    private void ResetStacks_Click(object sender, RoutedEventArgs e)
    {
        you.SetChips(1000);
        opponent.SetChips(1000);
        pot = 0;
        youPostSmallBlind = true;
        HistoryText.Text = "Last hand: stacks reset to 1000.";
        StartHand();
        HandNameText.Text = "Stacks reset to 1000. " + HandNameText.Text;
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
            HandNameText.Text = "Not enough chips for another hand. Use Reset stacks.";
            UpdateButtons();
            return;
        }

        HolePanel.Children.Clear();
        BoardPanel.Children.Clear();
        OpponentPanel.Children.Clear();

        youAreSmallBlindThisHand = smallBlindPlayer == you;
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
        opponentStyle = PickOpponentStyle();
        youPostSmallBlind = !youPostSmallBlind;
        HandNameText.Text = blindText + " Check or bet, then deal the flop.";
        UpdateButtons();
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
        RecordHistory("Opponent won " + won + " chips after you folded.");
        UpdateButtons();
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
            UpdateButtons();
            return;
        }

        hasActed = true;
        HandNameText.Text = "You checked. Opponent checks. Deal the next cards.";
        UpdateButtons();
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
            UpdateButtons();
            return;
        }

        HandNameText.Text = "You called " + paid + ". Deal the next cards.";

        if (you.Chips == 0 || opponent.Chips == 0)
        {
            FinishAllIn();
        }

        UpdateButtons();
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
            RecordHistory("You won " + won + " chips after the opponent folded to a raise.");
            UpdateButtons();
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
            UpdateButtons();
            return;
        }

        HandNameText.Text = HandNameText.Text + " Deal the next cards.";
        UpdateButtons();
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
            RecordHistory("You won " + won + " chips after the opponent folded to a bet.");
            UpdateButtons();
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
            UpdateButtons();
            return;
        }

        HandNameText.Text = HandNameText.Text + " Deal the next cards.";
        UpdateButtons();
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
            UpdateButtons();
            return;
        }

        if (street == 1)
        {
            List<Card> turn = TakeCards(1);
            boardCards.AddRange(turn);
            ShowCards(turn, BoardPanel);
            street = 2;
            HandNameText.Text = "Turn. Check or bet.";
            UpdateButtons();
            return;
        }

        List<Card> river = TakeCards(1);
        boardCards.AddRange(river);
        ShowCards(river, BoardPanel);
        street = 3;
        Showdown();
        UpdateButtons();
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
            RecordHistory("You won " + won + " chips with " + evaluator.Describe(yourHand.Category) + ".");
        }
        else if (comparison < 0)
        {
            int won = pot;
            opponent.Receive(won);
            pot = 0;
            ShowChips();
            HandNameText.Text = "Opponent wins " + won + " with " + evaluator.Describe(opponentHand.Category) + ".";
            RecordHistory("Opponent won " + won + " chips with " + evaluator.Describe(opponentHand.Category) + ".");
        }
        else
        {
            int share = pot / 2;
            int yourShare = pot - share;
            opponent.Receive(share);
            you.Receive(yourShare);
            pot = 0;
            ShowChips();
            HandNameText.Text = "Both have " + evaluator.Describe(yourHand.Category) + ". The pot is split.";
            RecordHistory("The pot was split. You took " + yourShare + ", opponent took " + share + ".");
        }
    }

    private void RecordHistory(string summary)
    {
        HistoryText.Text = "Last hand: " + summary;
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

    private OpponentStyle PickOpponentStyle()
    {
        int roll = random.Next(100);

        if (roll < 25)
        {
            return OpponentStyle.Tight;
        }

        if (roll < 70)
        {
            return OpponentStyle.Normal;
        }

        return OpponentStyle.Aggressive;
    }

    private int OpponentPower()
    {
        HandStrength strength = OpponentStrength();

        if (strength.Category >= HandCategory.TwoPair)
        {
            return 3;
        }

        if (strength.Category >= HandCategory.Pair)
        {
            return 2;
        }

        if (strength.Kickers.Length > 0 && strength.Kickers[0] >= 13)
        {
            return 1;
        }

        return 0;
    }

    private bool Chance(int percent)
    {
        return random.Next(100) < percent;
    }

    private bool OpponentChoosesBet(int bet)
    {
        if (opponent.Chips < bet)
        {
            return false;
        }

        int power = OpponentPower();

        if (power >= 2)
        {
            int trapChance = 20;
            if (opponentStyle == OpponentStyle.Tight)
            {
                trapChance = 35;
            }
            else if (opponentStyle == OpponentStyle.Aggressive)
            {
                trapChance = 8;
            }

            return !Chance(trapChance);
        }

        if (power == 1)
        {
            int valueBetChance = 45;
            if (opponentStyle == OpponentStyle.Tight)
            {
                valueBetChance = 25;
            }
            else if (opponentStyle == OpponentStyle.Aggressive)
            {
                valueBetChance = 70;
            }

            return Chance(valueBetChance);
        }

        int bluffChance = 18;
        if (opponentStyle == OpponentStyle.Tight)
        {
            bluffChance = 8;
        }
        else if (opponentStyle == OpponentStyle.Aggressive)
        {
            bluffChance = 35;
        }

        if (street >= 2)
        {
            bluffChance = bluffChance + 5;
        }

        return Chance(bluffChance);
    }

    private bool OpponentChoosesFold()
    {
        int power = OpponentPower();

        if (power >= 2)
        {
            return false;
        }

        if (power == 1)
        {
            int foldChance = 10;
            if (opponentStyle == OpponentStyle.Tight)
            {
                foldChance = 20;
            }
            else if (opponentStyle == OpponentStyle.Aggressive)
            {
                foldChance = 5;
            }

            return Chance(foldChance);
        }

        int weakFoldChance = 80;
        if (opponentStyle == OpponentStyle.Tight)
        {
            weakFoldChance = 92;
        }
        else if (opponentStyle == OpponentStyle.Aggressive)
        {
            weakFoldChance = 60;
        }

        return Chance(weakFoldChance);
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
        string yourMark = "";
        string opponentMark = "";

        if (youAreSmallBlindThisHand)
        {
            yourMark = " (SB)";
            opponentMark = " (BB)";
        }
        else
        {
            yourMark = " (BB)";
            opponentMark = " (SB)";
        }

        YouBlindText.Text = yourMark.Trim(' ', '(', ')');
        OppBlindText.Text = opponentMark.Trim(' ', '(', ')');
        YouChipsText.Text = you.Chips.ToString();
        OppChipsText.Text = opponent.Chips.ToString();
        PotAmountText.Text = pot.ToString();
        DrawChipStack(YouChipStack, you.Chips);
        DrawChipStack(OppChipStack, opponent.Chips);
        DrawChipStack(PotChipStack, pot);
    }

    private void DrawChipStack(StackPanel panel, int amount)
    {
        panel.Children.Clear();

        int shown = amount / 100;
        if (amount > 0 && shown < 1)
        {
            shown = 1;
        }

        if (shown > 8)
        {
            shown = 8;
        }

        Color[] colors =
        {
            Color.FromRgb(196, 58, 58),
            Color.FromRgb(47, 111, 196),
            Color.FromRgb(46, 158, 90),
            Color.FromRgb(232, 232, 232),
            Color.FromRgb(92, 46, 140),
            Color.FromRgb(196, 123, 18)
        };

        for (int i = 0; i < shown; i++)
        {
            Ellipse chip = new Ellipse();
            chip.Width = 18;
            chip.Height = 18;
            chip.Margin = new Thickness(2, 0, 2, 0);
            chip.Fill = new SolidColorBrush(colors[i % colors.Length]);
            chip.Stroke = new SolidColorBrush(Color.FromRgb(243, 211, 138));
            chip.StrokeThickness = 2;
            panel.Children.Add(chip);
        }
    }

    private void UpdateButtons()
    {
        bool handOpen = street < 3;
        bool canOpenAction = handOpen && !hasActed && !facingBet;
        bool canAnswerBet = handOpen && facingBet;

        bool canBet = canOpenAction && you.Chips > 0;
        bool canRaise = canAnswerBet && you.Chips > facingBetAmount;
        bool canDeal = handOpen && hasActed && !facingBet;
        bool handOver = !handOpen;

        ShowButton(FoldButton, handOpen);
        ShowButton(CheckButton, canOpenAction);
        ShowButton(BetButton, canBet);
        ShowButton(CallButton, canAnswerBet);
        ShowButton(RaiseButton, canRaise);
        ShowButton(DealNextButton, canDeal);
        ShowButton(NewHandButton, handOver);
        ShowButton(ResetStacksButton, handOver);

        if (facingBet)
        {
            CallButton.Content = "Call " + facingBetAmount;
        }
        else
        {
            CallButton.Content = "Call";
        }

        UpdateStatus();
    }

    private void ShowButton(Button button, bool show)
    {
        if (show)
        {
            button.Visibility = Visibility.Visible;
            button.IsEnabled = true;
        }
        else
        {
            button.Visibility = Visibility.Collapsed;
            button.IsEnabled = false;
        }
    }

    private void UpdateStatus()
    {
        if (street >= 3)
        {
            StatusText.Text = "Hand over. Start a new hand when you are ready.";
            return;
        }

        if (facingBet)
        {
            StatusText.Text = "Your turn: call, raise, or fold.";
            return;
        }

        if (!hasActed)
        {
            StatusText.Text = "Your turn: check or bet.";
            return;
        }

        StatusText.Text = "Street finished. Deal the next cards.";
    }

    private Border CreateCardBack()
    {
        Border back = new Border();
        back.Width = 86;
        back.Height = 124;
        back.Margin = new Thickness(5);
        back.CornerRadius = new CornerRadius(8);
        back.Background = new SolidColorBrush(Color.FromRgb(18, 52, 122));
        back.BorderBrush = new SolidColorBrush(Color.FromRgb(243, 211, 138));
        back.BorderThickness = new Thickness(3);
        back.Effect = CardShadow();

        TextBlock monogram = new TextBlock();
        monogram.Text = "♠";
        monogram.FontSize = 32;
        monogram.Foreground = new SolidColorBrush(Color.FromRgb(243, 211, 138));
        monogram.HorizontalAlignment = HorizontalAlignment.Center;
        monogram.VerticalAlignment = VerticalAlignment.Center;
        back.Child = monogram;

        return back;
    }

    private DropShadowEffect CardShadow()
    {
        DropShadowEffect shadow = new DropShadowEffect();
        shadow.BlurRadius = 12;
        shadow.ShadowDepth = 3;
        shadow.Opacity = 0.45;
        shadow.Color = Colors.Black;
        return shadow;
    }

    private Border CreateCardFace(Card card)
    {
        Image picture = new Image();
        picture.Source = LoadCardPicture(CardFileName(card));
        picture.Stretch = Stretch.Uniform;

        Border face = new Border();
        face.Width = 86;
        face.Height = 124;
        face.Margin = new Thickness(5);
        face.CornerRadius = new CornerRadius(6);
        face.Background = Brushes.White;
        face.Effect = CardShadow();
        face.Child = picture;

        return face;
    }

    private BitmapImage LoadCardPicture(string fileName)
    {
        if (cardPictures.ContainsKey(fileName))
        {
            return cardPictures[fileName];
        }

        BitmapImage picture = new BitmapImage();
        picture.BeginInit();
        picture.UriSource = new Uri("pack://application:,,,/Images/Cards/" + fileName, UriKind.Absolute);
        picture.CacheOption = BitmapCacheOption.OnLoad;
        picture.EndInit();
        picture.Freeze();
        cardPictures[fileName] = picture;
        return picture;
    }

    private string CardFileName(Card card)
    {
        string rankName;

        if (card.Rank == Rank.Ace)
        {
            rankName = "ace";
        }
        else if (card.Rank == Rank.King)
        {
            rankName = "king";
        }
        else if (card.Rank == Rank.Queen)
        {
            rankName = "queen";
        }
        else if (card.Rank == Rank.Jack)
        {
            rankName = "jack";
        }
        else
        {
            rankName = ((int)card.Rank).ToString();
        }

        string suitName = card.Suit.ToString().ToLower();
        return rankName + "_of_" + suitName + ".png";
    }
}