using FishingGame.Domain.Events;
using FishingGame.Domain.Interfaces;

namespace FishingGame.Logger;

/// <summary>
/// Utilisation du pattern observateur pour log les events dans la Console
/// </summary>
public sealed class ConsoleLogger
{
    private EventHandler<TurnEventArgs>? _onTurn;
    private EventHandler<DrawEventArgs>? _onDraw;
    private EventHandler<InfoEventArgs>? _onDeckAlmostEmpty;
    private EventHandler<InfoEventArgs>? _onInfo;
    private EventHandler<PlayerEventArgs>? _onOneCard;
    private EventHandler<PlayerEventArgs>? _onGameWon;
    
    /// <summary>
    /// Abonnement des observateurs
    /// </summary>
    public void Wire(IFishingGameObserver game)
    {
        _onTurn ??= (s,e) =>
        {
            var player = e.Player;
            var played = e.PlayedCard?.ToString() ?? " a pioché ";
            var top = e.TopOfDeposit?.ToString() ?? " vide ";
            var sens = e.Sens == 1 ? "horaire" : "anti-horaire";
            Console.WriteLine(
                $"{player} a joué {played} - Carte Actuelle: {top} - Sens: {sens}");
        };
        _onDraw ??= (s, e) =>
        {
            var sens = e.Sens == 1 ? "horaire" : "anti-horaire";

            Console.WriteLine($"{e.Player} pioche {e.Count} cartes(s) -  Carte Actuelle: {e.TopOfDeposit} - Sens: {sens}");
        };
            
        _onDeckAlmostEmpty ??= (s, e) =>
            Console.WriteLine(e.Message);
        _onInfo ??= (s, e) =>
            Console.WriteLine(e.Message);
        _onOneCard ??= (s, e) =>
            Console.WriteLine($"{e.Player} n'a plus qu'une carte !");
        _onGameWon ??= (s, e) =>
            Console.WriteLine($"-----{e.Player} a gagné la partie !-----");
        
        game.TurnPlayed += _onTurn;
        game.HaveDrawCards += _onDraw;
        game.DeckAlmostEmpty += _onDeckAlmostEmpty;
        game.Info += _onInfo;
        game.OneCard += _onOneCard;
        game.GameWon += _onGameWon;
    }

    /// <summary>
    /// Désabonnement des observateurs
    /// Utile si plusieurs games dans la même exécution
    /// </summary>
    public void UnWire(IFishingGameObserver game)
    {
        if (_onTurn != null) game.TurnPlayed -= _onTurn;
        if (_onDraw != null) game.HaveDrawCards -= _onDraw;
        if (_onDeckAlmostEmpty != null) game.DeckAlmostEmpty -= _onDeckAlmostEmpty;
        if (_onInfo != null) game.Info -= _onInfo;
        if (_onOneCard != null) game.OneCard -= _onOneCard;
        if (_onGameWon != null) game.GameWon -= _onGameWon;
    }
}