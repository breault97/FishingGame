using System.ComponentModel;
using FishingGame.Attributes;

namespace FishingGame.Domain.Enums
{
    
    /// <summary>
    /// Enum des valeurs de cartes possibles avec score et description.
    /// Fonctionne avec Extensions/EnumExtensions.cs et Attributes/ScoreAttribute.cs
    /// GetDescription() pour obtenir la valeur et GetScore() pour le score.
    /// </summary>
    public enum CARD_VALUE 
    {
        [Description("A"), Score(11)]
        ACE,
        [Description("2"), Score(2)]
        TWO,
        [Description("3"), Score(3)]
        THREE,
        [Description("4"), Score(4)]
        FOUR,
        [Description("5"), Score(5)]
        FIVE,
        [Description("6"), Score(6)]
        SIX,
        [Description("7"), Score(7)]
        SEVEN,
        [Description("8"), Score(8)]
        EIGHT,
        [Description("9"), Score(9)]
        NINE,
        [Description("10"), Score(10)]
        TEN,
        [Description("J"), Score(2)]
        JACK,
        [Description("Q"), Score(2)]
        QUEEN,
        [Description("K"), Score(2)]
        KING
    }
}