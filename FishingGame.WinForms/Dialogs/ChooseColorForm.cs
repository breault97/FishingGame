using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using FishingGame.Domain.Enums;

namespace FishingGame.WinForms.Dialogs
{
    /// <summary>
    /// Dialogue permettant de choisir la couleur d'un Valet.  Il s'intègre au thème sombre de l'application
    /// et propose quatre icônes centrées (Trèfle, Carreau, Cœur, Pique) qui s'agrandissent en restant
    /// centrées au survol. La fenêtre possède sa propre barre de titre et un bouton de fermeture rond.
    /// </summary>
    public sealed class ChooseColorForm : Form
    {
        public COLOR_TYPE? ChosenColor { get; private set; }

        // Taille par défaut des icônes (64×64).
        private readonly Size _iconSize = new(64, 64);

        public ChooseColorForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterParent;
            // Fenêtre plus compacte : 600px de large
            ClientSize      = new Size(600, 200);
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(34, 34, 40);
            ForeColor       = Color.Gainsboro;
            Font            = new Font("Segoe UI", 10f);

            // ==== Barre de titre ====
            var titleBar = new Panel {
                Dock      = DockStyle.Top,
                Height    = 40,
                BackColor = Color.FromArgb(35, 35, 42),
                Padding   = new Padding(12, 0, 12, 0)
            };
            Controls.Add(titleBar);

            var titleLabel = new Label {
                Text      = "Choisir une couleur",
                AutoSize  = true,
                ForeColor = Color.White,
                Font      = new Font(Font.FontFamily, 11, FontStyle.Bold),
                BackColor = Color.Transparent,
                Dock      = DockStyle.Left,
                Padding   = new Padding(0, 12, 0, 0)
            };
            titleBar.Controls.Add(titleLabel);

            var btnClose = new Button {
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(200, 40, 50),
                ForeColor = Color.White,
                Size      = new Size(30, 30),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                Margin    = new Padding(0)
            };
            btnClose.FlatAppearance.BorderSize         = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 60, 70);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(170, 30, 40);
            btnClose.Click += (_, __) => Close();
            try {
                var closePng = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "close.png");
                if (File.Exists(closePng)) {
                    using var raw = Image.FromFile(closePng);
                    btnClose.Image = new Bitmap(raw, new Size(16, 16));
                    btnClose.Text  = "";
                } else {
                    btnClose.Text = "✕";
                    btnClose.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
                }
            } catch {
                btnClose.Text = "✕";
                btnClose.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
            }
            btnClose.Paint += (s, e) => {
                var b = (Button)s!;
                using var gp = new GraphicsPath();
                gp.AddEllipse(0, 0, b.Width, b.Height);
                b.Region = new Region(gp);
            };
            void LayoutClose() {
                btnClose.Left = titleBar.ClientSize.Width - btnClose.Width - titleBar.Padding.Right;
                btnClose.Top  = Math.Max(0, (titleBar.Height - btnClose.Height) / 2);
            }
            titleBar.Controls.Add(btnClose);
            titleBar.Resize += (_, __) => LayoutClose();
            LayoutClose();

            // ==== Conteneur pour les icônes ====
            var iconsPanel = new FlowLayoutPanel {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = BackColor,
                Margin        = new Padding(0),
                Padding       = new Padding(0)
            };
            Controls.Add(iconsPanel);

            var files = new Dictionary<COLOR_TYPE, string> {
                { COLOR_TYPE.CLUBS,    "clubs.png"    },
                { COLOR_TYPE.DIAMONDS, "diamonds.png" },
                { COLOR_TYPE.HEARTS,   "hearts.png"   },
                { COLOR_TYPE.SPADES,   "spade.png"    }
            };

            foreach (var kvp in files) {
                var type = kvp.Key;
                var file = kvp.Value;
                Image img;
                try {
                    var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Color", file);
                    img = File.Exists(path) ? Image.FromFile(path) : new Bitmap(_iconSize.Width, _iconSize.Height);
                } catch {
                    img = new Bitmap(_iconSize.Width, _iconSize.Height);
                }

                // Panneau conteneur 96×96 pour que l'image agrandie reste centrée
                var container = new Panel {
                    Width  = 96,
                    Height = 96,
                    Margin = new Padding(20),
                    BackColor = Color.Transparent
                };
                var pb = new PictureBox {
                    Image    = img,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size     = _iconSize,
                    Cursor   = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                void PositionPb() {
                    pb.Left = (container.Width - pb.Width) / 2;
                    pb.Top  = (container.Height - pb.Height) / 2;
                }
                PositionPb();

                pb.MouseEnter += (_, __) => {
                    pb.Size = new Size((int)(_iconSize.Width * 1.25), (int)(_iconSize.Height * 1.25));
                    PositionPb();
                };
                pb.MouseLeave += (_, __) => {
                    pb.Size = _iconSize;
                    PositionPb();
                };
                pb.Click += (_, __) => {
                    ChosenColor = type;
                    DialogResult = DialogResult.OK;
                };
                container.Controls.Add(pb);
                iconsPanel.Controls.Add(container);
            }

            Shown += (_, __) => {
                iconsPanel.Size = iconsPanel.PreferredSize;
                iconsPanel.Left = (ClientSize.Width - iconsPanel.Width) / 2;
                var availableHeight = ClientSize.Height - titleBar.Height;
                iconsPanel.Top = titleBar.Height + (availableHeight - iconsPanel.Height) / 2;
            };
        }

        /// <summary>Affiche la boîte et retourne la couleur choisie, ou null si l'utilisateur annule.</summary>
        public static COLOR_TYPE? Prompt(IWin32Window owner) {
            using var dlg = new ChooseColorForm();
            return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.ChosenColor : null;
        }
    }
}