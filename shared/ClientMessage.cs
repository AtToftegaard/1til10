using System.Text.Json.Serialization;

namespace EtTilTi.Signalr.Messages
{
    public record ClientMessage
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Message")]
        public string Message { get; set; }
    }
}
