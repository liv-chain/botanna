namespace AveManiaBot.Exceptions;

public class PorcodioException(string msg, DateTime banDate, int days) : Exception(msg)
{
    public DateTime BanDate { get; } = banDate;
    public int Days { get; } = days;
}