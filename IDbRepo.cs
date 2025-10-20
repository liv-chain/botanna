using AveManiaBot.Model;
using Telegram.Bot;

namespace AveManiaBot;

public interface IDbRepo
{
    void InitTables(bool initData);

    /// <summary>
    /// Processes unprocessed Telegram messages by loading data from JSON files
    /// and importing the chat messages into the system.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<int> ProcessTelegramMessages(ITelegramBotClient botClient, CancellationToken cancellationToken);

    AveMania? Find(int entryId);
    List<AveMania> FindMessagesContaining(string searchText);

    (bool hasExceeded, int count, DateTime? date, double timeSpan) HasAuthorExceededLimit(string author,
        int limit, DateTime messageDateTime);

    Task<(bool hasExceeded, int count)> HasAuthorExceededPenalLimit(string author,
        DateTime messageDateTime);

    /// <summary>
    /// Checks if a message with the specified text already exists in the database
    /// and retrieves its corresponding identifier.
    /// </summary>
    /// <param name="messageText">The text of the message to check for existence in the database.</param>
    /// <returns>
    /// The identifier of the existing message if found, or null if the message is not present in the database.
    /// </returns>
    int? CheckPenalty(string messageText);

    int Insert(AveMania aveMania);
    void Insert(Penalty penalty);
    long Count();
    void DeleteDuplicates();
    List<AveMania> GetRandom(int n);
    Dictionary<string, int> GetDaysSinceLastMessageForAllAuthors();
    List<AveMania> GetLast(int i);
    Dictionary<string, int> GetPenaltiesForAllAuthors();
    void Execute(string messageText);
    Task<Dictionary<string, int>> GetAveManiaCountPerAuthor();
    Dictionary<string, double> GetPenaltiesRatioStats();
    DateTime? GetLastMessageDateTime();

    /// <summary>
    /// Update the message column with the value of newAm where messageId matches the parameter
    /// </summary>
    /// <param name="messageId">The id of the message to be updated</param>
    /// <param name="newMessage">The new value to update the message column with</param>
    Task Update(int messageId, string newMessage);

    /// <summary>
    /// Get the original text from am_table based on message id
    /// </summary>
    /// <param name="messageId"></param>
    /// <returns></returns>
    string GetOriginalText(int messageId);

    void EnsureSchemaAndUpdate();
    List<BotannaRequest> GetBotannaRequests(string author);
    void InsertBotannaRequest(BotannaRequest botannaRequest);
}