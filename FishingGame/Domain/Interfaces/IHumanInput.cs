using System.Threading;
using System.Threading.Tasks;
using FishingGame.Domain.Struct;
using FishingGame.Domain.Enums;

namespace FishingGame.Domain.Interfaces
{
    /// <summary>
    /// Interface permettant au moteur de jeu de recueillir l'action du joueur humain.
    /// </summary>
    public interface IHumanInput
    {
        /// <summary>
        /// Attend qu'un humain effectue une action. Retourne (null, null) si le joueur décide de piocher.
        /// </summary>
        Task<(Card? card, COLOR_TYPE? chosenColor)> WaitForActionAsync(CancellationToken token);
        /// <summary>Soumet une carte jouée par l'utilisateur.</summary>
        void SubmitPlay(Card card, COLOR_TYPE? chosenColor);
        /// <summary>Signale que l’utilisateur souhaite piocher.</summary>
        void SubmitDraw();
        /// <summary>Réinitialise l'attente en cours (appelée lors d'un reset ou d'un tour passé).</summary>
        void Reset();
    }
}