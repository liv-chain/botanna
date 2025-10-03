namespace AveManiaBot.Model;

public class Message
{
    public string sender_name { get; set; }
    public long timestamp_ms { get; set; }
    public string content { get; set; }
    public int message_id { get; set; }
}

public class ChatData
{
    public List<Message> messages { get; set; }
}

