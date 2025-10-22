using FishingGame.Domain.Enums;
using FishingGame.Domain.Interfaces;
using FishingGame.Domain.Struct;

namespace FishingGame.Domain.Class.Strategies
{
    public class StrategyAttack : IStrategy
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
            if (!card.IsAttack)       //On veut prioriser les cartes d'attaque
                return false;

            if (lastCard == null)
                return true;

            if (two)
                return card.Value == CARD_VALUE.TWO; // en phase punition, seul un 2 passe

            if (card.Value == CARD_VALUE.JACK) //le valet peut être tout le temps mis
                return true;
            return card.Color.Type == lastCard.Value.Color.Type || card.Value == lastCard.Value.Value;
        }
    }
}
