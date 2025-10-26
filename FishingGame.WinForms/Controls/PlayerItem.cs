using System.Drawing.Drawing2D;
using FishingGame.Domain.Class;

namespace FishingGame.WinForms.Controls
{
    public class PlayerItem : Panel
    {
        private readonly PictureBox _avatar = new()
        {
            Width = 50,
            Height = 50,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        private readonly Label _name  = new() { AutoSize = true, ForeColor = Color.White };
        private readonly Label _count = new() { AutoSize = true, ForeColor = Color.Silver };

        // Petit badge rond (pastille verte) en bas-droite <<<<<
        private readonly Panel _badge = new()
        {
            Width = 30,
            Height = 30,
            AutoSize = false,
            BackColor = Color.Gray,
        };

        private readonly ToolTip _tip = new() { ShowAlways = true };

        public Player Player { get; private set; }

        public PlayerItem(Player p, string avatarPath)
        {
            Player = p;
            Height = 56;
            Padding = new Padding(8);
            BackColor = Color.FromArgb(30, 30, 35);
            DoubleBuffered = true; // anti-flicker

            // Avatar: charger SANS verrouiller le fichier sur le disque
            if (!string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath))
            {
                try
                {
                    using var fs = new FileStream(avatarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    _avatar.Image = new Bitmap(fs); // copie en mémoire → pas de lock
                }
                catch { /* ignore si image invalide */ }
            }

            _name.Text  = p.ToString();
            _count.Text = "—";

            // Colonne nom + compteur
            var nameStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = Color.Transparent
            };
            nameStack.Controls.Add(_name);
            nameStack.Controls.Add(_count);

            // Ligne: avatar | espace | (nom, compteur)
            var row = new FlowLayoutPanel
            {
                Dock         = DockStyle.Fill,
                AutoSize     = false,
                BackColor    = Color.Transparent,
                WrapContents = false
            };
            row.Controls.Add(_avatar);
            row.Controls.Add(new Panel { Width = 8, Height = 1 });
            row.Controls.Add(nameStack);

            Controls.Add(row);
            Controls.Add(_badge);          // badge en superposition
            _badge.BringToFront();
            _tip.SetToolTip(_badge, "Joueur actif");

            // Positionner et rendre le badge circulaire
            SizeChanged            += (_, __) => UpdateBadgeGeometry();
            _badge.SizeChanged     += (_, __) => UpdateBadgeGeometry();
            UpdateBadgeGeometry();
        }

        private void UpdateBadgeGeometry()
        {
            // ancrage DROITE + centre vertical (+ marge)
            const int margin = 10;
            _badge.Left = Width  - _badge.Width  - margin;
            _badge.Top  = (Height - _badge.Height) / 2;

            using var gp = new GraphicsPath();
            gp.AddEllipse(0, 0, _badge.Width - 1, _badge.Height - 1);
            _badge.Region = new Region(gp);
            _badge.Invalidate();
        }

        /// <summary>Active/désactive le badge et change légèrement le fond.</summary>
        public void SetActive(bool isActive)
        {
            // Toujours visible, on change juste la couleur
            _badge.Visible   = true;
            _badge.BackColor = isActive ? Color.LimeGreen : Color.Gray;
            BackColor        = isActive ? Color.FromArgb(45, 45, 55) : Color.FromArgb(30, 30, 35);
        }

        public void SetHandCount(int n)
        {
            _count.Text = n == 1 ? "1 carte" : $"{n} cartes";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _avatar.Image?.Dispose();
                _tip?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}