using FishingGame.Domain.Class;
using FishingGame.Domain.Struct;

namespace FishingGame.Domain.Events;

/// <summary>
///  Event contenant les informations d'un tour 
/// </summary>
public class TurnEventArgs : EventArgs
{
    public Player Player { get; init; }
    public Card? PlayedCard { get; init; }
    public Card? TopOfDeposit { get; init; }
    public int Sens { get; init; }
}