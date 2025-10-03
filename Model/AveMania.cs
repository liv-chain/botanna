namespace AveManiaBot.Model;

public record AveMania(string Message, string Author, long TimeStamp, DateTime DateTime, int? MessageId)
{
    public string Message { get; set; } = Message;
    public string Author { get; set; } = Author;
    
    public long TimeStamp { get; set; } = TimeStamp;
    public DateTime DateTime { get; set; } = DateTime;
    public int? MessageId { get; set; } = MessageId;

    public override string ToString()
    {
        return $"{Message} - {Author} - {DateTime:dd-MM-yyyy}";
    }
}