namespace EtTilTi.Shared
{
    public record Session(
        string Creator,
        string Question, 
        string SessionName,
        int CreatorGuessValue)
    {
        public int PlayerGuessValue { get; set; }
        public bool GuessCorrect => PlayerGuessValue == CreatorGuessValue;

        public bool HasGuess => PlayerGuessValue != default;
    }
}
