namespace AveManiaBot.JsonData.Telegram;

public class TelegramChatData
{
    public string Name { get; set; }
    public string Type { get; set; }
    public long Id { get; set; }
    public List<Message> Messages { get; set; }
}

public class Message
{
    public long Id { get; set; }
    public string Type { get; set; }
    public DateTime Date { get; set; }
    public long DateUnixTime { get; set; }
        
    // For messages
    public string From { get; set; }
    public string FromId { get; set; }
    public string Text { get; set; }
    public List<TextEntity> TextEntities { get; set; }
        
    // For service messages
    public string Actor { get; set; }
    public string ActorId { get; set; }
    public string Action { get; set; }
    public string Title { get; set; }
    public List<string> Members { get; set; }

    // For photo-related actions
    public string Photo { get; set; }
    public int? PhotoFileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public class TextEntity
{
    public string Type { get; set; }
    public string Text { get; set; }
}