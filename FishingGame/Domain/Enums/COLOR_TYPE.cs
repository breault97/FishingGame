using System.ComponentModel;

namespace FishingGame.Domain.Enums;

/// <summary>
/// Enum des types de couleurs possible avec les symbols en description
/// Description fonctionne avec System.ComponentModel et GetDescription()
/// IMPORTANT: Insérer "Console.OutputEncoding = Encoding.UTF8;" avant d'afficher les symbols dans la console
/// </summary>
public enum COLOR_TYPE
{
    [Description("♣")]
    CLUBS,
    [Description("♦")]
    DIAMONDS,
    [Description("♥")]
    HEARTS,
    [Description("♠")]
    SPADES
}