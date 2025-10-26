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

            if (_seatBottom != null) UpdateHand(_bottomHand, _seatBottom);
            if (_seatLeft   != null) UpdateHand(_leftHand,   _seatLeft);
            if (_seatRight  != null) UpdateHand(_rightHand,  _seatRight);
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
        }
        
        // Côtés = empilement horizontal décalé (~50%) SANS bordure ni scroll.
        private void UpdateHand(OverlapHandControl handCtrl, Domain.Class.Player player)
        {
            var imgs = player.Hand.Select(LoadCardImage).Where(img => img != null)!;
            // reconstruit à chaque fois – pas de cache figé
            handCtrl.SetCards(imgs!);

            // MAJ du compteur dans le PlayerItem
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
        }
        
        private static List<Player> BuildRing(Player start)
        {
            var ring = new List<Player>();
            var seen = new HashSet<Player>();
            var p = start;
            do { ring.Add(p); seen.Add(p); p = p.Next; } while (p != null && !seen.Contains(p));
            return ring;
        }
    }
}
