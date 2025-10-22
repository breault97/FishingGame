using FishingGame.Domain.Enums;
using FishingGame.Domain.Struct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishingGame.Domain.Class
{
    public class PlayerRegular : Player
    {
        #region Constructors
        public PlayerRegular(int id) : base(id) { }
        #endregion

        #region Public Method
        public override Card? Play(GameBoard board)
        {
            Card? lastCard = null;
            Card? selectedCard = null;

            if (board.DepositStack.Count > 0)
                lastCard = board.DepositStack.Peek();

            if (lastCard == null)
            {
                //Si c'est la première carte de la partie, on joue n'importe quel carte
                selectedCard = Hand.FirstOrDefault();
            }
            else
            {
                foreach (Card card in Hand)
                {
                    if (ValidateCard(card, lastCard, board.nbrTwo > 0))
                    {
                        selectedCard = card;
                    }
                }
                //if (board.nbrTwo > 0)
                //{
                //    foreach (Card card in Hand)
                //    {
                //        if (ValidateCard(card, lastCard, board.nbrTwo > 0))
                //        {
                //            selectedCard = card;
                //        }
                //    }
                //}
                //else
                //{
                //    foreach (Card card in Hand)
                //    {
                //        if (ValidateCard(card, lastCard, board.nbrTwo > 0))
                //        {
                //            selectedCard = card;
                //        }
                //    }
                //}
            }


            return selectedCard;
        }

        public override CardColor ChooseCardColor()
        {
            var color = Hand.GroupBy(c => c.Color)
                          .OrderByDescending(g => g.Count())
                          .FirstOrDefault();

            if (color != null)
            {
                return color.Key;
            }

            return new CardColor(COLOR_TYPE.DIAMONDS);

            //TODO trouver un moyen de faire avec la manière qu'on a monté les coleurs.
            // sinon je renvoie une couleur au hasard
            //Array values = Enum.GetValues(typeof(COLOR_TYPE));
            //Random random = new Random();

            //return new CardColor {values[random.Next(values.Length)] };
        }
        #endregion
    }
}
