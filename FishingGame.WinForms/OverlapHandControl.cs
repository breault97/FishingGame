using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FishingGame.WinForms
{
    /// <summary>
    /// Affiche une main en “bande” avec recouvrement gauche→droite.
    /// VisibleRatio = part visible de la carte suivante (ex. 0.70 ⇒ ~30% recouvert).
    /// </summary>
    public class OverlapHandControl : Control
    {
        // Images à dessiner, dans l’ordre, null accepté (image manquante)
        public List<Image?> Cards { get; } = new();
        
        public int ActiveDotSize { get; set; } = 32;                // pastille verte
        public Point ActiveDotOffset { get; set; } = new Point(8,8); // marge
        public bool ShowActiveDot { get; set; } = false;

        // Partie visible de la carte suivante (0.05..0.95)
        private float _visibleRatio = 0.70f;
        public float VisibleRatio
        {
            get => _visibleRatio;
            set
            {
                var v = Math.Clamp(value, 0.05f, 0.95f);
                if (Math.Abs(v - _visibleRatio) < 0.0001f) return;
                _visibleRatio = v;
                Invalidate();
            }
        }

        // Bas: scale via largeur; Côtés: via hauteur
        public bool UseWidthForScale { get; set; } = false;
        public int MaxCardHeight { get; set; } = 160;
        public int MaxCardWidth  { get; set; } = 120;

        public bool VerticalCenter { get; set; } = true;
        public Color Fill { get; set; } = Color.Transparent;

        // pastille "joueur actif"
        private bool _active;

        // ratio par défaut si aucune image (H/W ≈ 3:2)
        private const double DefaultAspect = 1.5;

        public OverlapHandControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        // -------- API de mise à jour (à appeler sur le thread UI) -----------------

        /// <summary>Remplace la main par ces images et redessine.</summary>
        public void SetCards(IEnumerable<Image?> images)
        {
            Cards.Clear();
            Cards.AddRange(images);
            Invalidate(); // repaint immédiat
        }

        /// <summary>Affiche 'count' dos de cartes et redessine.</summary>
        public void SetBacks(int count, Image back)
        {
            Cards.Clear();
            if (count > 0 && back != null)
            {
                for (int i = 0; i < count; i++) Cards.Add(back);
            }
            Invalidate();
        }

        public void ClearCards()
        {
            Cards.Clear();
            Invalidate();
        }

        public void SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;
            Invalidate();
        }

        // --------------------------------------------------------------------------

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Fill == Color.Transparent && Parent != null)
                e.Graphics.Clear(Parent.BackColor);
            else
                e.Graphics.Clear(Fill);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int availW = ClientSize.Width  - 12;
            int availH = ClientSize.Height - 12;
            if (availW <= 0 || availH <= 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Ratio dynamique depuis la 1re image dispo
            double aspect = DefaultAspect;
            for (int i = 0; i < Cards.Count; i++)
            {
                var img = Cards[i];
                if (img != null && img.Width > 0)
                {
                    aspect = img.Height / (double)img.Width;
                    break;
                }
            }

            // Taille carte + pas (décalage)
            int w, h, step;

            // clamp local assuré (au cas où VisibleRatio serait modifié entre-temps)
            double vis = Math.Clamp(VisibleRatio, 0.05f, 0.95f);

            if (UseWidthForScale)
            {
                // main du bas : largeur prioritaire
                w = Math.Min(MaxCardWidth, Math.Max(48, availW - 8));
                h = (int)Math.Round(w * aspect);

                // si trop haut pour l’aire, on scale
                if (h > availH)
                {
                    double s = availH / (double)h;
                    w = Math.Max(32, (int)Math.Round(w * s));
                    h = availH;
                }

                // Pas = part visible de la carte suivante (ex. 0.70 * w)
                step = Math.Max(6, (int)Math.Round(w * vis));

                // Largeur totale occupée
                int totalW = w + (Cards.Count - 1) * step;

                // Si ça ne rentre pas, scale proportionnellement w & step
                if (totalW > availW)
                {
                    double s = availW / (double)totalW;
                    w = Math.Max(32, (int)Math.Round(w * s));
                    h = Math.Max(32, (int)Math.Round(h * s));
                    step = Math.Max(6, (int)Math.Round(step * s));
                }
            }
            else
            {
                // côtés : hauteur prioritaire
                h = Math.Min(MaxCardHeight, Math.Max(60, availH - 8));
                w = (int)Math.Round(h / aspect);

                step = Math.Max(6, (int)Math.Round(w * vis));
                int totalW = w + (Cards.Count - 1) * step;

                if (totalW > availW)
                {
                    double s = availW / (double)totalW;
                    w = Math.Max(32, (int)Math.Round(w * s));
                    h = Math.Max(32, (int)Math.Round(h * s));
                    step = Math.Max(6, (int)Math.Round(step * s));
                }
            }

            int x = 6;
            int y = VerticalCenter ? (ClientSize.Height - h) / 2
                                   : (ClientSize.Height - h); // collé en bas

            for (int i = 0; i < Cards.Count; i++)
            {
                var img = Cards[i];
                if (img != null)
                    g.DrawImage(img, new Rectangle(x, y, w, h));
                x += step; // recouvrement vient du fait que step < w
            }

            // Pastille verte (joueur actif)
            if (_active && ShowActiveDot)
            {
                using var b = new SolidBrush(Color.LimeGreen);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(ActiveDotOffset, new Size(ActiveDotSize, ActiveDotSize));
                g.FillEllipse(b, r);
            }
        }
    }
}