using System.Diagnostics;
using FishingGame.Domain.Class;
using FishingGame.Domain.Enums;
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
                HideColorOverlay(); // on masque l’overlay dès que la défausse change
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

                (Point start, Point end) = GetCenters_PlayToDeposit_UI(player);
                if (start == Point.Empty || end == Point.Empty)
                {
                    DebugLog("   [ANIM] Empty centers → fallback delay");
                    try { await Task.Delay(Math.Max(120, _delayMs / 2), ct); } catch { }
                    return;
                }

                var durationMs = Math.Max(260, Math.Min(900, (int)(_delayMs * 0.8)));
                DebugLog($"   [ANIM] Start anim ({durationMs}ms)…");

                // Si ce n'est pas le joueur humain, on démarre avec l'image du dos de la carte
                // et on la retourne en cours d'animation.
                var isHuman = _humanPlayer != null && player == _humanPlayer;
                Image startImg = isHuman ? img : (_cardBack ?? img);
                Image? flipImg = isHuman ? null : img;

                // On passe flipImg en paramètre pour retourner la carte en cours d’animation
                await AnimateImageAsync(startImg, start, end, durationMs, ct, flipImg);

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
        // Ajoute la possibilité de retourner la carte en cours d’animation via le paramètre flipImage.
        private async Task AnimateImageAsync(
            Image img,
            Point start,
            Point end,
            int durationMs,
            CancellationToken ct,
            Image? flipImage = null)
        {
            if (img == null) return;
            durationMs = Math.Max(1, durationMs);

            PictureBox? pb = null;
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
                // Place le sprite au-dessus des piles (défausse incluse)
                pb.BringToFront();
                pb.Left = start.X - pb.Width  / 2;
                pb.Top  = start.Y - pb.Height / 2;
            });

            var sw = Stopwatch.StartNew();
            var flipApplied = false;
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
                        if (!pb.IsDisposed)
                        {
                            pb.Location = new Point(x, y);
                            // à mi-chemin, si flipImage est fourni et pas encore appliqué, on retourne la carte
                            if (!flipApplied && flipImage != null && t >= 0.5)
                            {
                                pb.Image = flipImage;
                                flipApplied = true;
                            }
                        }
                    });

                    if (t >= 1.0) break;
                    await Task.Delay(15, ct);
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
                var host = GetHandHostFor(player);
                s = CenterOf(host, AnimHost);
                e = CenterOf(_depositStack.ImageBox, AnimHost);

                if (s != Point.Empty && host != null)
                {
                    // Dimensions du sprite animé (identiques à AnimateImageAsync)
                    const int SPRITE_W = 120;
                    const int SPRITE_H = 170;
                    const int MARGIN   = 8;

                    int dxEdge = Math.Max(0, host.Width  / 2 - SPRITE_W / 2 - MARGIN);
                    int dyEdge = Math.Max(0, host.Height / 2 - SPRITE_H / 2 - MARGIN);

                    // Départ au bord logique de la main en fonction de sa position
                    if (ReferenceEquals(host, _bottomHand))
                    {
                        // La défausse est au-dessus de la main du bas → partir du bord supérieur
                        s.Offset(0, -dyEdge);
                    }
                    else if (ReferenceEquals(host, _leftHand))
                    {
                        // La défausse est à droite de la main gauche → partir du bord droit
                        s.Offset(+dxEdge, 0);
                    }
                    else if (ReferenceEquals(host, _rightHand))
                    {
                        // La défausse est à gauche de la main droite → partir du bord gauche
                        s.Offset(-dxEdge, 0);
                    }
                }
            });
            return (s, e);
        }

        private (Point start, Point end) GetCenters_DrawToHand_UI(Player current)
        {
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
                if (e == Point.Empty || host == null) return;

                int dxEdge = Math.Max(0, host.Width  / 2 - SPRITE_W / 2 - MARGIN);
                int dyEdge = Math.Max(0, host.Height / 2 - SPRITE_H / 2 - MARGIN);

                // Arrivée au bord logique de la main vers la pioche
                if (ReferenceEquals(host, _bottomHand))
                {
                    // La pioche est au-dessus de la main du bas → viser le bord supérieur
                    e.Offset(0, -dyEdge);
                }
                else if (ReferenceEquals(host, _leftHand))
                {
                    // La pioche est à droite de la main gauche → viser le bord droit
                    e.Offset(+dxEdge, 0);
                }
                else if (ReferenceEquals(host, _rightHand))
                {
                    // La pioche est à gauche de la main droite → viser le bord gauche
                    e.Offset(-dxEdge, 0);
                }
            });

            return (s, e);
        }
        
        // Charge et affiche l’icône correspondant à la couleur choisie
        private void ShowColorOverlay(COLOR_TYPE color)
        {
            try
            {
                var fileName = color switch
                {
                    COLOR_TYPE.CLUBS    => "clubs.png",
                    COLOR_TYPE.DIAMONDS => "diamonds.png",
                    COLOR_TYPE.HEARTS   => "hearts.png",
                    COLOR_TYPE.SPADES   => "spade.png",
                    _ => ""
                };
                if (string.IsNullOrEmpty(fileName)) return;
                
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Color", fileName);
                if (!File.Exists(path)) return;
                    
                // Libère l'image précédente pour éviter des verrous sur fichier
                _colorOverlayImg?.Dispose();
                _colorOverlayImg = Image.FromFile(path);
                _colorOverlayVisible = true;
                _depositStack.ImageBox.Invalidate(); // déclenche Paint pour dessiner l’icône
            }
            catch { /* ignore */ }
        }

        // Masque l’icône et libère l’image
        private void HideColorOverlay()
        {
            _colorOverlayVisible = false;
            if (_colorOverlayImg != null)
            {
                _colorOverlayImg.Dispose();
                _colorOverlayImg = null;
            }
            _depositStack.ImageBox.Invalidate();
        }
    }
}