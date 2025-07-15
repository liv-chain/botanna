namespace AveManiaBot;

public static class Helpers
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

        // Check if the string contains any emoji
        foreach (char c in input)
        {
            if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.OtherSymbol ||
                (c >= 0x1F600 && c <= 0x1F64F) || // Emoticons
                (c >= 0x1F300 && c <= 0x1F5FF) || // Miscellaneous Symbols and Pictographs
                (c >= 0x1F680 && c <= 0x1F6FF) || // Transport and Map Symbols
                (c >= 0x2600 && c <= 0x26FF) || // Miscellaneous Symbols
                (c >= 0x2700 && c <= 0x27BF)) // Dingbats
            {
                return false;
            }
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