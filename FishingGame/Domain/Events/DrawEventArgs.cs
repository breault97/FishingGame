using FishingGame.Domain.Class;
using FishingGame.Domain.Struct;

namespace FishingGame.Domain.Events;

/// <summary>
///  Event lorsqu'un Joueur pioche une carte
/// </summary>
public class DrawEventArgs : EventArgs
{
    public Player Player { get; init; }
    public int Count { get; init; }
    public Card? TopOfDeposit { get; init; }
    public int Sens { get; init; }
}