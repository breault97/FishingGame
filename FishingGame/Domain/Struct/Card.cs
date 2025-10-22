using FishingGame.Domain.Enums;
using FishingGame.Extensions;

namespace FishingGame.Domain.Struct
{
    public struct Card
    {
        public CARD_VALUE Value { get; }
        public CardColor Color { get; }
        public int Score { get; }

        public bool IsAttack => Value is CARD_VALUE.ACE or CARD_VALUE.JACK or CARD_VALUE.TWO or CARD_VALUE.TEN;

        public bool IsVirtual { get; set; }

        public Card(CARD_VALUE value, CardColor color, bool isVirtual = false)
        {
            this.Value = value;
            this.Color = color;
            this.Score = value.GetScore();
            this.IsVirtual = isVirtual;
        }

        public override string ToString()
        {
            return $"{Value.GetDescription()}{Color.ToString()}";
        }
    }
}