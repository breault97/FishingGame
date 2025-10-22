using FishingGame.Domain.Events;

namespace FishingGame.Domain.Interfaces;

public interface IFishingGameObserver
{
    public event EventHandler<TurnEventArgs>? TurnPlayed;
    public event EventHandler<DrawEventArgs>? HaveDrawCards;
    public event EventHandler<InfoEventArgs>? DeckAlmostEmpty;
    public event EventHandler<InfoEventArgs>? Info;
    public event EventHandler<PlayerEventArgs>? OneCard;
    public event EventHandler<PlayerEventArgs>? GameWon;
}