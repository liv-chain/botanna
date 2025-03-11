using System.Text.Json.Serialization;

namespace AveManiaBot.JsonData.Telegram;

public class Root
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; }
}

public class Message
{    
    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }
    
    [JsonPropertyName("date_unixtime")]
    public string? DateUnixtime { get; set; }
    
    [JsonPropertyName("actor")]
    public string? Actor { get; set; }   
    
    [JsonPropertyName("text")]
    [JsonConverter(typeof(TextFieldConverter))]
    public string? Text { get; set; }
    
}
