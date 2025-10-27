using System;
using FishingGame.Domain.Class;
using FishingGame.Domain.Struct;

namespace FishingGame.Domain.Events
{
    public class HumanTurnEventArgs : EventArgs
    {
        public Player Player { get; set; } = default!;
        public Card? TopCard { get; set; }
        public int PendingDraw { get; set; }
    }
}