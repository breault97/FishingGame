using FishingGame.Domain.Struct;

namespace FishingGame.Domain.Class
{
    public class DrawStack : Stack<Card>
    {
        #region Properties
        private readonly Random _rng = new Random();
        #endregion

        #region Constructors
        public DrawStack(CardPair cardPair) {
            Shuffle(cardPair.Cards);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Mélange une collection de carte et les ajoute à la pioche.
        /// </summary>
        /// <param name="cardPair"></param>
        public void Shuffle(IEnumerable<Card> cardPair)
        {
            List<Card> CardList = cardPair.ToList();

            //S'il reste des cartes dans la pioche, on les conserve pour les mettres par dessus.
            Stack<Card> Buffer = new Stack<Card>();

            while (Count > 0)
                Buffer.Push(Pop());

            //On vide la pioche pour remettre les cartes
            Clear();

            // ici je mélange “à la main” 
            for (int i = CardList.Count - 1; i >= 0; i--)
            {
                int k = _rng.Next(i + 1);
                (CardList[i], CardList[k]) = (CardList[k], CardList[i]);
            }

            foreach (var c in CardList) 
                this.Push(c);

            //On rajoute les cartes du buffer par dessus.
            while (Buffer.Count > 0)
                Push(Buffer.Pop());
        }

        public Card? Piocher()
        {
            if (Count < 1) return null;
            return Pop();
        }

        #endregion
    }
}