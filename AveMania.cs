public record AveMania(string Message, string Author, DateTime DateTime)
{
    public string Message { get; set; } = Message;
    public string Author { get; set; } = Author;
    public DateTime DateTime { get; set; } = DateTime;
}