namespace FishingGame.Domain.Events;

/// <summary>
///  Event pour les messages informatifs
/// </summary>
public class InfoEventArgs : EventArgs
{
    public string Message { get; init; }
}