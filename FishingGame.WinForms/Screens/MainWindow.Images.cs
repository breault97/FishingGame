using FishingGame.Domain.Enums;
using FishingGame.Domain.Struct;
using FishingGame.WinForms.Services;

// Card

namespace FishingGame.WinForms.Screens
{
    public partial class MainWindow
    {
        // --------------------------------------------------------------------
        // Cache d'images : on ne Dispose JAMAIS ce que l’UI pourrait référencer.
        // On vit avec pour toute la durée du process (taille très raisonnable).
        // --------------------------------------------------------------------
        private static readonly Dictionary<string, (Bitmap bmp, DateTime mtime)> _imgCache =
            new(StringComparer.OrdinalIgnoreCase);
        
        private Image? _cardBack;
        private string _cardBackKey = "black"; // défaut
        
        private static Bitmap? LoadBitmapUnlocked(string path)
        {
            try
            {
                using var fs  = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var img = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: true);
                return new Bitmap(img); // clone = pas de lock disque
            }
            catch
            {
                return null;
            }
        }
        
        // Retourne une instance valide; recharge si le fichier a changé OU si l'instance en cache est devenue invalide.
        private static Bitmap? GetCachedBitmap(string path)
        {
            if (!File.Exists(path)) return null;

            var mtime = File.GetLastWriteTimeUtc(path);
            if (_imgCache.TryGetValue(path, out var entry))
            {
                try
                {
                    // Teste si le Bitmap n'est pas disposé
                    _ = entry.bmp.Width;
                    if (entry.mtime == mtime) return entry.bmp;
                }
                catch
                {
                    _imgCache.Remove(path); // instance devenue invalide
                }
            }

            var bmp = LoadBitmapUnlocked(path);
            if (bmp == null) return null;
            _imgCache[path] = (bmp, mtime);
            return bmp;
        }
        
        // Charge l'image d'une carte via CardView (mapping card_<suit>_<rank>.png)
        private Image? LoadCardImage(Card c)
        {
            string suit = (c.Color.Type /* ou c.Color.ColorType */) switch
            {
                COLOR_TYPE.CLUBS    => "clubs",
                COLOR_TYPE.DIAMONDS => "diamonds",
                COLOR_TYPE.HEARTS   => "hearts",
                COLOR_TYPE.SPADES   => "spades",
                _                   => "spades"
            };

            string rank = c.Value switch
            {
                CARD_VALUE.TWO   => "02",
                CARD_VALUE.THREE => "03",
                CARD_VALUE.FOUR  => "04",
                CARD_VALUE.FIVE  => "05",
                CARD_VALUE.SIX   => "06",
                CARD_VALUE.SEVEN => "07",
                CARD_VALUE.EIGHT => "08",
                CARD_VALUE.NINE  => "09",
                CARD_VALUE.TEN   => "10",
                CARD_VALUE.JACK  => "J",
                CARD_VALUE.QUEEN => "Q",
                CARD_VALUE.KING  => "K",
                CARD_VALUE.ACE   => "A",
                _                => "A"
            };

            var file = Path.Combine(AppContext.BaseDirectory, "Assets", "Cards",
                $"card_{suit}_{rank}.png");
            return GetCachedBitmap(file) ?? Images.CardEmpty;
        }

        /// <summary>
        /// Retourne les backs disponibles : key -> fullPath
        /// (key = blue, red, purple, green, orange, black, …)
        /// </summary>
        private Dictionary<string, string> GetAvailableCardBacks()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var dir = Path.Combine(AppContext.BaseDirectory, "Assets", "Cards");
            if (!Directory.Exists(dir)) return dict;

            foreach (var full in Directory.EnumerateFiles(dir, "card_back_*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(full); // ex: card_back_blue
                var key = name.Replace("card_back_", "");
                if (!string.Equals(key, "empty", StringComparison.OrdinalIgnoreCase))
                    dict[key] = full;
            }
            
            // rétro-compat : card_back.png (ancien nom)
            var legacy = Path.Combine(dir, "card_back.png");
            if (File.Exists(legacy) && !dict.ContainsKey("legacy"))
                dict["legacy"] = legacy;
            
            return dict;
        }

        /// <summary>
        /// Charge le dos courant (_cardBackKey) et met à jour la pioche.
        /// </summary>
        /// <param name="preserveDeposit">
        /// true = ne pas remettre la défausse à null (utile lors d'un simple changement de dos).
        /// </param>
        private void LoadBackImage(bool preserveDeposit = false)
        {
            var options = GetAvailableCardBacks();

            if (!options.ContainsKey(_cardBackKey))
                _cardBackKey = options.ContainsKey("blue") ? "blue" : options.Keys.FirstOrDefault() ?? "blue";

            var path = options.TryGetValue(_cardBackKey, out var p)
                ? p
                : Path.Combine(AppContext.BaseDirectory, "Assets", "Cards", "card_back_blue.png");

            // ⚠️ Ne PAS disposer l'ancien _cardBack : il peut être encore attaché à une PictureBox.
            _cardBack = GetCachedBitmap(path);

            // Affectations sécurisées (Image peut être null, ce qui est OK pour PictureBox)
            _drawStack.Image = _cardBack;
            if (!preserveDeposit) _depositStack.Image = null;

            // Rafraîchir les mains adverses (dos)
            RenderHandsUI();
        }

        /// <summary>Change le dos via sa clé (ex: "red", "blue").</summary>
        private void ApplyCardBack(string key)
        {
            _cardBackKey = key;
            LoadBackImage(preserveDeposit: true);
            UpdateBackButtonCaption();
        }

        // Appel no-op conservé pour compatibilité (on ne dispose plus le cache)
        private static void InvalidateImageCache() { /* no-op */ }

    }
}