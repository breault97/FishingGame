using System.Diagnostics;
using FishingGame.Domain.Class;
using FishingGame.Domain.Events;
using FishingGame.Domain.Struct;

namespace FishingGame.WinForms.Screens
{
    public partial class MainWindow
    {
        // Exécute du code UI de manière SYNCHRONE (bloque le thread appelant le temps d’appliquer)
        private void UI_SYNC(Action a)
        {
            if (!InvokeRequired) { a(); return; }

            Exception? err = null;
            using var done = new ManualResetEventSlim(false);
            BeginInvoke(new Action(() =>
            {
                try { a(); }
                catch (Exception ex) { err = ex; }
                finally { done.Set(); }
            }));

            if (!done.Wait(500))
            {
                // On reposte en fire-and-forget et on continue
                UI_FF(a);
                return;
            }
            if (err != null) throw err;
        }

        // Fire-and-forget sur le thread UI (ne JAMAIS attendre ce post)
        private void UI_FF(Action a)
        {
            if (InvokeRequired) BeginInvoke(a);
            else a();
        }

        private Control GetHandHostFor(Player p)
        {
            if (_bottomPlayer?.Player == p) return _bottomHand;
            if (_leftPlayer?.Player   == p) return _leftHand;
            if (_rightPlayer?.Player  == p) return _rightHand;
            return _bottomHand;
        }

        private static bool IsReady(Control c)
            => c != null && !c.IsDisposed && c.IsHandleCreated && c.Visible;

        private static Point CenterOf(Control from, Control toContainer)
        {
            if (from == null || toContainer == null) return Point.Empty;
            if (!IsReady(from) || !IsReady(toContainer)) return Point.Empty;

            var centerLocal = new Point(from.Width / 2, from.Height / 2);
            var screen = from.PointToScreen(centerLocal);
            return toContainer.PointToClient(screen);
        }

        private async Task AnimatePlayAsync(TurnEventArgs e, CancellationToken ct)
        {
            // 100% SANS _animGate ici
            await AnimatePlayFromPlayerToDepositAsync(e.Player, e.PlayedCard!.Value, ct);

            // Fin d’anim : mettre la carte sur la défausse (UI)
            DebugLog("   [ANIM] Set deposit image");
            if (!IsDisposed) BeginInvoke(new Action(() =>
            {
                _depositStack.Image = LoadCardImage(e.PlayedCard!.Value);
            }));
        }

        // --- Animations domaine -> UI ---
        private async Task AnimatePlayFromPlayerToDepositAsync(Player player, Card card, CancellationToken ct)
        {
            try
            {
                DebugLog("   [ANIM] Enter AnimatePlay…");

                var img = LoadCardImage(card);
                if (img == null) { DebugLog("   [ANIM] No image"); return; }

                (Point start, Point end) = (Point.Empty, Point.Empty);
                try
                {
                    DebugLog("   [ANIM] Compute centers (UI_SYNC)...");
                    (start, end) = GetCenters_PlayToDeposit_UI(player);
                    DebugLog($"   [ANIM] Centers ok: start={start} end={end}");
                }
                catch (Exception ex)
                {
                    DebugLog($"   [ANIM ERR] GetCenters: {ex.Message}");
                }

                if (start == Point.Empty || end == Point.Empty)
                {
                    DebugLog("   [ANIM] Empty centers → fallback delay");
                    try { await Task.Delay(Math.Max(120, _delayMs / 2), ct); } catch { }
                    return;
                }

                int durationMs = Math.Max(260, Math.Min(900, (int)(_delayMs * 0.8)));
                DebugLog($"   [ANIM] Start anim ({durationMs}ms)…");

                // attend la vraie fin de l’anim
                await AnimateImageAsync(img, start, end, durationMs, ct);

                DebugLog("   [ANIM] Delay done");
            }
            catch (OperationCanceledException)
            {
                DebugLog("   [ANIM] Canceled");
            }
            catch (Exception ex)
            {
                DebugLog($"   [ANIM ERR] {ex.Message}");
            }
        }

        // surcharge pratique si un ancien appel sans token subsiste
        private Task AnimateDrawFromPileToCurrentAsync()
            => AnimateDrawFromPileToCurrentAsync(CancellationToken.None);

        // version attendante SANS gate : le gate est géré par le handler
        private async Task AnimateDrawFromPileToCurrentAsync(CancellationToken ct)
        {
            if (_drawStack.Image == null || _game?.CurrentPlayer == null) return;

            var (start, end) = GetCenters_DrawToHand_UI(_game.CurrentPlayer);
            if (start == Point.Empty || end == Point.Empty)
            {
                try { await Task.Delay(Math.Max(120, _delayMs / 2), ct); } catch { }
                return;
            }

            int durationMs = Math.Max(260, Math.Min(900, (int)(_delayMs * 0.8)));

            await AnimateImageAsync(_drawStack.Image!, start, end, durationMs, ct);
        }

        // Moteur d’animation unique (sans Timer, 100% async/await, sûr et robuste)
        private async Task AnimateImageAsync(Image img, Point start, Point end, int durationMs, CancellationToken ct)
        {
            if (img == null) return;
            durationMs = Math.Max(1, durationMs);

            PictureBox? pb = null;
            // Crée le sprite sur le thread UI
            await UI(() =>
            {
                pb = new PictureBox
                {
                    SizeMode  = PictureBoxSizeMode.Zoom,
                    Width     = 120,
                    Height    = 170,
                    BackColor = Color.Transparent,
                    Image     = img,
                    Tag       = "anim-sprite"
                };
                AnimHost.Controls.Add(pb);
                AnimHost.BringToFront();
                pb!.Left = start.X - pb.Width  / 2;
                pb!.Top  = start.Y - pb.Height / 2;
            });

            var sw = Stopwatch.StartNew();
            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    double t = Math.Min(1.0, sw.Elapsed.TotalMilliseconds / durationMs);
                    double ease = 0.5 - 0.5 * Math.Cos(Math.PI * t);

                    int x = (int)Math.Round(start.X + (end.X - start.X) * ease) - pb!.Width  / 2;
                    int y = (int)Math.Round(start.Y + (end.Y - start.Y) * ease) - pb!.Height / 2;

                    await UI(() =>
                    {
                        if (!pb.IsDisposed) pb.Location = new Point(x, y);
                    });

                    if (t >= 1.0) break;
                    await Task.Delay(15, ct); // cadence ~66 FPS
                }
            }
            finally
            {
                await UI(() =>
                {
                    if (pb != null && !pb.IsDisposed)
                    {
                        AnimHost.Controls.Remove(pb);
                        pb.Dispose();
                    }
                });
            }
        }

        // Calcule start/end STRICTEMENT sur le thread UI (évite tout cross-thread)
        private (Point start, Point end) GetCenters_PlayToDeposit_UI(Player player)
        {
            Point s = Point.Empty, e = Point.Empty;
            UI_SYNC(() =>
            {
                var from = GetHandHostFor(player);
                s = CenterOf(from, AnimHost);
                e = CenterOf(_depositStack.ImageBox, AnimHost);
            });
            return (s, e);
        }

        private (Point start, Point end) GetCenters_DrawToHand_UI(Player current)
        {
            // dimensions du sprite animé (mêmes que dans AnimateImageAsync)
            const int SPRITE_W = 120;
            const int SPRITE_H = 170;
            const int MARGIN   = 8;

            Point s = Point.Empty, e = Point.Empty;

            UI_SYNC(() =>
            {
                // Centre de départ : la pioche
                s = CenterOf(_drawStack.ImageBox, AnimHost);

                // Centre de l'hôte cible (main du joueur courant)
                var host = GetHandHostFor(current);
                e = CenterOf(host, AnimHost);
                if (e == Point.Empty) return;

                // Calcule un décalage pour atterrir près du bord "logique" de la main,
                // en laissant une marge et en tenant compte du demi-sprite.
                int dxEdge = Math.Max(0, host.Width  / 2 - SPRITE_W / 2 - MARGIN);
                int dyEdge = Math.Max(0, host.Height / 2 - SPRITE_H / 2 - MARGIN);

                if (ReferenceEquals(host, _rightHand))
                {
                    // Aller vers la droite (cartes empilées côté droit)
                    e.Offset(+dxEdge, 0);
                }
                else if (ReferenceEquals(host, _leftHand))
                {
                    // Aller vers la gauche
                    e.Offset(-dxEdge, 0);
                }
                else if (ReferenceEquals(host, _bottomHand))
                {
                    // Aller vers le bas
                    e.Offset(0, +dyEdge);
                }
                // Si un autre siège existait (haut), on ferait e.Offset(0, -dyEdge);
            });

            return (s, e);
        }

    }
}