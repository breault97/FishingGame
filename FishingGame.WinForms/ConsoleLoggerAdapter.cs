using System;
using System.Windows.Forms;
using FishingGame.Domain.Interfaces;
using FishingGame.Domain.Events;

namespace FishingGame.WinForms
{
    internal sealed class ConsoleLoggerAdapter
    {
        private readonly RichTextBox _rtb;
        private readonly Label _counts;
        private readonly Label _sens;

        public ConsoleLoggerAdapter(RichTextBox rtb, Label counts, Label sens, Func<int> _)
        {
            _rtb = rtb; _counts = counts; _sens = sens;
        }

        public void Wire(IFishingGameObserver game)
        {
            game.Info += OnInfo;
            game.TurnPlayed += OnTurnPlayed;
            game.HaveDrawCards += OnDraw;
        }
        public void Unwire(IFishingGameObserver game)
        {
            game.Info -= OnInfo;
            game.TurnPlayed -= OnTurnPlayed;
            game.HaveDrawCards -= OnDraw;
        }

        private void Append(string line)
        {
            if (_rtb.IsDisposed) return;
            if (_rtb.InvokeRequired) { _rtb.BeginInvoke(new Action(() => Append(line))); return; }
            _rtb.SelectionColor = System.Drawing.Color.Gainsboro;
            _rtb.AppendText(line + "\n");
            _rtb.ScrollToCaret();
        }

        private static T? Get<T>(object obj, params string[] names)
        {
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n);
                if (p != null && p.PropertyType == typeof(T)) return (T?)p.GetValue(obj);
            }
            return default;
        }

        private void OnInfo(object? s, InfoEventArgs e) => Append($"[INFO] {e.Message}");

        private void OnTurnPlayed(object? s, TurnEventArgs e)
        {
            var player = Get<object>(e, "Player", "PlayerInfo")?.ToString() ?? "<?>"; // tolérant
            var played = Get<object>(e, "PlayedCard", "Card")?.ToString() ?? "?";
            var top    = Get<object>(e, "TopOfDeposit", "DepositTop", "TopCard")?.ToString() ?? "?";
            var sens   = Get<int?>(e, "Sens", "Direction", "Spin") == 1 ? "Sens: ↻ horaire" : "Sens: ↺ antihoraire";

            Append($"[TURN] {player} joue {played} (top={top})");
            _sens.BeginInvoke(new Action(() => _sens.Text = sens));
        }

        private void OnDraw(object? s, DrawEventArgs e)
        {
            var draw    = Get<int?>(e, "DrawCount", "DeckCount", "DrawPileCount") ?? 0;
            var deposit = Get<int?>(e, "DepositCount", "DiscardCount", "DepositPileCount") ?? 0;
            _counts.BeginInvoke(new Action(() => _counts.Text = $"Pioche: {draw} | Dépôt: {deposit}"));
        }
    }
}
