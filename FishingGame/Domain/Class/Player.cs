using FishingGame.Constantes;
using FishingGame.Domain.Class.Strategies;
using FishingGame.Domain.Enums;
using FishingGame.Domain.Events;
using FishingGame.Domain.Interfaces;
using FishingGame.Domain.Struct;
using FishingGame.Extensions;

namespace FishingGame.Domain.Class
{
    public class Player : Person
    {
        #region Properties
        public List<Card> Hand { get; set; }
        public bool IsMinimise { get; set; }
        public List<IStrategy> Strategies { get; set; }
        public PLAYER_MODE Mode { get; set; }
        public int FinalScore {
            get
            {
                int score = 0;

                foreach (Card card in Hand)
                    score += card.Value.GetScore(); 

                return score;
            }
        }

        public Player Last { get; set; }
        public Player Next { get; set; }
        
        #endregion

        #region Constructeur
        public Player(int id, bool isMinimise, Player? last, Controller.FishingGame sujet)
        {
            //Génération aléatoire des prénoms et nom de famille du joueur.
            Random random = new Random();

            int indexFirstName = random.Next(CONST_PERSON.FIRST_NAME.Length);
            int indexLastName = random.Next(CONST_PERSON.LAST_NAME.Length);

            this.FirstName = CONST_PERSON.FIRST_NAME[indexFirstName];
            this.LastName = CONST_PERSON.LAST_NAME[indexLastName];
            this.Id = id;
            this.IsMinimise = isMinimise;

            if (last != null)
            {
                this.Last = last;

                //On initialise le prochain joueur de celui avant nous.
                last.Next = this;
            }

            this.Hand = new List<Card>();

            this.Strategies = new List<IStrategy>();

            setRegularStrategies();

            //Abonnement à l'event pour changer de stratégie
            sujet.ChangeStrategyHandler += SetAttackMode;

        }
        #endregion

        #region Public Methods
        public Card? Play(GameBoard board){
            Card? selectedCard = null;

            //Si le joueur veut minimiser son score, on réordonne sa main pour mettre les cartes de plus grand score en premier.
            //Comme la stratégie retourne la première carte valide trouvée, il jouera tout le temps celle qui a le plus grand score en premier
            if (IsMinimise)
                Hand = Hand.OrderByDescending(t => t.Score).ToList();

            //On parcours l'ensemble des stratégies dans l'ordre afin de trouver une carte
            //Les stratégies sont placés en ordre de priorité, donc dès qu'on en trouve une, on sort.
            foreach(IStrategy strategy in this.Strategies)
            {
                selectedCard = strategy.ChooseCard(board, this);

                if (selectedCard != null)
                    break;
            }

            return selectedCard;
        }
        public CardColor ChooseCardColor()
        {
            var color = Hand.GroupBy(c => c.Color)
                          .OrderByDescending(g => g.Count())
                          .FirstOrDefault();

            if (color != null)
                return color.Key;

            return new CardColor(COLOR_TYPE.DIAMONDS);
        }
        public bool EmptyEnd() => Hand.Count == 0;
        public void RemoveCard(Card card) => Hand.Remove(card);
        public override string ToString()
        {
            string retour = FirstName + " " + LastName;
            //if (IsMinimise) retour += " (Joueur qui minimise)";
            return retour;
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Initialise les stratégies du joueur en mode normal.
        /// </summary>
        private void setRegularStrategies()
        {
            this.Strategies.Clear();
            this.Strategies.Add(new StrategyBase());
            this.Strategies.Add(new StrategyAttack());

            Mode = PLAYER_MODE.REGULAR;
        }

        /// <summary>
        /// Initialise les stratégies du joueur en mode Attaque.
        /// </summary>
        private void SetAttackStrategies()
        {
            this.Strategies.Clear();
            this.Strategies.Add(new StrategyAttack());
            this.Strategies.Add(new StrategyBase());

            Mode = PLAYER_MODE.ATTACK;
        }

        private void SetAttackMode(object sender,ChangeStrategyEventArgs eventArgs)
        {
            if ((eventArgs.Sens == 1 && Next.Hand.Count == 1) || (eventArgs.Sens == -1 && Last.Hand.Count == 1))
            {
                if (Mode != PLAYER_MODE.ATTACK)
                    SetAttackStrategies();
            }
            else if (Mode != PLAYER_MODE.REGULAR)
                setRegularStrategies();
        }
        #endregion
    }
}