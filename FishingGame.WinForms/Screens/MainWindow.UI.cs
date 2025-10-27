﻿using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using FishingGame.Domain.Class;
using FishingGame.WinForms.Controls;

namespace FishingGame.WinForms.Screens
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            // ─────────────────────────────
            // Fenêtre & perf
            // ─────────────────────────────
            FormBorderStyle = FormBorderStyle.None;   // pas de cadre système
            ControlBox      = false;                  // pas de menu système
            Text = "";
            Width = 1920; Height = 1080;
            BackColor = _bg;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();

            // ─────────────────────────────
            // Barre de titre custom (Dock=Top)
            // ─────────────────────────────
            _titleBar = new Panel {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(35, 35, 42),
                Padding = new Padding(12, 0, 12, 0)
            };
            Controls.Add(_titleBar);

            // Zone gauche : icône + titre
            var leftStack = new FlowLayoutPanel {
                Dock = DockStyle.Left,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0) // légère descente pour aligner verticalement
            };
            _titleBar.Controls.Add(leftStack);

            // Icône d’app
            _titleAppIcon = new PictureBox {
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 25,
                Height = 25,
                Margin = new Padding(0, 2, 8, 0),
                BackColor = Color.Transparent
            };
            leftStack.Controls.Add(_titleAppIcon);

            // Titre
            _titleLabel = new Label {
                Text = "FishingGame",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font(Font.FontFamily, 11, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            leftStack.Controls.Add(_titleLabel);

            // Bouton fermer (rond rouge) — ancré à droite, centré V via LayoutCloseButton()
            _btnClose = new Button {
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(200, 40, 50),
                ForeColor = Color.White,
                Size = new Size(30, 30),      // carré → MakeCircle() appliquera la forme ronde
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = false
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 60, 70);
            _btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(170, 30, 40);
            _btnClose.Click += (_, __) => Close();

            // Cercle auto + recalc du centrage vertical quand la taille change
            MakeCircle(_btnClose);
            _btnClose.SizeChanged += (_, __) => { MakeCircle(_btnClose); LayoutCloseButton(); };

            _titleBar.Controls.Add(_btnClose);

            // Charger les icônes (app + close si dispo)
            try
            {
                var appIco = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "app.ico");
                var appPng = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "app.png");
                if (File.Exists(appPng))
                    _titleAppIcon.Image = new Bitmap(appPng);
                else if (File.Exists(appIco))
                    using (var ico = new Icon(appIco, new Size(20, 20)))
                        _titleAppIcon.Image = ico.ToBitmap();

                var closePng = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "close.png");
                if (File.Exists(closePng))
                {
                    using var raw = Image.FromFile(closePng);
                    _btnClose.Image = new Bitmap(raw, new Size(16, 16));
                    _btnClose.Text = "";
                }
                else
                {
                    _btnClose.Text = "✕";
                    _btnClose.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
                }
            }
            catch
            {
                _btnClose.Text = "✕";
                _btnClose.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
            }

            // Drag fenêtre : toute la zone gauche est "draggable"
            void HookDrag(Control c) => c.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) DragWindow(); };
            HookDrag(_titleBar); HookDrag(leftStack); HookDrag(_titleAppIcon); HookDrag(_titleLabel);

            // Définir l’icône de la fenêtre si dispo
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "app.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);

            // Centrer V le bouton fermer quand la barre se redimensionne
            _titleBar.Resize += (_, __) => LayoutCloseButton();
            LayoutCloseButton(); // premier placement

            SuspendLayout(); // construire le reste sans relayout intermédiaire

            // ─────────────────────────────
            // Toolbar
            // ─────────────────────────────
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(12),
                BackColor = _panel,
                WrapContents = false
            };
            StylePrimaryButton(_btnStart);
            StyleButton(_btnReset);
            StyleButton(_btnStep);

            toolbar.Controls.Add(_btnStart);
            toolbar.Controls.Add(_btnReset);
            toolbar.Controls.Add(new Panel { Width = 16, Height = 1 });
            toolbar.Controls.Add(_chkAuto);
            toolbar.Controls.Add(_chkHuman);
            toolbar.Controls.Add(_btnStep);
            toolbar.Controls.Add(new Panel { Width = 16, Height = 1 });

            void StyleSpeed(Button b)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.BackColor = Color.FromArgb(50, 50, 58);
                b.ForeColor = Color.White;
                b.Height = 32;
                b.Width = 84;
                b.Margin = new Padding(6, 6, 0, 0);
            }
            void SetSpeedActive(Button active)
            {
                foreach (var b in new[] { _btnSlow, _btnNormal, _btnFast })
                    b.BackColor = (b == active) ? Color.FromArgb(70, 70, 90) : Color.FromArgb(50, 50, 58);
            }
            StyleSpeed(_btnSlow);
            StyleSpeed(_btnNormal);
            StyleSpeed(_btnFast);

            _btnSlow.Click   += (_, __) => { _delayMs = SPEED_SLOW;   if (_game != null) _game.UiRenderDelayMs = _delayMs; SetSpeedActive(_btnSlow);   };
            _btnNormal.Click += (_, __) => { _delayMs = SPEED_NORMAL; if (_game != null) _game.UiRenderDelayMs = _delayMs; SetSpeedActive(_btnNormal); };
            _btnFast.Click   += (_, __) => { _delayMs = SPEED_FAST;   if (_game != null) _game.UiRenderDelayMs = _delayMs; SetSpeedActive(_btnFast);   };

            SetSpeedActive(_btnNormal);

            toolbar.Controls.Add(_btnSlow);
            toolbar.Controls.Add(_btnNormal);
            toolbar.Controls.Add(_btnFast);
            
            // --- Sélecteur de dos de carte ---
            StyleButton(_btnBackStyle);
            UpdateBackButtonCaption();
            _btnBackStyle.Click += (_, __) =>
            {
                BuildCardBackMenu();
                _menuBacks.Show(_btnBackStyle, new Point(0, _btnBackStyle.Height));
            };
            toolbar.Controls.Add(new Panel { Width = 8, Height = 1 });
            toolbar.Controls.Add(_btnBackStyle);
            
            Controls.Add(toolbar);

            // Espace sous la toolbar
            Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10, BackColor = _bg });
            
            // Accrocher les événements de clic pour joueur humain
            _bottomHand.CardClicked      += OnBottomHandCardClicked;
            _drawStack.Click             += OnDrawStackClicked;
            _drawStack.ImageBox.Click    += OnDrawStackClicked;
            _drawStack.MouseEnter        += OnDrawStackMouseEnter;
            _drawStack.MouseLeave        += OnDrawStackMouseLeave;
            _drawStack.ImageBox.MouseEnter += OnDrawStackMouseEnter;
            _drawStack.ImageBox.MouseLeave += OnDrawStackMouseLeave;

            // ─────────────────────────────
            // Journal
            // ─────────────────────────────
            var journalHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = 300,
                Padding = new Padding(16),
                BackColor = Color.FromArgb(28, 28, 33)
            };
            var jTitle = new Label { Text = "Journal", ForeColor = _text, Dock = DockStyle.Top, Height = 22 };
            _journal.Dock = DockStyle.Fill;
            journalHost.Controls.Add(_journal);
            journalHost.Controls.Add(jTitle);
            Controls.Add(journalHost);

            // Espace fixe entre journal et board
            Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12, BackColor = _bg });

            // ─────────────────────────────
            // Hôtes joueurs + mains
            // ─────────────────────────────
            Controls.Add(_leftPlayerHost);
            Controls.Add(_rightPlayerHost);
            Controls.Add(_bottomPlayerHost);

            _leftPlayerHost.Controls.Add(_leftHand);
            _rightPlayerHost.Controls.Add(_rightHand);
            _bottomPlayerHost.Controls.Add(_bottomHand);

            RemoveLegacyCardViews(_leftPlayerHost);
            RemoveLegacyCardViews(_rightPlayerHost);
            RemoveLegacyCardViews(_bottomPlayerHost);

            // chevauchement (≈ 70% visible)
            _leftHand.VisibleRatio   = 0.70f;
            _rightHand.VisibleRatio  = 0.70f;
            _bottomHand.VisibleRatio = 0.70f;

            // pas de pastille sur les mains (elles sont sur les PlayerItem)
            _bottomHand.ShowActiveDot = false;
            _leftHand.ShowActiveDot   = false;
            _rightHand.ShowActiveDot  = false;

            _bottomHand.UseWidthForScale = true;
            _leftHand.UseWidthForScale   = false;
            _rightHand.UseWidthForScale  = false;

            _leftHand.BringToFront();
            _rightHand.BringToFront();
            _bottomHand.BringToFront();

            // ─────────────────────────────
            // Board/Table + piles
            // ─────────────────────────────
            _board.Padding = new Padding(0, 24, 0, 0);
            Controls.Add(_board); // Dock=Fill

            _table.Anchor = AnchorStyles.None;        // centré manuellement
            _board.Controls.Add(_table);
            _table.BringToFront();

            _drawStack.BackColor    = Color.Transparent;
            _depositStack.BackColor = Color.Transparent;
            _table.Controls.Add(_drawStack);
            _table.Controls.Add(_depositStack);
            _drawStack.BringToFront();
            _depositStack.BringToFront();
            
            // >>> DESSIN overlay couleur via PAINT de la PictureBox de la défausse
            _depositStack.ImageBox.Paint += DepositColorOverlay_Paint;

            _drawStack.ImageBox.SizeMode    = PictureBoxSizeMode.Zoom;
            _depositStack.ImageBox.SizeMode = PictureBoxSizeMode.Zoom;
            
            // --- échelle des piles (+25 %) ---
            _stackBaseSize = _drawStack.Size;    // base (130x180 dans les Fields)
            ApplyStackScale(STACK_SCALE);        // agrandit + recentre

            // Recentrages fiables
            _board.Resize += (_, __) => CenterPiles();
            _table.Resize += (_, __) => CenterPiles();
            Shown         += (_, __) => CenterPiles();

            // Infos plateau (compteurs + sens)
            var infoPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };
            _board.Controls.Add(infoPanel);
            infoPanel.BringToFront();
            infoPanel.Left = 12;
            infoPanel.Top  = _board.Height - 60;
            infoPanel.Controls.Add(_lblCounts);
            infoPanel.Controls.Add(_lblSens);
            _board.Resize += (_, __) => infoPanel.Top = _board.Height - 60;

            // ─────────────────────────────
            // Événements UI (lancement, reset, pas-à-pas, auto)
            // ─────────────────────────────
            _btnStart.Click += async (_, __) =>
            {
                if (GameRunning)
                {
                    AddLog("[INFO] Une partie est déjà en cours.");
                    return;
                }
                _btnStart.Enabled = false;
                try { await StartAsync(); }
                finally { UpdateButtonsByState(); }
            };

            _btnReset.Click += _btnReset_Click;

            _btnStep.Click += (_, __) =>
            {
                try { _stepGate.Set(); } catch { }
                _stepTcs?.TrySetResult(null);
            };

            _chkAuto.CheckedChanged += (_, __) =>
            {
                _auto = _chkAuto.Checked;
                _btnStep.Enabled = !_auto && GameRunning;
            };

            // Debounce redraw sur resize des hôtes
            _resizeDebounce.Tick += (_, __) => { _resizeDebounce.Stop(); UpdateHands(); };
            _leftPlayerHost.Resize   += (_, __) => { _resizeDebounce.Stop(); _resizeDebounce.Start(); };
            _rightPlayerHost.Resize  += (_, __) => { _resizeDebounce.Stop(); _resizeDebounce.Start(); };
            _bottomPlayerHost.Resize += (_, __) => { _resizeDebounce.Stop(); _resizeDebounce.Start(); };

            // ─────────────────────────────
            // État initial
            // ─────────────────────────────
            LoadBackImage();     // dos de la pioche
            CenterPiles();
            ResetUI();           // nettoie labels/compteurs/mains (sans toucher aux piles)

            UpdateButtonsByState();

            // S’assurer que la barre de titre reste au-dessus
            if (_titleBar != null)
            {
                _titleBar.BringToFront();
                Controls.SetChildIndex(_titleBar, Controls.Count - 1);
            }

            ResumeLayout(performLayout: true);
        }
        
        // Centre verticalement le bouton "fermer" dans la barre de titre
        private void LayoutCloseButton()
        {
            if (_titleBar == null || _btnClose == null) return;

            // aligné à droite (respecte le Padding droit de la barre)
            int padRight = _titleBar.Padding.Right;
            _btnClose.Left = _titleBar.ClientSize.Width - _btnClose.Width - padRight;

            // centré verticalement dans la hauteur de la barre
            _btnClose.Top = Math.Max(0, (_titleBar.ClientSize.Height - _btnClose.Height) / 2);
        }
        
        protected override CreateParams CreateParams {
            get {
                const int CS_DROPSHADOW = 0x00020000;
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }
        
        // ——— Rendu des 3 mains à partir du modèle ———
        private void RenderHandsUI()
        {
            if (_game == null || IsDisposed) return;

            // Bas : vraies cartes
            if (_bottomPlayer != null)
            {
                var imgs = _bottomPlayer.Player.Hand
                    .Select(LoadCardImage)
                    .Where(img => img != null)!
                    .ToArray()!;
                _bottomHand.SetCards(imgs);
            }
            else _bottomHand.ClearCards();

            // Gauche/Droite : dos uniquement
            if (_leftPlayer != null)
            {
                _leftHand.ClearCards();
                if (_cardBack != null)
                    _leftHand.SetBacks(_leftPlayer.Player.Hand.Count, _cardBack);
            }
            else _leftHand.ClearCards();

            if (_rightPlayer != null)
            {
                _rightHand.ClearCards();
                if (_cardBack != null)
                    _rightHand.SetBacks(_rightPlayer.Player.Hand.Count, _cardBack);
            }
            else _rightHand.ClearCards();

            // Ajuste la hauteur réelle pour autoriser le zoom 25% sans rognage
            EnsureBottomHandRoom();

            _lblCounts.Text = $"Pioche: {_game.DrawPileCount} | Dépôt: {_game.DepositPileCount}";
        }

        private void RenderActiveBadgeUI()
        {
            if (_game == null || IsDisposed) return;
            var cur = _game.CurrentPlayer;

            // pas de pastille sur les mains
            _bottomHand.SetActive(false);
            _leftHand.SetActive(false);
            _rightHand.SetActive(false);

            // pastille sur les entêtes joueurs
            _bottomPlayer?.SetActive(_bottomPlayer.Player == cur);
            _leftPlayer?.SetActive(_leftPlayer.Player     == cur);
            _rightPlayer?.SetActive(_rightPlayer.Player   == cur);
        }
        
        private void RenderPlayersHeaderUI()
        {
            if (_bottomPlayer != null) _bottomPlayer.SetHandCount(_bottomPlayer.Player.Hand.Count);
            if (_leftPlayer   != null) _leftPlayer.SetHandCount(_leftPlayer.Player.Hand.Count);
            if (_rightPlayer  != null) _rightPlayer.SetHandCount(_rightPlayer.Player.Hand.Count);
        }

        private void RenderTurnUI()
        {
            RenderHandsUI();         // mains (déjà OK)
            RenderPlayersHeaderUI(); // ← compteurs “X cartes”
            RenderActiveBadgeUI();   // ← pastilles entêtes
        }

        // --- helper local : purge d’anciens CardView qui masquent OverlapHandControl
        private static void RemoveLegacyCardViews(Control host)
        {
            for (int i = host.Controls.Count - 1; i >= 0; i--)
            {
                if (host.Controls[i] is CardView cv)
                {
                    host.Controls.RemoveAt(i);
                    cv.Dispose();
                }
            }
        }

        // --------- Helpers intacts ---------
        // === Logging UI-safe, jamais annulé, toujours en nouvelle ligne ===
        private void AddLog(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => AddLog(text))); return; }

            // Si _journal est un TextBox/RichTextBox
            _journal.AppendText(text);
            if (!text.EndsWith(Environment.NewLine))
                _journal.AppendText(Environment.NewLine);

            // Auto-scroll
            _journal.SelectionStart = _journal.TextLength;
            _journal.ScrollToCaret();
        }

        private void AddInfo(string text) => AddLog($"[INFO] {text}");

        private Task UI(Action action)
        {
            if (IsDisposed) return Task.CompletedTask;

            if (InvokeRequired)
            {
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                BeginInvoke(new Action(() =>
                {
                    try { action(); tcs.SetResult(); }
                    catch (Exception ex) { tcs.SetException(ex); }
                }));
                return tcs.Task;
            }

            action();
            return Task.CompletedTask;
        }

        private string PickAvatarFor(Player p)
        {
            var files = new[]
            {
                "avatar1.png","avatar2.png","avatar3.png","avatar4.png",
                "avatar5.png","avatar6.png","avatar7.png","avatar8.png",
            };
            var list = new List<string>();
            foreach (var name in files)
            {
                var full = Path.Combine(AppContext.BaseDirectory, "Assets", "Avatars", name);
                if (File.Exists(full)) list.Add(full);
            }
            if (list.Count == 0)
                return Path.Combine(AppContext.BaseDirectory, "Assets", "Avatars", files[0]);

            int i = Math.Abs(p.GetHashCode()) % list.Count;
            return list[i];
        }

        private static void StylePrimaryButton(Button b)
        {
            b.UseVisualStyleBackColor = false;
            b.ForeColor = Color.White;
            b.BackColor = Color.FromArgb(80, 120, 255);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Padding = new Padding(10, 6, 10, 6);
            b.AutoSize = true;
        }

        private static void StyleButton(Button b)
        {
            b.UseVisualStyleBackColor = false;
            b.ForeColor = Color.White;
            b.BackColor = Color.FromArgb(60, 60, 70);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Padding = new Padding(10, 6, 10, 6);
            b.AutoSize = true;
        }
        
        // — déplacer la fenêtre sans bordure —
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private void DragWindow()
        {
            try { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0); }
            catch { /* ignore */ }
        }
        
        private static void MakeCircle(Control c)
        {
            // s’assure que le contrôle est carré (diamètre = côté mini)
            int d = Math.Min(c.Width, c.Height);
            if (c.Width != d || c.Height != d)
                c.Size = new Size(d, d);

            c.Region?.Dispose();
            using var gp = new GraphicsPath();
            gp.AddEllipse(0, 0, d - 1, d - 1);   // cercle parfait
            c.Region = new Region(gp);
        }

        // ========= AJOUTS POUR LA PASTILLE AVANT L’ANIM =========
        // Force la pastille verte sur l’entête correspondant à player (sans attendre le domaine)
        private void SetActivePlayer(Player p)
        {
            if (IsDisposed) return;

            // pas de pastille sur les mains
            _bottomHand.SetActive(false);
            _leftHand.SetActive(false);
            _rightHand.SetActive(false);

            _bottomPlayer?.SetActive(_bottomPlayer.Player == p);
            _leftPlayer?.SetActive(_leftPlayer.Player     == p);
            _rightPlayer?.SetActive(_rightPlayer.Player   == p);
        }

        // Laisse la boucle de messages WinForms peindre avant de continuer
        private Task FlushUIAsync()
        {
            if (IsDisposed) return Task.CompletedTask;

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            BeginInvoke(new Action(() => tcs.SetResult()));
            return tcs.Task;
        }
        
        // Dessin de l’overlay couleur directement dans la PictureBox de la défausse
        private void DepositColorOverlay_Paint(object? sender, PaintEventArgs e)
        {
            if (!_colorOverlayVisible || _colorOverlayImg == null) return;

            var pb = (PictureBox)sender!;
            int w = Math.Min(COLOR_OVERLAY_PX, pb.ClientSize.Width);
            int h = Math.Min(COLOR_OVERLAY_PX, pb.ClientSize.Height);

            // garder le ratio source
            double ar = _colorOverlayImg.Width / (double)_colorOverlayImg.Height;
            if (w / (double)h > ar) w = (int)(h * ar);
            else                     h = (int)(w / ar);

            var x = (pb.ClientSize.Width  - w) / 2;
            var y = (pb.ClientSize.Height - h) / 2;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            e.Graphics.DrawImage(_colorOverlayImg, new Rectangle(x, y, w, h));
        }
        
        // Construit le menu contextuel avec les dos disponibles
        private void BuildCardBackMenu()
        {
            _menuBacks.Items.Clear();
            var options = GetAvailableCardBacks();
            if (options.Count == 0)
            {
                _menuBacks.Items.Add(new ToolStripMenuItem("(Aucun dos trouvé)") { Enabled = false });
                return;
            }

            foreach (var key in options.Keys.OrderBy(k => k))
            {
                var mi = new ToolStripMenuItem(ToTitle(key))
                {
                    Checked = string.Equals(key, _cardBackKey, StringComparison.OrdinalIgnoreCase)
                };
                mi.Click += (_, __) => ApplyCardBack(key);
                _menuBacks.Items.Add(mi);
            }
        }

        private void UpdateBackButtonCaption()
        {
            _btnBackStyle.Text = $"Dos : {ToTitle(_cardBackKey)}";
        }

        private static string ToTitle(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return key;
            return char.ToUpperInvariant(key[0]) + key.Substring(1).ToLowerInvariant();
        }
        
        private void ApplyStackScale(float factor)
        {
            if (_stackBaseSize.Width <= 0 || _stackBaseSize.Height <= 0) return;

            var newSize = new Size(
                (int)Math.Round(_stackBaseSize.Width  * factor),
                (int)Math.Round(_stackBaseSize.Height * factor));

            _drawStack.Size    = newSize;
            _depositStack.Size = newSize;

            CenterPiles(); // recalcule l’emplacement au centre du plateau
        }
    }
}