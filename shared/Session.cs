using System.Text.Json.Serialization;

namespace EtTilTi.Shared
{
    public record Session(
        string Creator,
        string Question, 
        string SessionName,
        int CreatorGuessValue)
    {
        public int PlayerGuessValue { get; set; }
        [JsonIgnore]
        public bool GuessCorrect => PlayerGuessValue == CreatorGuessValue;
        [JsonIgnore]
        public bool HasGuess => PlayerGuessValue != default;
    }
}
