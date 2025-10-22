using FishingGame.Domain.Struct;

namespace FishingGame.Domain.Class
{
    public class GameBoard
    {
        #region Properties
        public event Action? DrawStackRecycled;
        public CardPair CardPair { get; set; }
        public DrawStack DrawStack { get; set; }
        public DepositStack DepositStack { get; set; }
        public int DrawPileCount => DrawStack.Count;
        public int DepositPileCount => DepositStack.Count; 
        public int nbrTwo { get; set; }
        #endregion

        #region Constructors
        public GameBoard() { 
            CardPair = new CardPair();

            DrawStack = new DrawStack(CardPair);
            DepositStack = new DepositStack();

            nbrTwo = 0;
        }
        #endregion

        #region Public Methods
        public void DistributBeginingCards(Player firstPlayer, int nbrCard)
        {
           if (nbrCard < 5 || nbrCard > 8)
                throw new Exception("Vous devez distribuer entre 5 et 8 cartes pour commencer.");

           Player player = firstPlayer;

           for (int r = 0; r < nbrCard; r++)
           {
               do
               {
                   Card? card = DrawStack.Piocher();

                   if (card.HasValue)
                       player.Hand.Add(card.Value);

                   player = player.Next;
               } while (player != firstPlayer);
                
           }
        }
        
        /// <summary>
        /// Piocher des carte pour un joueur
        /// </summary>
        /// <param name="player">Le joueur qui doit piocher des cartes.</param>
        public int Draw(Player player)
        {
            int toTake = Math.Max(1, nbrTwo);
            int taken = 0;

            for(int i = 0; i < toTake; i++)
            {
                if (DrawStack.Count == 0)
                {
                    DrawStackRecycled?.Invoke();
                    DrawStack.Shuffle(DepositStack.TakeAllExceptCurrent());
                }

                var c = DrawStack.Piocher();
                if (c.HasValue)
                {
                    player.Hand.Add(c.Value);
                    taken++;
                }
                else break;
            }

            //Réinitialisation du nombre de carte à piocher à cause des punitions.
            nbrTwo = 0;
            return taken;
        }
        #endregion
    }
}