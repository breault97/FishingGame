namespace FishingGame.Attributes
{
    /// <summary>
    /// Ajout de l'attribut Score pour l'enum CARD_VALUE
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ScoreAttribute : Attribute
    {
        public int Score { get; }
        public ScoreAttribute(int score) => this.Score = score;
    }
}