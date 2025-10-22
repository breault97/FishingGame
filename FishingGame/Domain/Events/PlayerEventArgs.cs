using FishingGame.Domain.Class;

namespace FishingGame.Domain.Events;

public class PlayerEventArgs : EventArgs
{
    public Player Player { get; init; }
}