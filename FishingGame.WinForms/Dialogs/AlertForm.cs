using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace FishingGame.WinForms.Dialogs
{
    /// <summary>
    /// Fenêtre d'alerte modale au style sombre, dotée d'une barre de titre
    /// personnalisée similaire à la fenêtre principale (titre + bouton fermer rouge).
    /// La fenêtre affiche un message et un bouton OK.
    /// </summary>
    public sealed class AlertForm : Form
    {
        public AlertForm(string message)
        {
            // La fenêtre sera sans bordure afin d'utiliser notre propre barre de titre.
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterParent;
            MinimumSize     = new Size(320, 180);
            BackColor       = Color.FromArgb(34, 34, 40);
            ForeColor       = Color.Gainsboro;
            Font            = new Font("Segoe UI", 10f);

            // ===================================================================
            // Barre de titre personnalisée (icône + libellé + bouton fermer)
            // ===================================================================
            var titleBar = new Panel {
                Dock      = DockStyle.Top,
                Height    = 40,
                BackColor = Color.FromArgb(35, 35, 42),
                Padding   = new Padding(12, 0, 12, 0)
            };
            Controls.Add(titleBar);

            // Libellé (titre) de la fenêtre
            var titleLabel = new Label {
                Text      = "Information",
                AutoSize  = true,
                ForeColor = Color.White,
                Font      = new Font(Font.FontFamily, 11, FontStyle.Bold),
                BackColor = Color.Transparent,
                Dock      = DockStyle.Left,
                Padding   = new Padding(0, 12, 0, 0)
            };
            titleBar.Controls.Add(titleLabel);

            // Bouton de fermeture (rond rouge)
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

            // Essayer de charger un icône personnalisé si disponible, sinon un X
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

            // Rendre le bouton parfaitement rond via la propriété Region
            btnClose.Paint += (s, e) => {
                var b = (Button)s!;
                using var gp = new GraphicsPath();
                gp.AddEllipse(0, 0, b.Width, b.Height);
                b.Region = new Region(gp);
            };
            titleBar.Controls.Add(btnClose);
            void LayoutClose() {
                btnClose.Left = titleBar.ClientSize.Width - btnClose.Width - titleBar.Padding.Right;
                btnClose.Top  = Math.Max(0, (titleBar.Height - btnClose.Height) / 2);
            }
            titleBar.Controls.Add(btnClose);
            titleBar.Resize += (_, __) => LayoutClose();
            LayoutClose();

            // ===================================================================
            // Corps de la fenêtre (message + bouton OK)
            // ===================================================================
            var layout = new TableLayoutPanel {
                Dock       = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 70f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));
            Controls.Add(layout);
            
            // Label pour afficher le message (centré et paddé)
            var label = new Label {
                Text      = message,
                Dock      = DockStyle.Fill,
                ForeColor = Color.Gainsboro,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                Padding   = new Padding(20, 10, 20, 10)
            };
            layout.Controls.Add(label, 0, 0);

            // Bouton OK pour fermer la fenêtre
            var btnOk = new Button {
                Text       = "OK",
                DialogResult = DialogResult.OK,
                Anchor     = AnchorStyles.None,
                AutoSize   = true,
                BackColor  = Color.FromArgb(60, 60, 70),
                ForeColor  = Color.Gainsboro,
                FlatStyle  = FlatStyle.Flat,
                Margin     = new Padding(0, 4, 0, 12)
            };
            btnOk.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 90);
            btnOk.FlatAppearance.BorderSize  = 1;
            layout.Controls.Add(btnOk, 0, 1);

            // Permettre de valider la boîte avec la touche Entrée
            AcceptButton = btnOk;
        }

        /// <summary>Affiche une alerte modale.</summary>
        public static void Show(IWin32Window owner, string message) {
            using var form = new AlertForm(message);
            form.ShowDialog(owner);
        }
    }
}