using FishingGame.Constantes;
using FishingGame.Domain.Enums;
using FishingGame.Domain.Struct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FishingGame.Domain.Class
{
    public class PlayerMinimise : Player
    {
        #region Constructors
        public PlayerMinimise(int id) : base(id) { }
        #endregion

        #region Méthodes publiques
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
                        //PlayerMinimise prend la carte avec le plus gros Score
                        if (!selectedCard.HasValue || card.Score > selectedCard.Value.Score) 
                            selectedCard = card;
                    }
                }
            }


            return selectedCard;
        }

        public override CardColor ChooseCardColor()
        {
            //Temporairement fait la même chose que le joueur régulier

            var color = Hand.GroupBy(c => c.Color)
                          .OrderByDescending(g => g.Count())
                          .FirstOrDefault();

            if (color != null)
            {
                return color.Key;
            }

            return new CardColor(COLOR_TYPE.DIAMONDS);
        }
        #endregion
    }
}
