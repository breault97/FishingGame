using FishingGame.Domain.Class;
using FishingGame.WinForms.Controls;

namespace FishingGame.WinForms.Screens
{
    public partial class MainWindow
    {
        // Met à jour les trois mains SANS rotation (sièges figés)
        private void UpdateHands()
        {
            if (_game == null) return;

            if (_seatBottom != null) UpdateHandFaces(_bottomHand, _seatBottom);
            if (_seatLeft   != null) UpdateHandBacks(_leftHand,   _seatLeft);
            if (_seatRight  != null) UpdateHandBacks(_rightHand,  _seatRight);

            EnsureBottomHandRoom();
        }

        private void UpdateStacks()
        {
            if (_game == null) return;

            _lblCounts.Text = $"Pioche: {_game.Board.DrawStack.Count} | Dépôt: {_game.Board.DepositStack.Count}";

            _depositStack.Image = _game.Board.DepositStack.Count > 0
                ? LoadCardImage(_game.Board.DepositStack.Peek())
                : null;

            // Pas de réaffectation inutile de _drawStack.Image : l'image 'dos' est déjà chargée au setup
            CenterPiles();
        }
        
        private void UpdatePlayersSlots()
        {
            if (_game == null) return;

            EnsureSeats(); // fixe l'assignation UNE FOIS

            // Active/inactive badge selon joueur courant
            var current = _game.CurrentPlayer;

            _bottomPlayer?.SetActive(_seatBottom == current);
            _leftPlayer?.SetActive(_seatLeft == current);
            _rightPlayer?.SetActive(_seatRight == current);

            // Compteurs
            _bottomPlayer?.SetHandCount(_seatBottom?.Hand.Count ?? 0);
            _leftPlayer?.SetHandCount(_seatLeft?.Hand.Count ?? 0);
            _rightPlayer?.SetHandCount(_seatRight?.Hand.Count ?? 0);
            
            // S'assure que la zone du bas a la place pour le hover 1.25×
            EnsureBottomHandRoom();
        }
        
        private void UpdateHandFaces(OverlapHandControl handCtrl, Player player)
        {
            var imgs = player.Hand.Select(LoadCardImage).Where(img => img != null)!;
            handCtrl.SetCards(imgs!);

            var item = (PlayerItem?)handCtrl.Parent?.Controls
                .OfType<PlayerItem>().FirstOrDefault();
            item?.SetHandCount(player.Hand.Count);
        }

        private void UpdateHandBacks(OverlapHandControl handCtrl, Player player)
        {
            handCtrl.ClearCards();
            if (_cardBack != null)
                handCtrl.SetBacks(player.Hand.Count, _cardBack);

            var item = (PlayerItem?)handCtrl.Parent?.Controls
                .OfType<PlayerItem>().FirstOrDefault();
            item?.SetHandCount(player.Hand.Count);
        }

        private void CenterPiles()
        {
            if (_board.ClientSize.Width <= 0 || _board.ClientSize.Height <= 0) return;

            // Dimensionner le plateau (plus petit que le board)
            var host = _board.ClientSize;
            int w = Math.Max(520, (int)(host.Width  * 0.52));
            int h = Math.Max(300, (int)(host.Height * 0.40)); 

            _table.Size = new Size(w, h);
            _table.Left = (host.Width  - w) / 2;
            _table.Top  = (host.Height - h) / 2;
            _table.BringToFront();

            // Piles au centre du plateau (_table)
            const int PILES_Y_OFFSET = 50; // ↓↓↓ décale les piles vers le bas de 50 px
            int gap = 36;
            int stackW = _depositStack.Width;
            int stackH = _depositStack.Height;

            int cx = _table.ClientSize.Width  / 2;
            int cy = _table.ClientSize.Height / 2 + PILES_Y_OFFSET;

            // Clamp vertical pour rester dans la table
            cy = Math.Max(stackH / 2, Math.Min(cy, _table.ClientSize.Height - stackH / 2));

            _drawStack.Location    = new Point(cx - stackW - gap / 2, cy - stackH / 2);
            _depositStack.Location = new Point(cx + gap / 2,          cy - stackH / 2);

            _drawStack.Visible = true;
            _depositStack.Visible = true;

            _drawStack.BringToFront();
            _depositStack.BringToFront();
            
            // L’overlay reste au-dessus de tout
            AnimHost.BringToFront();
            
            // La PictureBox de la défausse se repeint avec l’overlay centré
            _depositStack.ImageBox.Invalidate();
            
            // >>> Ajuste aussi la zone de la main utilisateur après tout changement de taille
            EnsureBottomHandRoom();
        }
        
        private static List<Player> BuildRing(Player start)
        {
            var ring = new List<Player>();
            var seen = new HashSet<Player>();
            var p = start;
            do { ring.Add(p); seen.Add(p); p = p.Next; } while (p != null && !seen.Contains(p));
            return ring;
        }
        
        // Calcule une hauteur mini pour la main du bas
        private double GetCardAspectRatio()
        {
            // Essaie de prendre un visuel réel si dispo, sinon 1.5 par défaut
            try
            {
                Image? probe = _depositStack.Image ?? _drawStack.Image ?? _cardBack;
                if (probe != null && probe.Width > 0) return probe.Height / (double)probe.Width;
            }
            catch { }
            return 1.5;
        }
        
        // Helper de calcul (nombre de cartes, largeur dispo, ratio, etc.)
        private (int h, int bigH, int hoverPad, int neededHandHeight) ComputeBottomHandMetrics()
        {
            // largeur disponible pour la main (hors padding)
            int availW = _bottomPlayerHost.ClientSize.Width
                         - _bottomPlayerHost.Padding.Horizontal - 12;
            if (availW < 48) availW = 48;

            // nb de cartes (si jeu pas encore démarré, utiliser celles déjà dans le contrôle)
            int count = _seatBottom?.Hand.Count ?? _bottomHand.Cards.Count;
            if (count <= 0) count = 1;

            double aspect = GetCardAspectRatio();           // ~1.5
            double vis    = Math.Clamp(_bottomHand.VisibleRatio, 0.05f, 0.95f);

            // Même logique que OverlapHandControl (UseWidthForScale=true)
            int w = Math.Min(_bottomHand.MaxCardWidth, Math.Max(48, availW - 8));
            int h = (int)Math.Round(w * aspect);

            int step = Math.Max(6, (int)Math.Round(w * vis));
            int totalW = w + (count - 1) * step;
            if (totalW > availW)
            {
                double s = availW / (double)totalW;
                w   = Math.Max(32, (int)Math.Round(w * s));
                h   = Math.Max(32, (int)Math.Round(h * s));
                step = Math.Max(6, (int)Math.Round(step * s));
            }

            // Zoom 25% (=1.25x). pad haut = 12.5% de h.
            int bigH     = (int)Math.Round(h * 1.25);
            int hoverPad = (int)Math.Round(h * 0.125) + 2; // +2px de sécurité pour le bord

            // Hauteur que doit faire le contrôle de la main (assez pour le zoom complet)
            int neededHandHeight = Math.Max(bigH, h + hoverPad) + 6; // +6 marge bas

            return (h, bigH, hoverPad, neededHandHeight);
        }
        
        private void EnsureBottomHandRoom()
        {
            if (_bottomPlayerHost.Width <= 0) return;

            var (_, _, _, neededHandHeight) = ComputeBottomHandMetrics();

            // S’assure que le contrôle _bottomHand a la bonne hauteur
            if (_bottomHand.Height != neededHandHeight)
                _bottomHand.Height = neededHandHeight; // Dock=Bottom → la zone augmente réellement

            // S’assure que le panneau hôte est suffisant (entête + main + marge)
            int headerH = _bottomPlayer?.Height ?? 64;
            int margin  = 20;
            int desired = headerH + neededHandHeight + margin;

            if (_bottomPlayerHost.Height != desired)
                _bottomPlayerHost.Height = desired;
        }
    }
}
