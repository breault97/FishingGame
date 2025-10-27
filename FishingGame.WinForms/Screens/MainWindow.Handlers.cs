﻿using FishingGame.Domain.Events;
using FishingGame.Domain.Interfaces;
using FishingGame.WinForms.Dialogs;
using FishingGame.Domain.Enums;

namespace FishingGame.WinForms.Screens
{
    public partial class MainWindow
    {
        // ───────────────────────────────────────────────────────────────────────
        // Champs asynchrones
        // ───────────────────────────────────────────────────────────────────────
        private TaskCompletionSource<object?>? _stepTcs;
        private Task? _gameTask;

        // ───────────────────────────────────────────────────────────────────────
        // Démarrage / Reset
        // ───────────────────────────────────────────────────────────────────────
        private async Task StartAsync()
        {
            try
            {
                if (GameRunning)
                {
                    AddLog("[INFO] Une partie est déjà en cours.");
                    return;
                }

                // Annuler proprement l'ancienne partie (anims/attentes)
                try { _animCts.Cancel(); } catch { }
                _animCts.Dispose();
                _animCts = new CancellationTokenSource();

                _auto = _chkAuto.Checked;
                _btnStep.Enabled = !_auto;
                _stepGate.Reset(); // porte fermée au lancement en manuel

                if (_game != null) UnWire(_game);
                _game = new FishingGame.Controller.FishingGame(3);
                Wire(_game);

                UI(() =>
                {
                    _journal.Clear();
                    _lblCounts.Text = "Pioche: — | Dépôt: —";
                    _lblSens.Text = "Sens: ↻ horaire";
                    _depositStack.Image = null;
                    LoadBackImage();

                    _leftHand.ClearCards();
                    _rightHand.ClearCards();
                    _bottomHand.ClearCards();

                    _seatsLocked = false;
                    _btnStart.Enabled = false;
                    _btnReset.Enabled = true;
                    _btnStep.Enabled  = !_auto;
                });

                _game.UiRenderDelayMs = _delayMs;
                _game.Begin(7);
                _game.FlipFirstCard();

                // Prime UI immédiat : impose backs sur adversaires AVANT toute anim
                await UI(() =>
                {
                    EnsureSeats();
                    
                    // configuration du pont pour joueur humain
                    if (_chkHuman.Checked)
                    {
                        _humanInput = new FishingGame.Controller.HumanInput();
                        _game.HumanInput = _humanInput;
                        _humanPlayer = _seatBottom;
                        if (_seatBottom != null) _seatBottom.IsHuman = true;
                        if (_seatLeft != null) _seatLeft.IsHuman = false;
                        if (_seatRight != null) _seatRight.IsHuman = false;
                    }
                    else
                    {
                        _humanInput = null;
                        _game.HumanInput = null;
                        _humanPlayer = null;
                        if (_seatBottom != null) _seatBottom.IsHuman = false;
                        if (_seatLeft != null) _seatLeft.IsHuman = false;
                        if (_seatRight != null) _seatRight.IsHuman = false;
                    }
                    // désactiver la case pour qu'on ne change pas en pleine partie
                    _chkHuman.Enabled = false;

                    
                    UpdatePlayersSlots();

                    // Bas en faces, adversaires en backs uniquement
                    if (_bottomPlayer != null)
                    {
                        var imgs = _bottomPlayer.Player.Hand
                            .Select(LoadCardImage)
                            .Where(img => img != null)!
                            .ToArray()!;
                        _bottomHand.SetCards(imgs);
                    }
                    else _bottomHand.ClearCards();

                    if (_leftPlayer != null)
                    {
                        _leftHand.ClearCards();
                        _leftHand.SetBacks(_leftPlayer.Player.Hand.Count, _cardBack!);
                    }
                    else _leftHand.ClearCards();

                    if (_rightPlayer != null)
                    {
                        _rightHand.ClearCards();
                        _rightHand.SetBacks(_rightPlayer.Player.Hand.Count, _cardBack!);
                    }
                    else _rightHand.ClearCards();

                    RenderPlayersHeaderUI();
                    RenderActiveBadgeUI();
                    UpdateStacks();
                    CenterPiles();
                });

                // IMPORTANT : boucle de jeu sur thread de fond (jamais l'UI)
                _gameTask = Task.Run(() => _game.Play(), _animCts.Token);

                UpdateButtonsByState();

                // journaliser la fin / erreurs sans bloquer l’UI
                _gameTask.ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception is { } ex)
                        BeginInvoke(new Action(() => DebugLog($"[ENGINE ERR] {ex.GetBaseException().Message}")));
                    BeginInvoke(new Action(() =>
                    {
                        _btnStart.Enabled = true;
                        _btnReset.Enabled = false;
                        _btnStep.Enabled  = false;
                    }));
                    UpdateButtonsByState();
                }, TaskScheduler.Default);
            }
            catch (OperationCanceledException) { /* normal au reset */ }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Erreur au démarrage",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StopGameAsync()
        {
            try { _animCts.Cancel(); } catch { }
            var t = _gameTask;
            _gameTask = null;
            if (t != null)
            {
                // attendre un peu que le moteur s’arrête
                var completed = await Task.WhenAny(t, Task.Delay(1000));
                // si encore en cours après 1s, on abandonne l’attente (on ne bloque pas l’UI)
            }
        }

        private async void _btnReset_Click(object? s, EventArgs e)
        {
            await StopGameAsync();
            ResetUI();
            UpdateButtonsByState();
        }

        bool GameRunning => _gameTask is { IsCompleted: false };

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            // éviter de tuer un thread en plein milieu
            await StopGameAsync();
            base.OnFormClosing(e);
        }

        private void ResetUI()
        {
            try { _animCts.Cancel(); } catch { }
            _animCts = new CancellationTokenSource();
            _stepTcs?.TrySetCanceled();

            if (_game != null) UnWire(_game);
            _game = null;

            for (int i = AnimHost.Controls.Count - 1; i >= 0; i--)
            {
                if (AnimHost.Controls[i] is PictureBox pb && Equals(pb.Tag, "anim-sprite"))
                {
                    AnimHost.Controls.RemoveAt(i);
                    pb.Dispose();
                }
            }

            _seatsLocked = false;
            _seatBottom = _seatLeft = _seatRight = null;

            _journal.Clear();
            _lblCounts.Text = "Pioche: — | Dépôt: —";
            _lblSens.Text = "Sens: ↻ horaire";

            _leftPlayerHost.Controls.Clear();    _leftPlayerHost.Controls.Add(_leftHand);
            _rightPlayerHost.Controls.Clear();   _rightPlayerHost.Controls.Add(_rightHand);
            _bottomPlayerHost.Controls.Clear();  _bottomPlayerHost.Controls.Add(_bottomHand);

            _leftPlayer = _rightPlayer = _bottomPlayer = null;

            LoadBackImage();
            _depositStack.Image = null;
            _leftHand.ClearCards();
            _rightHand.ClearCards();
            _bottomHand.ClearCards();

            CenterPiles();
            Invalidate(true);

            _btnStart.Enabled = true;
            _btnReset.Enabled = false;
            _btnStep.Enabled  = !_chkAuto.Checked;
            
            _isHumanTurn = false;
            _humanPendingDraw = 0;
            _humanTopCard = null;
            _humanInput?.Reset();
            _humanInput = null;
            _humanPlayer = null;
            _chkHuman.Enabled = true;  // l’utilisateur peut à nouveau changer de mode

            UpdateButtonsByState();
        }

        // ───────────────────────────────────────────────────────────────────────
        // Wire / UnWire
        // ───────────────────────────────────────────────────────────────────────
        private void Wire(FishingGame.Controller.FishingGame g)
        {
            g.TurnPlayed      += OnTurnPlayed;
            g.HaveDrawCards   += OnDraw;
            g.Info            += OnInfo;
            g.DeckAlmostEmpty += OnInfo;
            g.OneCard         += OnOneCard;
            g.GameWon         += OnGameWon;
            g.HumanTurnStarted += OnHumanTurnStarted;
            g.HumanTurnSkipped += OnHumanTurnSkipped;
        }

        private void UnWire(IFishingGameObserver game)
        {
            game.TurnPlayed      -= OnTurnPlayed;
            game.HaveDrawCards   -= OnDraw;
            game.Info            -= OnInfo;
            game.DeckAlmostEmpty -= OnInfo;
            game.OneCard         -= OnOneCard;
            game.GameWon         -= OnGameWon;
            
            if (game is FishingGame.Controller.FishingGame fg) {
                fg.HumanTurnStarted -= OnHumanTurnStarted;
                fg.HumanTurnSkipped -= OnHumanTurnSkipped;
            }
        }

        // ───────────────────────────────────────────────────────────────────────
        // Gates “tempo / pas-à-pas”
        // ───────────────────────────────────────────────────────────────────────
        private Task AfterActionGateAsync()
        {
            if (_auto) return Task.Delay(Math.Max(1, _delayMs / 2), _animCts.Token);

            DebugLog("    [Attente] Prêt pour le prochain pas...");
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            Interlocked.Exchange(ref _stepTcs, tcs)?.TrySetCanceled();
            return tcs.Task.WaitAsync(_animCts.Token);
        }

        // version bloquante pour le thread moteur (jamais l'UI)
        private void AfterActionGate()
        {
            if (_auto) Thread.Sleep(Math.Max(1, _delayMs / 3));
            else _stepGate.WaitOne();
        }

        private void UpdateButtonsByState()
        {
            var running = GameRunning;
            _btnStart.Enabled = !running;
            _btnReset.Enabled = running;
            _btnStep.Enabled  = running && !_auto;
        }

        // ───────────────────────────────────────────────────────────────────────
        // HANDLERS — visibles : seulement les infos “partie”
        // ───────────────────────────────────────────────────────────────────────

        private void OnTurnPlayed(object? sender, TurnEventArgs e)
        {
            // Info “partie” : TOUJOURS visible
            AddLog($"→ Tour : {e.Player} joue {e.PlayedCard}");

            // Pare-feu : si, par erreur, on est sur l'UI → ne jamais bloquer l'UI
            if (!InvokeRequired)
            {
                BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        // >>> Pastille AVANT ANIMATION
                        SetActivePlayer(e.Player);
                        await FlushUIAsync();
                        await Task.Delay(16, _animCts.Token);

                        DebugLog("    [ANIM] Enter AnimatePlay…");
                        DebugLog("    [ANIM] Wait _animGate…");
                        await _animGate.WaitAsync(_animCts.Token); DebugLog("    [ANIM] _animGate acquired");
                        await AnimatePlayAsync(e, _animCts.Token);

                        // Sync globale (mains/compteurs/pastilles selon l’état courant)
                        RenderTurnUI();
                    }
                    catch (OperationCanceledException) { DebugLog("    [ANIM] Canceled"); }
                    catch (Exception ex) { DebugLog($"    [ANIM ERR] {ex.Message}"); }
                    finally { try { _animGate.Release(); DebugLog("    [ANIM] _animGate released"); } catch { } }
                }));
                return; // surtout ne pas Wait() ni AfterActionGate() sur l'UI
            }

            using var done = new ManualResetEventSlim(false);
            using var cts  = CancellationTokenSource.CreateLinkedTokenSource(_animCts.Token);

            BeginInvoke(new Action(async () =>
            {
                try
                {
                    // >>> Pastille AVANT ANIMATION
                    SetActivePlayer(e.Player);
                    await FlushUIAsync();
                    await Task.Delay(16, cts.Token);

                    DebugLog("    [ANIM] Enter AnimatePlay…");
                    DebugLog("    [ANIM] Wait _animGate…");
                    await _animGate.WaitAsync(cts.Token); DebugLog("    [ANIM] _animGate acquired");

                    await AnimatePlayAsync(e, cts.Token); // met à jour la défausse
                    RenderTurnUI();                       // mains + pastille (sync globale)
                }
                catch (OperationCanceledException) { DebugLog("    [ANIM] Canceled"); }
                catch (Exception ex) { DebugLog($"    [ANIM ERR] {ex.Message}"); }
                finally
                {
                    try { _animGate.Release(); DebugLog("    [ANIM] _animGate released"); } catch { }
                    done.Set();
                }
            }));

            done.Wait();       // OK : on est sur le thread moteur
            AfterActionGate(); // tempo / pas-à-pas
        }

        private void OnDraw(object? s, DrawEventArgs e)
        {
            // Info “partie” : TOUJOURS visible
            AddLog($"→ Pioche : {e.Player} pioche {e.Count} carte(s)");

            if (!InvokeRequired)
            {
                BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        // >>> Pastille AVANT ANIMATION
                        SetActivePlayer(e.Player);
                        await FlushUIAsync();
                        await Task.Delay(16, _animCts.Token);

                        await _animGate.WaitAsync(_animCts.Token);
                        await AnimateDrawFromPileToCurrentAsync(_animCts.Token);
                        RenderTurnUI();
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { DebugLog($"[ANIM ERR] {ex.Message}"); }
                    finally { try { _animGate.Release(); } catch { } }
                }));
                return;
            }

            using var done = new ManualResetEventSlim(false);
            using var cts  = CancellationTokenSource.CreateLinkedTokenSource(_animCts.Token);

            BeginInvoke(new Action(async () =>
            {
                try
                {
                    // >>> Pastille AVANT ANIMATION
                    SetActivePlayer(e.Player);
                    await FlushUIAsync();
                    await Task.Delay(16, cts.Token);

                    await _animGate.WaitAsync(cts.Token);
                    await AnimateDrawFromPileToCurrentAsync(cts.Token); // ATTEND la fin
                    RenderTurnUI();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { DebugLog($"[ANIM ERR] {ex.Message}"); }
                finally
                {
                    try { _animGate.Release(); } catch { }
                    done.Set();
                }
            }));

            done.Wait();
            AfterActionGate();
        }

        private void OnInfo(object s, InfoEventArgs e)
        {
            // Infos “ConsoleLogger” : TOUJOURS visibles
            if (!InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    AddInfo(e.Message);
                    RenderActiveBadgeUI();
                }));
                return;
            }

            AddInfo(e.Message);
            BeginInvoke(new Action(RenderActiveBadgeUI));
            AfterActionGate(); // bloquant côté moteur uniquement
        }

        private async void OnOneCard(object? s, PlayerEventArgs e)
        {
            AddLog($"⚠️ {e.Player} n'a plus qu'une seule carte !");
            await AfterActionGateAsync();
        }

        private async void OnGameWon(object? s, PlayerEventArgs e)
        {
            UI(() =>
            {
                AddLog($"🏆 {e.Player} a gagné la partie !");
                _btnStart.Enabled = true;
                _btnReset.Enabled = false;
                _btnStep.Enabled  = false;
            });
            _stepTcs?.TrySetResult(null); // libère un pas à pas éventuel
            await Task.Delay(500);
        }
        
        private void OnHumanTurnStarted(object? sender, HumanTurnEventArgs e) {
            if (_game == null) return;
            UI(() => {
                _humanPlayer      = _bottomPlayer?.Player;
                _humanTopCard     = e.TopCard;
                _humanPendingDraw = e.PendingDraw;
                _isHumanTurn      = true;

                // Allume immédiatement la pastille verte du joueur
                SetActivePlayer(e.Player);
                RenderActiveBadgeUI();

                // Active la main et configure l’agrandissement sur cartes jouables
                _bottomHand.Enabled = true;
                if (_humanPlayer != null) {
                    _bottomHand.AllowHoverCallback = idx => {
                        try {
                            if (!_isHumanTurn || _game == null || _humanPlayer == null) return false;
                            if (idx < 0 || idx >= _humanPlayer.Hand.Count) return false;
                            var c = _humanPlayer.Hand.ElementAt(idx);
                            return _game.IsCardPlayable(c);
                        } catch { return false; }
                    };
                } else {
                    _bottomHand.AllowHoverCallback = null;
                }

                // Alerte sur les +2
                if (_humanPendingDraw > 0 && _humanPlayer != null) {
                    bool hasTwo = _humanPlayer.Hand.Any(c => c.Value == CARD_VALUE.TWO);
                    if (!hasTwo) ShowAlert($"Tu dois piocher {_humanPendingDraw} cartes!");
                    else ShowAlert($"Attaque +{_humanPendingDraw} \n Tu peux contrer avec un 2.");
                }
            });
        }

        private void OnHumanTurnSkipped(object? sender, PlayerEventArgs e) {
            UI(() => {
                _isHumanTurn = false;
                _bottomHand.Enabled = false;
                _bottomHand.AllowHoverCallback = null;
                ShowAlert("Ton tour est passé!");
                _humanInput?.Reset();
            });
        }

        private void OnBottomHandCardClicked(object? sender, int index) {
            if (!_isHumanTurn) { ShowAlert("Ce n'est pas votre tour."); return; }
            if (_game == null || _humanPlayer == null) return;
            if (index < 0 || index >= _humanPlayer.Hand.Count) return;

            var card = _humanPlayer.Hand.ElementAt(index);
            if (!_game.IsCardPlayable(card)) {
                ShowAlert("Cette carte n'est pas jouable.");
                return;
            }
            COLOR_TYPE? chosenColor = null;
            if (card.Value == CARD_VALUE.JACK) {
                chosenColor = ChooseColorForm.Prompt(this);
                if (chosenColor == null) {
                    ShowAlert("Aucune couleur choisie! \n L'action est annulée.");
                    return;
                }
            }
            _isHumanTurn = false;
            _bottomHand.Enabled = false;
            _bottomHand.AllowHoverCallback = null;
            _humanInput?.SubmitPlay(card, chosenColor);
        }

        private void OnDrawStackClicked(object? sender, EventArgs e) {
            if (!_isHumanTurn) return;
            _isHumanTurn = false;
            _bottomHand.Enabled = false;
            _bottomHand.AllowHoverCallback = null;
            _humanInput?.SubmitDraw();
        }

        // Agrandissement de la pioche
        private void OnDrawStackMouseEnter(object? sender, EventArgs e) {
            // Ne rien faire si ce n’est pas le tour du joueur humain
            if (!_isHumanTurn) return;

            if (_drawStackOriginalSize == null) {
                _drawStackOriginalSize = _drawStack.Size;
                _drawStackOriginalLocation = _drawStack.Location;
            }
            if (_drawStackOriginalSize.HasValue && _drawStack.Size == _drawStackOriginalSize.Value) {
                var orig    = _drawStackOriginalSize.Value;
                var newSize = new Size((int)(orig.Width * 1.25), (int)(orig.Height * 1.25));
                var centerX = _drawStackOriginalLocation!.Value.X + orig.Width / 2;
                var centerY = _drawStackOriginalLocation!.Value.Y + orig.Height / 2;
                var newLoc  = new Point(centerX - newSize.Width / 2, centerY - newSize.Height / 2);
                _drawStack.Size     = newSize;
                _drawStack.Location = newLoc;
                _drawStack.BringToFront();
            }
        }
        
        private void OnDrawStackMouseLeave(object? sender, EventArgs e) {
            if (_drawStackOriginalSize.HasValue && _drawStackOriginalLocation.HasValue) {
                _drawStack.Size = _drawStackOriginalSize.Value;
                _drawStack.Location = _drawStackOriginalLocation.Value;
            }
        }

        /// <summary>Affiche un message à l’utilisateur via la fenêtre personnalisée.</summary>
        private void ShowAlert(string message) {
            if (InvokeRequired) {
                BeginInvoke(new Action(() => ShowAlert(message)));
                return;
            }
            try {
                FishingGame.WinForms.Dialogs.AlertForm.Show(this, message);
            } catch {
                AddInfo(message);
            }
        }
    }
}