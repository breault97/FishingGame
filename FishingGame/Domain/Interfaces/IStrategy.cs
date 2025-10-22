using FishingGame.Domain.Class;
using FishingGame.Domain.Struct;

namespace FishingGame.Domain.Interfaces
{
    public interface IStrategy
    {
        public Card? ChooseCard(GameBoard board, Player player);

        protected bool ValidateCard(Card card, Card? lastCard, bool two);
    }
}
