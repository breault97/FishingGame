using System.Drawing;
using System.IO;

namespace FishingGame.WinForms
{
    internal static class Images
    {
        public static Image CardBack => Load("Assets/Cards/card_back.png");
        public static Image CardEmpty => Load("Assets/Cards/card_empty.png");

        public static Image GetCardImage(object? card)
        {
            if (card == null) return CardEmpty;
            // Adaptez ce mapping à votre Card/enum
            // Exemple simple: card.ToString() => "hearts_07" => fichier "card_hearts_07.png"
            var s = card.ToString()?.ToLowerInvariant() ?? "";
            var file = $"Assets/Cards/card_{s.Replace('♠','s').Replace('♥','h').Replace('♦','d').Replace('♣','c').Replace(' ','_')}.png";
            return File.Exists(file) ? Load(file) : CardEmpty;
        }

        private static Image Load(string path) => Image.FromFile(path);
    }
}