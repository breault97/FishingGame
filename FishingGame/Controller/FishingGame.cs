using FishingGame.Domain.Class;
using FishingGame.Domain.Struct;
using FishingGame.Domain.Events;
using FishingGame.Domain.Interfaces;
using FishingGame.Domain.Interfaces;
using FishingGame.Domain.Events;
using System.Threading.Tasks;


namespace FishingGame.Controller
{
    public delegate void ChangeStrategyHandler(object sender, ChangeStrategyEventArgs e);
    public class FishingGame : IFishingGameObserver
    {
        #region Properties
        public event EventHandler<TurnEventArgs>? TurnPlayed;
        public event EventHandler<DrawEventArgs>? HaveDrawCards;
        public event EventHandler<InfoEventArgs>? DeckAlmostEmpty;
        public event EventHandler<InfoEventArgs>? Info;
        public event EventHandler<PlayerEventArgs>? OneCard;
        public event EventHandler<PlayerEventArgs>? GameWon;
        public event EventHandler<HumanTurnEventArgs>? HumanTurnStarted;
        public event EventHandler<PlayerEventArgs>? HumanTurnSkipped;
        public event ChangeStrategyHandler? ChangeStrategyHandler;
        public Player CurrentPlayer { get; set; }
        public IHumanInput? HumanInput { get; set; }
        private int Sens { get; set; }
        private bool PassNextPlayer {  get; set; }
        private List<Player> OnlyOneCard { get; set; }
        public GameBoard Board { get; set; }
        public int DrawPileCount    => Board.DrawPileCount;
        public int DepositPileCount => Board.DepositPileCount;
        private Card? TopState() => Board.DepositStack.Count > 0 ? Board.DepositStack.Peek() : null;
        private readonly UiClock _clock = new();

        public int UiRenderDelayMs
        {
            get => _clock.DelayMs;
            set => _clock.SetDelay(value);
        }

        #endregion

        #region Constructeur
        public FishingGame(int nbrPlayers)
        {

            Sens = 1;
            PassNextPlayer = false;
            OnlyOneCard = new List<Player>();
            Board = new GameBoard();
            Board.DrawStackRecycled += () => OnDeckAlmostEmpty("La pile de pioche est vide - brassage des cartes");
            
            InitialisePlayers(nbrPlayers);

            SetFirstPlayer();
        }
        #endregion
        
        #region Events
        private void OnTurnPlayed(Player p, Card? played, Card? top)
        {
            TurnEventArgs args = new TurnEventArgs { Player = p, PlayedCard = played, TopOfDeposit = top , Sens = Sens };
            TurnPlayed?.Invoke(this, args);
        }
        private void OnHaveDraw(Player p, int n, Card? top, int sens)
        {
            DrawEventArgs args = new DrawEventArgs { Player = p, Count = n, TopOfDeposit = top, Sens = sens };
            HaveDrawCards?.Invoke(this, args);
        }
        private void OnOneCard(Player p) => OneCard?.Invoke(this, new PlayerEventArgs { Player = p });
        private void OnGameWon(Player p) => GameWon?.Invoke(this, new PlayerEventArgs { Player = p });

        private void OnDeckAlmostEmpty(string msg)
        {
            InfoEventArgs args = new InfoEventArgs { Message = msg };
            DeckAlmostEmpty?.Invoke(this, args);
        }

        private void OnInfo(string msg)
        {
            
            InfoEventArgs args = new InfoEventArgs { Message = msg };
            Info?.Invoke(this, args);
        }

        //Lève l'élèvement comme quoi il faut potentiellement changer de stratégie.
        public virtual void RaiseChangeStrategyEvent()
        {
            ChangeStrategyHandler? handlers = ChangeStrategyHandler;

            ChangeStrategyEventArgs eventArgs = new ChangeStrategyEventArgs(Sens);

            if (handlers != null)
            {
                handlers(this, eventArgs);
            }
        }
        
        // Repropage l'événement vers Form1 (facilite l'abonnement côté UI)
        public event EventHandler<int>? DelayChanged
        {
            add    { _clock.DelayChanged += value; }
            remove { _clock.DelayChanged -= value; }
        }
        
        private void OnHumanTurnStarted(Player player, Card? top, int pendingDraw)
        {
            var args = new HumanTurnEventArgs { Player = player, TopCard = top, PendingDraw = pendingDraw };
            HumanTurnStarted?.Invoke(this, args);
        }

        private void OnHumanTurnSkipped(Player player)
        {
            HumanTurnSkipped?.Invoke(this, new PlayerEventArgs { Player = player });
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Démarrer une partie selon nombre de carte par joueur
        /// </summary>
        /// <param name="nbrCard">Quantité de cartes par joueur entre 5 et 8 </param>
        public void Begin(int nbrCard)
        {
            if (nbrCard < 5 || nbrCard > 8)
                throw new ArgumentOutOfRangeException(nameof(nbrCard));
            
            //Distribution des cartes initiales
            Board.DistributBeginingCards(CurrentPlayer, nbrCard);
            OnInfo($"Distribution de {nbrCard} cartes aux joueurs");
        }

        public async Task Play()
        {
            var end = false;

            while (!end)
            {
                await Task.Delay(UiRenderDelayMs); // petit délai pour lire la console (consigne)

                if (PassNextPlayer)
                {
                    string msg = Sens == 1 ? " - Carte Actuelle:" + TopState() + " - Sens: Horaire" : " - Carte Actuelle:" + TopState() + " - Sens: Anti-Horaire";
                    OnInfo("Le tour de " + CurrentPlayer + " est passé à cause de l'as" + msg);
                    
                    // Notification et réinitialisation de l’attente si joueur humain
                    if (CurrentPlayer.IsHuman)
                    {
                        OnHumanTurnSkipped(CurrentPlayer);
                        HumanInput?.Reset();
                    }
                    
                    PassNextPlayer = false;
                    NextPlayer();
                    continue;
                }

                int nbrCardBefore = CurrentPlayer.Hand.Count();
                Card? selectedCard = null;
                
                // Brancher si le joueur est humain
                if (CurrentPlayer.IsHuman && HumanInput != null)
                {
                    // Notifie l’UI et attend la décision de l’utilisateur
                    OnHumanTurnStarted(CurrentPlayer, TopState(), Board.nbrTwo);
                    
                    Domain.Enums.COLOR_TYPE? chosenColor = null;
                    try
                    {
                        var result = await HumanInput.WaitForActionAsync(System.Threading.CancellationToken.None);
                        selectedCard = result.card;
                        chosenColor = result.chosenColor;
                    }
                    catch (TaskCanceledException)
                    {
                        // la partie a été annulée (reset), on sort
                        return;
                    }

                    // Si aucune carte jouée ou carte invalide, on pioche
                    if (selectedCard == null || !IsCardPlayable(selectedCard.Value))
                    {
                        var drawn = Board.Draw(CurrentPlayer);
                        OnHaveDraw(CurrentPlayer, drawn, TopState(), Sens);
                        if (CurrentPlayer.Hand.Count > 1 && nbrCardBefore == 1)
                        {
                            RaiseChangeStrategyEvent();
                            OnlyOneCard.Remove(CurrentPlayer);
                        }
                        NextPlayer();
                        continue;
                    }

                    // Jouer la carte
                    Board.DepositStack.Push(selectedCard.Value);
                    CurrentPlayer.RemoveCard(selectedCard.Value);
                    OnTurnPlayed(CurrentPlayer, selectedCard, TopState());

                    // Effets pour ACE, TWO, TEN
                    if (selectedCard.Value.Value != Domain.Enums.CARD_VALUE.JACK)
                    {
                        Effect(selectedCard.Value, CurrentPlayer);
                    }

                    // Cas du Valet : appliquer la couleur choisie
                    if (selectedCard.Value.Value == Domain.Enums.CARD_VALUE.JACK)
                    {
                        var chosen = chosenColor ?? CurrentPlayer.ChooseCardColor().Type;
                        var newColor = new CardColor(chosen);
                        var simulated = new Card(selectedCard.Value.Value, newColor, true);
                        Board.DepositStack.Push(simulated);
                        OnInfo($"Couleur choisie : {simulated.Color}");
                    }

                    // Gestion “une carte restante”
                    if (CurrentPlayer.Hand.Count == 1)
                    {
                        OnOneCard(CurrentPlayer);
                        OnlyOneCard.Add(CurrentPlayer);
                        RaiseChangeStrategyEvent();
                    }

                    if (CurrentPlayer.EmptyEnd())
                    {
                        OnGameWon(CurrentPlayer);
                        end = true;
                    }

                    NextPlayer();
                    continue;
                }

                
                //Le joueur IA joue
                selectedCard = CurrentPlayer.Play(Board);

                if (selectedCard == null)
                {
                    int drawn = Board.Draw(CurrentPlayer);
                    OnHaveDraw(CurrentPlayer, drawn ,TopState(), Sens);

                    if (CurrentPlayer.Hand.Count > 1 && nbrCardBefore == 1)
                    {
                        //Si le joueur a plus d'une carte et qu'avant de jouer il ne lui restait qu'une carte, on retire le mode attaque.
                        RaiseChangeStrategyEvent();
                        OnlyOneCard.Remove(CurrentPlayer);
                    }

                    NextPlayer();
                    continue;
                }

                Board.DepositStack.Push(selectedCard.Value);
                CurrentPlayer.RemoveCard(selectedCard.Value);
                
                OnTurnPlayed(CurrentPlayer, selectedCard, TopState());

                Effect(selectedCard.Value, CurrentPlayer);

                if (CurrentPlayer.Hand.Count == 1)
                {
                    OnOneCard(CurrentPlayer);
                    OnlyOneCard.Add(CurrentPlayer);
                    RaiseChangeStrategyEvent();
                }

                if (CurrentPlayer.EmptyEnd())
                {
                    OnGameWon(CurrentPlayer);
                    end = true;
                }
                
                NextPlayer();
            }

            AfficherScoresEtVainqueur();
        }
        
        /// <summary>
        /// Retourne la première carte de la pile de pioche et la place sur la défausse.
        /// Permet de commencer la partie avec une carte visible (comme au Uno).
        /// </summary>
        public void FlipFirstCard()
        {
            var c = Board.DrawStack.Piocher(); // ou Draw() selon votre implémentation
            if (c.HasValue)
            {
                Board.DepositStack.Push(c.Value);
                OnInfo($"Première carte retournée : {c.Value}");
            }
        }

        private void AfficherScoresEtVainqueur()
        {
            List<Player> players = new List<Player>();
            Player first = CurrentPlayer;

            //Je remplis une liste des joueurs pour pouvoir les ordonner par la suite.
            do
            {
                players.Add(CurrentPlayer);
                NextPlayer();

            } while (CurrentPlayer != first);

            // je calcule les points restants chez tous
            var scores = players.OrderBy(t => t.FinalScore)
                                .ToList();

            OnInfo("");
            OnInfo("===== Tableau des scores (moins = meilleur) =====");
            foreach (var s in scores)
            {
                OnInfo($"{s} : {s.FinalScore} pts");
            }
            
            var gagnant = scores.First();
            OnInfo($">>> Gagnant : {gagnant} <<<");
        }
        
        /// <summary>
        /// Méthode utilitaire qui retourne vrai si la carte est jouable
        /// selon les mêmes règles que l’IA:contentReference[oaicite:0]{index=0}.
        /// </summary>
        public bool IsCardPlayable(Card candidate)
        {
            // Pénalité +2 en cours → seul un 2 est jouable
            if (Board.nbrTwo > 0)
                return candidate.Value == Domain.Enums.CARD_VALUE.TWO;

            var last = TopState();
            if (last == null)
                return true; // défausse vide, tout est jouable

            // Valet toujours autorisé
            if (candidate.Value == Domain.Enums.CARD_VALUE.JACK)
                return true;

            // même couleur ou même valeur
            return candidate.Color.Type == last.Value.Color.Type || candidate.Value == last.Value.Value;
        }
        
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialiser les joueurs de la partie
        /// </summary>
        private void InitialisePlayers(int nbrPlayer)
        {
            Random random = new Random();

            int minimisePlayerIndex = random.Next(1, nbrPlayer);
            Player firstPlayer = new Player(1, minimisePlayerIndex == 1, null, this);
            Player? lastPlayer = firstPlayer;

            for (int x = 2; x <= nbrPlayer; x++) {
                lastPlayer = new Player(x, x == minimisePlayerIndex, lastPlayer, this);
            }

            //On boucle la liste chainée
            lastPlayer.Next = firstPlayer;
            firstPlayer.Last = lastPlayer;
            CurrentPlayer = firstPlayer;
        }
        
        private void SetFirstPlayer()
        {
            //Afin de rendre aléatoire le premier joueur, on génère un nombre aléatoire entre 1 et 10, puis on fait des next player.
            Random random = new Random();
            int randomint = random.Next(10);

            for(int x = 0; x < randomint; x++)
                CurrentPlayer = CurrentPlayer.Next;
        }

        private void NextPlayer()
        {
            if (Sens == 1)
                CurrentPlayer = CurrentPlayer.Next;
            else
                CurrentPlayer = CurrentPlayer.Last;
        }

        private void Effect(Card card, Player currentPlayer)
        {
            switch (card.Value)
            {
                case Domain.Enums.CARD_VALUE.ACE:
                    PassNextPlayer = true;
                    break;
                
                case Domain.Enums.CARD_VALUE.TWO:
                    Board.nbrTwo += 2;
                    OnInfo($"+2 lancé (total à piocher = {Board.nbrTwo}).");
                    break;
                
                case Domain.Enums.CARD_VALUE.TEN:
                    Sens *= -1;
                    //Si on est en mode attaque et qu'il y a un changement de sens, on change les joueurs qui sont en mode attaque.
                    if (OnlyOneCard.Count > 0)
                        RaiseChangeStrategyEvent();
                    OnInfo("Changement de sens!");
                    break;
                
                case Domain.Enums.CARD_VALUE.JACK:
                    // Si le joueur est humain, couleur appliquée depuis Play()
                    if (!currentPlayer.IsHuman)
                    {
                        CardColor newColor = currentPlayer.ChooseCardColor();
                        var simulatedCard = new Card(card.Value, newColor, true);
                        Board.DepositStack.Push(simulatedCard);
                        OnInfo($"Couleur choisie : {simulatedCard.Color}");
                    }
                    break;
            }
        }

        #endregion
    }
}