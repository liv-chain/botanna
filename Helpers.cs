namespace AveManiaBot;

public class Helpers
{
    public static bool IsAveMania(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (input.Length < 3)
        {
            return false;
        }
        
        if (input.All(c => !char.IsLetter(c)))
        {
            return false;
        }

        return input.All(c => char.IsUpper(c) || c == ' ' || c == '\'' || c == '-' || !char.IsLetter(c));
    }
    
    public static string GetArgument(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText) || !messageText.Contains(' '))
        {
            return string.Empty;
        }

        return messageText.Substring(messageText.IndexOf(' ') + 1);
    }
}