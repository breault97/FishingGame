using FishingGame.Domain.Struct;

namespace FishingGame.Domain.Class
{
    public class DepositStack : Stack<Card>
    {
        #region Properties
        #endregion

        #region Public Methods
        public IEnumerable<Card> TakeAllExceptCurrent()
        {
            // je garde la carte du dessus, je renvoie le reste pour la pioche
            if (Count <= 1) 
                return Enumerable.Empty<Card>();

            var top = Pop();
            var cards = this.ToList();

            Clear();
            Push(top);

            //On ne retourne pas les cartes virutelles
            return cards.Where(x => !x.IsVirtual);
        }
        #endregion
    }
}