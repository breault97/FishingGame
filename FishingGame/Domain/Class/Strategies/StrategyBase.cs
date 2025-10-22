using FishingGame.Domain.Enums;
using FishingGame.Domain.Interfaces;
using FishingGame.Domain.Struct;

namespace FishingGame.Domain.Class.Strategies
{
    public class StrategyBase : IStrategy
    {
        public Card? ChooseCard(GameBoard board, Player player)
        {
            Card? lastCard = null;
            Card? selectedCard = null;

            if (board.DepositStack.Count > 0)
                lastCard = board.DepositStack.Peek();

            if (lastCard == null)
            {
                //Si c'est la première carte de la partie, on joue n'importe quel carte
                selectedCard = player.Hand.FirstOrDefault();
            }
            else
            {
                foreach (Card card in player.Hand)
                {
                    if (ValidateCard(card, lastCard, board.nbrTwo > 0))
                        selectedCard = card;
                }
            }


            return selectedCard;
        }

        public bool ValidateCard(Card card, Card? lastCard, bool two)
        {
            if (two)
                return card.Value == CARD_VALUE.TWO; // en phase punition, seul un 2 passe

            if (card.IsAttack)       // On ne veut pas jouer les cartes d'attaque tout de suite, on veut les garder en cas d'attaque à faire.
                return false;

            if (lastCard == null)
                return true;

            return card.Color.Type == lastCard.Value.Color.Type || card.Value == lastCard.Value.Value;
        }
    }
}
