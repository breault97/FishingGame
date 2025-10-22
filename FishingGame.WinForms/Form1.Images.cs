using System;
using System.IO;
using System.Drawing;
using FishingGame.Domain.Enums;
using FishingGame.Domain.Struct; // Card

namespace FishingGame.WinForms
{
    public partial class Form1
    {
        private Image? _cardBack;
        
        // Charge l'image d'une carte via CardView (mapping card_<suit>_<rank>.png)
        private Image? LoadCardImage(Card c)
        {
            string suit = (c.Color.Type /* ou c.Color.ColorType */) switch
            {
                COLOR_TYPE.CLUBS    => "clubs",
                COLOR_TYPE.DIAMONDS => "diamonds",
                COLOR_TYPE.HEARTS   => "hearts",
                COLOR_TYPE.SPADES   => "spades",
                _                   => "spades"
            };

            string rank = c.Value switch
            {
                CARD_VALUE.TWO   => "02",
                CARD_VALUE.THREE => "03",
                CARD_VALUE.FOUR  => "04",
                CARD_VALUE.FIVE  => "05",
                CARD_VALUE.SIX   => "06",
                CARD_VALUE.SEVEN => "07",
                CARD_VALUE.EIGHT => "08",
                CARD_VALUE.NINE  => "09",
                CARD_VALUE.TEN   => "10",
                CARD_VALUE.JACK  => "J",
                CARD_VALUE.QUEEN => "Q",
                CARD_VALUE.KING  => "K",
                CARD_VALUE.ACE   => "A",
                _                => "A"
            };

            var file = Path.Combine(AppContext.BaseDirectory, "Assets", "Cards",
                $"card_{suit}_{rank}.png");
            return File.Exists(file) ? Image.FromFile(file) : Images.CardEmpty;
        }

        private void LoadBackImage()
        {
            var backPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Cards", "card_back.png");
            _cardBack = File.Exists(backPath) ? Image.FromFile(backPath) : null;

            _drawStack.Image   = _cardBack; // dos pour la pioche
            _depositStack.Image = null;     // défausse vide au départ
        }

    }
}