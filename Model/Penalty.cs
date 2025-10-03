namespace AveManiaBot.Model;

public record Penalty(string Message, string Author, long TimeStamp, DateTime DateTime)
{
    public string Message { get; set; } = Message;
    public string Author { get; set; } = Author;
    
    public long TimeStamp { get; set; } = TimeStamp;
    public DateTime DateTime { get; set; } = DateTime;

    public override string ToString()
    {
        return Message + " - " + Author + " - " + DateTime.ToString("dd-MM-yyyy");
    }
}