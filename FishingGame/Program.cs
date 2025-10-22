using System.Text;
using FishingGame.Logger;

namespace FishingGame
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // important pour les symbols ♥ ♦ ♣ ♠
            Console.OutputEncoding = Encoding.UTF8;

            //Déterminer le nombre de joueur ici, mettre à 0 pour demander à l'utilisateur.
            int nbrJoueur = 3;

            while (nbrJoueur < 1)
            {
                nbrJoueur = AskNbrPlayer();
            }

            var game = new Controller.FishingGame(nbrJoueur);
            //Assignation de game pour log les envents par l'Observateur
            var logger = new ConsoleLogger();
            logger.Wire(game);

            // De 5 à 8 cartes par joueurs
            game.Begin(8);
   
            Console.WriteLine();

            await game.Play();

            Console.WriteLine();
        }
        
        /// <summary>
        /// Demande à l'utilisateur le nombre de joueurs de la partie.
        /// </summary>
        /// <returns>Le nombre de joueur choisi.</returns>
        private static int AskNbrPlayer()
        {
            int nbrJoueur = 0;

            Console.WriteLine("Spécifiez le nombre de joueurs (entre 2 et 4).");
            string? strNbrJoueur = Console.ReadLine();

            if (int.TryParse(strNbrJoueur, out int value))
            {
                nbrJoueur = Convert.ToInt32(strNbrJoueur);

                if (nbrJoueur < 2 || nbrJoueur > 4)
                {
                    //Redéfinition du nombre de jouer à 0 pour redemander à l'utilisateur
                    nbrJoueur = 0;
                    Console.WriteLine("Vous devez entrer un nombre entier entre 2 et 4 inclusivement.");
                }  
            }
            else
                Console.WriteLine("La valeur " + strNbrJoueur + " n'est pas un entier.");

            return nbrJoueur;
        }
    }
}