using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FishingGame.Domain.Enums;   // COLOR_TYPE, CARD_VALUE
using FishingGame.Domain.Struct;  // Card
// Images.CardEmpty si tu as une image fallback
// using FishingGame.WinForms;  // si Images.CardEmpty est dans ce namespace

namespace FishingGame.WinForms
{
    /// <summary>
    /// Carte cliquable affichée via PNG: Assets/Cards/card_<suit>_<rank>.png
    /// </summary>
    public class CardView : PictureBox
    {
        public Card? Card { get; private set; }
        public bool IsPlayable { get; private set; }

        public CardView()
        {
            SizeMode  = PictureBoxSizeMode.Zoom;
            Width     = 90;
            Height    = 130;
            BackColor = Color.Transparent;
            Margin    = new Padding(4);
            Cursor    = Cursors.Default;
        }

        /// <summary>Lie la vue à une carte et charge l'image correspondante.</summary>
        public void Bind(Card card, bool isPlayable)
        {
            Card = card;
            IsPlayable = isPlayable;
            Cursor = isPlayable ? Cursors.Hand : Cursors.Default;

            // 1) Récupérer l'enum depuis le struct CardColor de la carte
            var suitEnum = card.Color.Type; // ou card.Color.ColorType selon ton struct

            // 2) Dossiers de couleur
            string suit = suitEnum switch
            {
                COLOR_TYPE.CLUBS    => "clubs",
                COLOR_TYPE.DIAMONDS => "diamonds",
                COLOR_TYPE.HEARTS   => "hearts",
                COLOR_TYPE.SPADES   => "spades",
                _                   => "spades"
            };

            // 3) Nom de valeur
            string rank = card.Value switch
            {
                CARD_VALUE.TWO   => "2",
                CARD_VALUE.THREE => "3",
                CARD_VALUE.FOUR  => "4",
                CARD_VALUE.FIVE  => "5",
                CARD_VALUE.SIX   => "6",
                CARD_VALUE.SEVEN => "7",
                CARD_VALUE.EIGHT => "8",
                CARD_VALUE.NINE  => "9",
                CARD_VALUE.TEN   => "10",
                CARD_VALUE.JACK  => "J",
                CARD_VALUE.QUEEN => "Q",
                CARD_VALUE.KING  => "K",
                CARD_VALUE.ACE   => "A",
                _                => "A"
            };

            // 4) Charger l'image
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Cards",
                                    $"card_{suit}_{rank}.png");

            Image = File.Exists(path)
                ? Image.FromFile(path)
                : Images.CardEmpty; // mets ton fallback ici
        }
    }
}
