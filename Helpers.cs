namespace AveManiaBot;

public class Helpers
{
    /// <summary>
    /// Checks if a string contains only capital letters and spaces.
    /// </summary>
    /// <param name="input">The string to check.</param>
    /// <returns>True if the string contains only capital letters and spaces; otherwise, false.</returns>
    public static bool ContainsOnlyCapitalsAndSpaces(string input)
    {
        return input.All(c => char.IsUpper(c) || c == ' ' || c == '\'');
    }
}