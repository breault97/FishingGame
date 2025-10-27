using System.Drawing.Drawing2D;

namespace FishingGame.WinForms.Controls
{
    // Conteneur avec ombre douce qui héberge une PictureBox
    public class ShadowStack : DoubleBufferedPanel
    {
        public PictureBox ImageBox { get; }

        // Repaint immédiat quand l'image change
        public Image? Image
        {
            get => ImageBox.Image;
            set
            {
                if (!ReferenceEquals(ImageBox.Image, value))
                    ImageBox.Image = value;
                Invalidate();
            }
        }
        
        private Size _cardSize = new Size(90, 130);

        public ShadowStack()
        {
            // Transparence et anti-flicker
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            Padding   = new Padding(14);       // bord pour peindre l’ombre
            BackColor = Color.Transparent;

            ImageBox = new PictureBox
            {
                BackColor = Color.Transparent,
                SizeMode  = PictureBoxSizeMode.Zoom,
                Dock      = DockStyle.Fill,
                Margin    = new Padding(0),
                InitialImage  = null,   // << empêche l’icône "chargement"
                ErrorImage    = null    // << empêche la croix rouge WinForms
            };
            Controls.Add(ImageBox);

            UpdateMinimumSize();
        }

        // Pas de flood-fill : vrai comportement transparent (le parent peint)
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Efface avec la couleur du parent (table)
            var bg = Parent?.BackColor ?? SystemColors.Control;
            e.Graphics.Clear(bg);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;
            rect.Inflate(-6, -6);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (GraphicsPath path = RoundedRect(rect, 10))
            using (PathGradientBrush shadow = new PathGradientBrush(path))
            {
                shadow.CenterColor    = Color.FromArgb(70, 0, 0, 0);
                shadow.SurroundColors = new[] { Color.FromArgb(0, 0, 0, 0) };
                shadow.CenterPoint    = new PointF(rect.Left + rect.Width / 2f,
                                                   rect.Top  + rect.Height / 2f);

                g.FillPath(shadow, path);
            }
        }

        private void UpdateMinimumSize()
        {
            MinimumSize = new Size(
                _cardSize.Width  + Padding.Horizontal,
                _cardSize.Height + Padding.Vertical
            );
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(r.X,         r.Y,          d, d, 180, 90);
            gp.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            gp.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            gp.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            gp.CloseFigure();
            return gp;
        }
    }
}