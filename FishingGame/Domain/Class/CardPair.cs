using FishingGame.Domain.Struct;
using FishingGame.Domain.Enums;

namespace FishingGame.Domain.Class
{
    public class CardPair
    {
        #region Properties
        public List<Card> Cards { get; }
        #endregion

        #region Constructeur
        public CardPair()
        {
            Cards = new List<Card>();
            foreach (CARD_VALUE value in Enum.GetValues(typeof(CARD_VALUE)))
            {
                foreach (COLOR_TYPE color in Enum.GetValues(typeof(COLOR_TYPE)))
                {
                    Cards.Add(new Card(value, new CardColor(color)));
                }
            }
        }
        #endregion
    }
}