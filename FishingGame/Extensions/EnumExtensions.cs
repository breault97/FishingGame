using System.ComponentModel;
using FishingGame.Attributes;

namespace FishingGame.Extensions
{
    /// <summary>
    /// Extensions pour Enum CARD_VALUE
    /// </summary>
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field!, typeof(DescriptionAttribute));
            return attribute?.Description ?? value.ToString();
        }
    
        public static int GetScore(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (ScoreAttribute?)Attribute.GetCustomAttribute(field!, typeof(ScoreAttribute));
            return attribute?.Score ?? 0;
        }
    }
}