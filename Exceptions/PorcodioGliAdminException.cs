namespace AveManiaBot.Exceptions;

public class PorcodioGliAdminException(string msg, DateTime banDate, int days) : Exception(msg)
{
    public DateTime BanDate { get; } = banDate;
    public int Days { get; } = days;
}