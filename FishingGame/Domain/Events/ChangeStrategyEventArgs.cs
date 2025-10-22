namespace FishingGame.Domain.Events
{
    public class ChangeStrategyEventArgs : EventArgs
    {
        public int Sens { get; set; }

        public ChangeStrategyEventArgs(int Sens)
        {
            this.Sens = Sens;
        }
    }
}
