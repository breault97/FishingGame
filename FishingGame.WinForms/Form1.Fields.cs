using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using FishingGame.Controller;
using FishingGame.Domain.Class;
using FishingGame.Domain.Struct;

namespace FishingGame.WinForms
{
    public partial class Form1 : Form
    {
        
        
        
        
        // Active/désactive tous les logs de debug (moteur/anim/gates).
        private const bool ENABLE_DEBUG_JOURNAL = false;

        [System.Diagnostics.DebuggerStepThrough]
        private void DebugLog(string text)
        {
            if (ENABLE_DEBUG_JOURNAL) AddLog(text);
        }
        
        
        
        
        private FishingGame.Controller.FishingGame? _game;
        // Mode & tempo
        //private TaskCompletionSource<object?>? _stepTcs;
        private volatile bool _auto = true;   // Auto coché/décoché pilote le gate
        
        // Sérialisation des animations (une à la fois)
        private readonly SemaphoreSlim _animGate = new(1, 1);

        // Mode pas-à-pas : l'utilisateur clique pour libérer
        private readonly AutoResetEvent _stepGate = new(false);

        // Annulation des animations en cours lors d’un Reset/Restart
        private CancellationTokenSource _animCts = new();
        
        private readonly Button _btnSlow   = new() { Text = "Lent" };
        private readonly Button _btnNormal = new() { Text = "Normal" };
        private readonly Button _btnFast   = new() { Text = "Rapide" };

        // vitesses de base (ms) : ajuste si besoin
        private const int SPEED_SLOW   = 1200;
        private const int SPEED_NORMAL = 900;
        private const int SPEED_FAST   = 600; // le plus rapide possible SANS bug visuel

        private int _delayMs = SPEED_NORMAL;
        
        private bool _seatsLocked;
        private Player? _seatBottom, _seatLeft, _seatRight;
        
        // --- custom title bar bits ---
        private Panel? _titleBar;
        private Label? _titleLabel;
        private PictureBox? _titleAppIcon;
        private Button? _btnClose;

        // THEME
        private readonly Color _bg = Color.FromArgb(24, 24, 28);
        private readonly Color _panel = Color.FromArgb(30, 30, 35);
        private readonly Color _text = Color.White;
        
        // Mains visibles
        private readonly OverlapHandControl _leftHand   = new() { Dock = DockStyle.Fill,  VerticalCenter = true,  MaxCardHeight = 160 };
        private readonly OverlapHandControl _rightHand  = new() { Dock = DockStyle.Fill,  VerticalCenter = true,  MaxCardHeight = 160 };
        private readonly OverlapHandControl _bottomHand = new() { Dock = DockStyle.Bottom, Height = 180, VerticalCenter = false, UseWidthForScale = true, MaxCardWidth = 120 };

        // Piles au centre : contrôles custom avec ombre + PictureBox interne
        private readonly ShadowStack _drawStack    = new() { Width = 130, Height = 180 };
        private readonly ShadowStack _depositStack = new() { Width = 130, Height = 180 };

        // Couche d'animation
        private Control AnimHost => _table;
        
        // Toolbar
        private readonly Button _btnStart = new() { Text = "Démarrer" };
        private readonly Button _btnReset = new() { Text = "Reset" };
        private readonly CheckBox _chkAuto = new() { Text = "Auto", Checked = true, ForeColor = Color.White, AutoSize = true };
        private readonly Button _btnStep = new() { Text = "Pas à pas", Enabled = false };
        
        // Journal (haut-centre)
        private readonly RichTextBox _journal = new()
        {
            BorderStyle = BorderStyle.None, ReadOnly = true, DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical, Font = new Font("Consolas", 12f),
            BackColor = Color.FromArgb(34, 34, 40), ForeColor = Color.Gainsboro, Dock = DockStyle.Fill
        };

        // Infos plateau
        private readonly Label _lblCounts = new() { ForeColor = Color.Silver, AutoSize = true };
        private readonly Label _lblSens   = new() { ForeColor = Color.Silver, AutoSize = true };

        // Joueurs (gauche, droite, bas)
        private readonly Panel _leftPlayerHost   = new() { Dock = DockStyle.Left,  Width = 320, Padding = new Padding(12), BackColor = Color.FromArgb(30,30,35) };
        private readonly Panel _rightPlayerHost  = new() { Dock = DockStyle.Right, Width = 320, Padding = new Padding(12), BackColor = Color.FromArgb(30,30,35) };
        private readonly Panel _bottomPlayerHost = new() { Dock = DockStyle.Bottom, Height = 240, Padding = new Padding(12), BackColor = Color.FromArgb(30,30,35) };

        private PlayerItem? _leftPlayer;
        private PlayerItem? _rightPlayer;
        private PlayerItem? _bottomPlayer;

        // Board centre
        private readonly Panel _board = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(26, 26, 30) };

        // Plateau (plus petit) centré dans _board
        private readonly Panel _table = new()
        {
            BackColor = Color.FromArgb(26, 26, 30), // même teinte que _board
            Size = new Size(800, 440)               // taille initiale ; sera recalculée
        };
        
        // >>> debounce pour UpdateHands pendant les resize
        private readonly System.Windows.Forms.Timer _resizeDebounce = new() { Interval = 60 };
        
        private void EnsureSeats()
        {
            if (_seatsLocked || _game == null) return;
            if (_game.CurrentPlayer == null) return; // on attend que Begin() ait défini le pivot

            var pivot = _game.CurrentPlayer;

            // ordre visuel qui ne tourne pas : [bas, gauche, droite]
            var list = BuildRing(pivot); // -> [pivot, next, next]
            _seatBottom = list.Count > 0 ? list[0] : null;
            _seatLeft   = list.Count > 1 ? list[1] : null;
            _seatRight  = list.Count > 2 ? list[2] : null;

            // Bas
            if (_seatBottom != null)
            {
                _bottomPlayerHost.Controls.Clear();
                _bottomPlayer = new PlayerItem(_seatBottom, PickAvatarFor(_seatBottom)) { Dock = DockStyle.Top };
                _bottomPlayerHost.Controls.Add(_bottomPlayer);
                _bottomPlayerHost.Controls.Add(_bottomHand);
            }
            // Gauche
            if (_seatLeft != null)
            {
                _leftPlayerHost.Controls.Clear();
                _leftPlayer = new PlayerItem(_seatLeft, PickAvatarFor(_seatLeft)) { Dock = DockStyle.Top };
                _leftPlayerHost.Controls.Add(_leftPlayer);
                _leftPlayerHost.Controls.Add(_leftHand);
            }
            // Droite
            if (_seatRight != null)
            {
                _rightPlayerHost.Controls.Clear();
                _rightPlayer = new PlayerItem(_seatRight, PickAvatarFor(_seatRight)) { Dock = DockStyle.Top };
                _rightPlayerHost.Controls.Add(_rightPlayer);
                _rightPlayerHost.Controls.Add(_rightHand);
            }

            // chevauchement 30% (70% visible)
            _leftHand.VisibleRatio   = 0.70f;
            _rightHand.VisibleRatio  = 0.70f;
            _bottomHand.VisibleRatio = 0.70f;

            _seatsLocked = true;
        }
    }
}