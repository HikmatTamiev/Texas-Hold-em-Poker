using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    private bool isAllInRunout;

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

    private void ResetStacks_Click(object sender, RoutedEventArgs e)
    {
        you.SetChips(1000);
        opponent.SetChips(1000);
        pot = 0;
        youPostSmallBlind = true;
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
        RefreshDeckPile();

        street = 0;
        hasActed = false;
        facingBet = false;
        facingBetAmount = 0;
        isAllInRunout = false;
        opponentStyle = PickOpponentStyle();
        youPostSmallBlind = !youPostSmallBlind;

        if (youAreSmallBlindThisHand)
        {
            facingBet = true;
            facingBetAmount = 10;
            HandNameText.Text = blindText + " Preflop. Call 10, raise, or fold.";
            UpdateButtons();
            return;
        }

        HandNameText.Text = blindText + " Preflop. Opponent acts first.";
        OpponentActFirstPreflop();
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
        UpdateButtons();
    }

    private void Check_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3 || hasActed || facingBet)
        {
            return;
        }

        if (!youAreSmallBlindThisHand)
        {
            hasActed = true;
            HandNameText.Text = "You checked. Deal the next cards.";
            UpdateButtons();
            return;
        }

        OpponentReplyToCheck();
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
        ShowChips();

        if (paid < bet)
        {
            hasActed = true;
            HandNameText.Text = "You are all in for " + paid + ".";
            FinishAllIn();
            UpdateButtons();
            return;
        }

        if (you.Chips == 0 || opponent.Chips == 0)
        {
            hasActed = true;
            HandNameText.Text = "You called " + paid + ".";
            FinishAllIn();
            UpdateButtons();
            return;
        }

        if (street == 0 && youAreSmallBlindThisHand)
        {
            HandNameText.Text = "You called " + paid + ".";
            OpponentBigBlindOption();
            return;
        }

        hasActed = true;
        HandNameText.Text = "You called " + paid + ". Deal the next cards.";
        UpdateButtons();
    }

    private void Raise_Click(object sender, RoutedEventArgs e)
    {
        HideAmountPanels();
        PerformRaise(20);
    }

    private void RaiseMenu_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3 || !facingBet || you.Chips <= facingBetAmount)
        {
            return;
        }

        BetSliderPanel.Visibility = Visibility.Collapsed;

        if (RaiseSliderPanel.Visibility == Visibility.Visible)
        {
            RaiseSliderPanel.Visibility = Visibility.Collapsed;
            return;
        }

        int maxRaise = you.Chips - facingBetAmount;
        int minRaise = 20;
        if (maxRaise < minRaise)
        {
            minRaise = maxRaise;
        }

        RaiseSlider.Minimum = minRaise;
        RaiseSlider.Maximum = maxRaise;
        RaiseSlider.Value = minRaise;
        RaiseAmountText.Text = "+" + minRaise;
        RaiseSliderPanel.Visibility = Visibility.Visible;
    }

    private void RaiseSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RaiseAmountText == null)
        {
            return;
        }

        RaiseAmountText.Text = "+" + (int)RaiseSlider.Value;
    }

    private void ConfirmCustomRaise_Click(object sender, RoutedEventArgs e)
    {
        int raiseAmount = (int)RaiseSlider.Value;
        HideAmountPanels();
        PerformRaise(raiseAmount);
    }

    private void PerformRaise(int raiseAmount)
    {
        if (street == 3 || !facingBet)
        {
            return;
        }

        if (you.Chips <= facingBetAmount)
        {
            Call_Click(RaiseButton, new RoutedEventArgs());
            return;
        }

        int total = facingBetAmount + raiseAmount;

        if (you.Chips < total)
        {
            total = you.Chips;
            raiseAmount = total - facingBetAmount;
        }

        if (raiseAmount < 1)
        {
            return;
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
        HandNameText.Text = "You raised " + raiseAmount + ". Opponent calls " + opponentPaid + ".";

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
        HideAmountPanels();
        PerformBet(20);
    }

    private void BetMenu_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3 || hasActed || facingBet || you.Chips == 0)
        {
            return;
        }

        RaiseSliderPanel.Visibility = Visibility.Collapsed;

        if (BetSliderPanel.Visibility == Visibility.Visible)
        {
            BetSliderPanel.Visibility = Visibility.Collapsed;
            return;
        }

        int maxBet = you.Chips;
        int minBet = 20;
        if (maxBet < minBet)
        {
            minBet = maxBet;
        }

        BetSlider.Minimum = minBet;
        BetSlider.Maximum = maxBet;
        BetSlider.Value = minBet;
        BetAmountText.Text = minBet.ToString();
        BetSliderPanel.Visibility = Visibility.Visible;
    }

    private void BetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BetAmountText == null)
        {
            return;
        }

        BetAmountText.Text = ((int)BetSlider.Value).ToString();
    }

    private void ConfirmCustomBet_Click(object sender, RoutedEventArgs e)
    {
        int bet = (int)BetSlider.Value;
        HideAmountPanels();
        PerformBet(bet);
    }

    private void PerformBet(int bet)
    {
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

        if (bet < 1)
        {
            return;
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

    private void HideAmountPanels()
    {
        RaiseSliderPanel.Visibility = Visibility.Collapsed;
        BetSliderPanel.Visibility = Visibility.Collapsed;
    }

    private void AllIn_Click(object sender, RoutedEventArgs e)
    {
        HideAmountPanels();

        if (street == 3 || you.Chips == 0)
        {
            return;
        }

        if (facingBet)
        {
            int raiseExtra = you.Chips - facingBetAmount;

            if (raiseExtra <= 0)
            {
                Call_Click(sender, e);
                return;
            }

            PerformRaise(raiseExtra);
            return;
        }

        if (hasActed)
        {
            return;
        }

        PerformBet(you.Chips);
    }

    private void FinishAllIn()
    {
        isAllInRunout = true;

        while (street < 3)
        {
            hasActed = true;
            facingBet = false;
            facingBetAmount = 0;
            DealNext_Click(this, new RoutedEventArgs());
        }

        isAllInRunout = false;
    }

    private void DealNext_Click(object sender, RoutedEventArgs e)
    {
        if (street == 3 || !hasActed || facingBet)
        {
            return;
        }

        if (street == 0)
        {
            List<Card> flop = TakeCards(3);
            boardCards.AddRange(flop);
            ShowCards(flop, BoardPanel);
            RefreshDeckPile();
            street = 1;
            BeginStreet("Flop dealt.");
            return;
        }

        if (street == 1)
        {
            List<Card> turn = TakeCards(1);
            boardCards.AddRange(turn);
            ShowCards(turn, BoardPanel);
            RefreshDeckPile();
            street = 2;
            BeginStreet("Turn dealt.");
            return;
        }

        List<Card> river = TakeCards(1);
        boardCards.AddRange(river);
        ShowCards(river, BoardPanel);
        RefreshDeckPile();
        street = 3;
        Showdown();
        UpdateButtons();
    }

    private void BeginStreet(string dealtText)
    {
        hasActed = false;
        facingBet = false;
        facingBetAmount = 0;

        if (isAllInRunout)
        {
            hasActed = true;
            HandNameText.Text = dealtText;
            UpdateButtons();
            return;
        }

        if (!youAreSmallBlindThisHand)
        {
            HandNameText.Text = dealtText + " Opponent acts first.";
            OpponentOpenAction(dealtText);
            return;
        }

        HandNameText.Text = dealtText + " Check or bet.";
        UpdateButtons();
    }

    private void OpponentActFirstPreflop()
    {
        int complete = 10;
        int raiseExtra = 20;

        if (OpponentChoosesFoldBlind())
        {
            int won = pot;
            you.Receive(won);
            pot = 0;
            street = 3;
            hasActed = true;
            RevealOpponentCards();
            ShowChips();
            HandNameText.Text = "Opponent folds the small blind. You win " + won + ".";
            UpdateButtons();
            return;
        }

        if (opponent.Chips >= complete + raiseExtra && OpponentChoosesBet(complete + raiseExtra))
        {
            int paid = opponent.PayUpTo(complete + raiseExtra);
            pot = pot + paid;
            facingBet = true;
            facingBetAmount = 10 + paid - 20;
            if (facingBetAmount < 1)
            {
                facingBetAmount = paid;
            }

            hasActed = false;
            ShowChips();
            HandNameText.Text = "Opponent raises. Call " + facingBetAmount + ", raise, or fold.";
            UpdateButtons();
            return;
        }

        int called = opponent.PayUpTo(complete);
        pot = pot + called;
        ShowChips();

        if (opponent.Chips == 0)
        {
            hasActed = true;
            HandNameText.Text = "Opponent is all in for " + called + ".";
            FinishAllIn();
            UpdateButtons();
            return;
        }

        HandNameText.Text = "Opponent calls " + called + ". Check or raise.";
        UpdateButtons();
    }

    private void OpponentBigBlindOption()
    {
        int bet = 20;

        if (opponent.Chips < bet)
        {
            bet = opponent.Chips;
        }

        if (bet > 0 && OpponentChoosesBet(bet))
        {
            int paid = opponent.PayUpTo(bet);
            pot = pot + paid;
            facingBet = true;
            facingBetAmount = paid;
            hasActed = false;
            ShowChips();
            HandNameText.Text = HandNameText.Text + " Opponent raises " + paid + ". Call, raise, or fold.";
            UpdateButtons();
            return;
        }

        hasActed = true;
        HandNameText.Text = HandNameText.Text + " Opponent checks. Deal the flop.";
        UpdateButtons();
    }

    private void OpponentReplyToCheck()
    {
        int bet = 20;

        if (opponent.Chips < bet)
        {
            bet = opponent.Chips;
        }

        if (bet > 0 && OpponentChoosesBet(bet))
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

    private void OpponentOpenAction(string dealtText)
    {
        int bet = 20;

        if (opponent.Chips < bet)
        {
            bet = opponent.Chips;
        }

        if (bet > 0 && OpponentChoosesBet(bet))
        {
            int paid = opponent.PayUpTo(bet);
            pot = pot + paid;
            facingBet = true;
            facingBetAmount = paid;
            hasActed = false;
            ShowChips();
            HandNameText.Text = dealtText + " Opponent bets " + paid + ". Call, raise, or fold.";
            UpdateButtons();
            return;
        }

        HandNameText.Text = dealtText + " Opponent checks. Check or bet.";
        UpdateButtons();
    }

    private bool OpponentChoosesFoldBlind()
    {
        int power = OpponentPower();

        if (power >= 1)
        {
            return false;
        }

        int foldChance = 25;
        if (opponentStyle == OpponentStyle.Tight)
        {
            foldChance = 40;
        }
        else if (opponentStyle == OpponentStyle.Aggressive)
        {
            foldChance = 10;
        }

        return Chance(foldChance);
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
            int yourShare = pot - share;
            opponent.Receive(share);
            you.Receive(yourShare);
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
        int startIndex = panel.Children.Count;

        for (int i = 0; i < cards.Count; i++)
        {
            Border face = CreateCardFace(cards[i]);
            panel.Children.Add(face);
            AnimateDealIn(face, startIndex + i);
        }
    }

    private void ShowCardBacks(int count, StackPanel panel)
    {
        int startIndex = panel.Children.Count;

        for (int i = 0; i < count; i++)
        {
            Border back = CreateCardBack();
            panel.Children.Add(back);
            AnimateDealIn(back, startIndex + i);
        }
    }

    private void RevealOpponentCards()
    {
        OpponentPanel.Children.Clear();

        for (int i = 0; i < opponentHoleCards.Count; i++)
        {
            Border shell = CreateCardBack();
            OpponentPanel.Children.Add(shell);
            AnimateFlipReveal(shell, opponentHoleCards[i], i * 140);
        }
    }

    private void AnimateDealIn(UIElement card, int index)
    {
        card.Opacity = 0;

        TranslateTransform slide = new TranslateTransform(0, -22);
        card.RenderTransform = slide;
        card.RenderTransformOrigin = new Point(0.5, 0.5);

        TimeSpan delay = TimeSpan.FromMilliseconds(index * 85);
        Duration length = new Duration(TimeSpan.FromMilliseconds(300));

        DoubleAnimation fade = new DoubleAnimation(0, 1, length);
        fade.BeginTime = delay;
        fade.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        DoubleAnimation move = new DoubleAnimation(-22, 0, length);
        move.BeginTime = delay;
        move.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        card.BeginAnimation(UIElement.OpacityProperty, fade);
        slide.BeginAnimation(TranslateTransform.YProperty, move);
    }

    private void AnimateFlipReveal(Border shell, Card card, int delayMs)
    {
        shell.Effect = null;
        shell.RenderTransformOrigin = new Point(0.5, 0.5);
        ScaleTransform scale = new ScaleTransform(1, 1);
        shell.RenderTransform = scale;

        DoubleAnimation close = new DoubleAnimation(1, 0.02, new Duration(TimeSpan.FromMilliseconds(160)));
        close.BeginTime = TimeSpan.FromMilliseconds(delayMs);
        close.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn };
        close.FillBehavior = FillBehavior.Stop;

        close.Completed += (_, _) =>
        {
            if (shell.Parent == null)
            {
                return;
            }

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.ScaleX = 0.02;

            Image picture = new Image();
            picture.Source = LoadCardPicture(CardFileName(card));
            picture.Stretch = Stretch.Uniform;

            shell.Background = new SolidColorBrush(Color.FromRgb(255, 252, 245));
            shell.BorderBrush = new SolidColorBrush(Color.FromRgb(210, 198, 176));
            shell.BorderThickness = new Thickness(1);
            shell.CornerRadius = new CornerRadius(6);
            shell.Child = picture;

            DoubleAnimation open = new DoubleAnimation(0.02, 1, new Duration(TimeSpan.FromMilliseconds(180)));
            open.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            open.FillBehavior = FillBehavior.Stop;
            open.Completed += (_, _) =>
            {
                if (shell.Parent == null)
                {
                    return;
                }

                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.ScaleX = 1;
                shell.Effect = CardShadow();
            };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, open);
        };

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, close);
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
        bool canAllIn = handOpen && you.Chips > 0 && ((!hasActed && !facingBet) || facingBet);
        bool canDeal = handOpen && hasActed && !facingBet;
        bool handOver = !handOpen;

        ShowButton(FoldButton, handOpen);
        ShowButton(CheckButton, canOpenAction);
        ShowButton(BetButton, canBet);
        ShowButton(BetMenuButton, canBet);
        ShowButton(CallButton, canAnswerBet);
        ShowButton(RaiseButton, canRaise);
        ShowButton(RaiseMenuButton, canRaise);
        ShowButton(AllInButton, canAllIn);
        ShowButton(DealNextButton, canDeal);
        ShowButton(NewHandButton, handOver);
        ShowButton(ResetStacksButton, handOver);

        if (canAllIn)
        {
            AllInButton.Content = "All in " + you.Chips;
        }

        if (!canRaise)
        {
            RaiseSliderPanel.Visibility = Visibility.Collapsed;
        }

        if (!canBet)
        {
            BetSliderPanel.Visibility = Visibility.Collapsed;
        }

        if (facingBet)
        {
            CallButton.Content = "Call " + facingBetAmount;
        }
        else
        {
            CallButton.Content = "Call";
        }
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

    private Border CreateCardBack()
    {
        Image picture = new Image();
        picture.Source = LoadCardPicture("cardbackimg.png");
        picture.Stretch = Stretch.Fill;
        picture.Width = 86;
        picture.Height = 124;

        Border back = new Border();
        back.Width = 86;
        back.Height = 124;
        back.Margin = new Thickness(5);
        back.CornerRadius = new CornerRadius(8);
        back.Background = Brushes.White;
        back.BorderThickness = new Thickness(0);
        back.ClipToBounds = true;
        back.Effect = CardShadow();
        back.Child = picture;

        return back;
    }

    private int CardsLeftInDeck()
    {
        return 52 - holeCards.Count - opponentHoleCards.Count - boardCards.Count;
    }

    private void RefreshDeckPile()
    {
        DeckCanvas.Children.Clear();

        int left = CardsLeftInDeck();
        if (left < 0)
        {
            left = 0;
        }

        DeckCountText.Text = left.ToString();

        if (left == 0)
        {
            return;
        }

        int layers = left / 4;
        if (layers < 1)
        {
            layers = 1;
        }

        if (layers > 8)
        {
            layers = 8;
        }

        for (int i = 0; i < layers; i++)
        {
            Border card = CreateCardBack();
            card.Width = 84;
            card.Height = 120;
            card.Margin = new Thickness(0);
            card.Effect = null;
            if (card.Child is Image deckPicture)
            {
                deckPicture.Width = 84;
                deckPicture.Height = 120;
            }

            Canvas.SetLeft(card, 4 + i * 1.2);
            Canvas.SetTop(card, 4 + (layers - 1 - i) * 1.2);
            DeckCanvas.Children.Add(card);
        }
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