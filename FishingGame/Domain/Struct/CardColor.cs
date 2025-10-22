using FishingGame.Domain.Enums;
using FishingGame.Extensions;

namespace FishingGame.Domain.Struct
{
    public struct CardColor
    {
        public COLOR_TYPE Type { get; }
        
        public CardColor(COLOR_TYPE type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return Type.GetDescription();
        }
    }
}