using System.Text.Json.Serialization;
using AveManiaBot.Helper;

namespace AveManiaBot.JsonData.Telegram;

public class TelegramChatData
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
    
    [JsonPropertyName("from")]
    public string? From { get; set; }   
    
    [JsonPropertyName("text")]
    [JsonConverter(typeof(TextFieldConverter))]
    public string? Text { get; set; }

    [JsonPropertyName("message_id")] 
    public int MessageId { get; set; }
}
