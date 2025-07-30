using System.Data.SQLite;
using System.Text.Json;
using AveManiaBot.JsonData.Telegram;
using static AveManiaBot.AmConstants;

namespace AveManiaBot;

public class DbRepo
{
    private const string AmTableName = "ave_mania";
    private const string PenaltyTableName = "penalties";


    private const string CreateTableQuery =
        $"CREATE TABLE IF NOT EXISTS {AmTableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT, author TEXT, datetime DATETIME)";

    private const string CreatePenaltyTableQuery =
        $"CREATE TABLE IF NOT EXISTS {PenaltyTableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT, author TEXT, datetime DATETIME)";

    private const string InsertCommand =
        $"INSERT INTO {AmTableName} (message, author, datetime, messageId) VALUES (@message, @author, @datetime, @messageId)";

    private const string InsertPenaltyCommand =
        $"INSERT INTO {PenaltyTableName} (message, author, datetime) VALUES (@message, @author, @datetime)";

    const string UpdateMessageCommand = $"UPDATE {AmTableName} SET message = @message WHERE messageId = @messageId";

    private const string SelectQuery = $"SELECT * FROM {AmTableName}";
    private const string CountQuery = $"SELECT COUNT(*) FROM {AmTableName}";
    private const string SelectWhereQuery = $"SELECT * FROM {AmTableName} WHERE message = @message";
    private const string SelectWhereIdQuery = $"SELECT * FROM {AmTableName} WHERE id = @id";

    public void InitDataBase(bool initData)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(CreateTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SQLiteCommand(CreatePenaltyTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            if (!initData)
            {
                return;
            }

            Console.WriteLine("Importing data...");
            List<ChatData> chatData = LoadChatDataFromJsonFiles();
            ImportChatData(chatData, connection);
        }
    }

    /// <summary>
    /// Processes unprocessed Telegram messages by loading data from JSON files
    /// and importing the chat messages into the system.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to interact with the Telegram API.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    // public async Task ProcessTelegramMessages(ITelegramBotClient botClient, CancellationToken cancellationToken)
    // {
    //     Console.WriteLine("Importing data from unprocessed telegram messages...");
    //     var chatData = LoadTelegramChatDataFromJsonFiles();
    //     if (chatData != null) await ImportChatData(chatData.Messages, botClient, cancellationToken);
    // }

    // private async Task ImportChatData(List<AveManiaBot.JsonData.Telegram.Message> chatDataMessages, ITelegramBotClient botClient, 
    //     CancellationToken cancellationToken)
    // {
    //     var lastMessageDateTime = GetLastMessageDateTime();
    //     long unixTime = ((DateTimeOffset)lastMessageDateTime!).ToUnixTimeSeconds();
    //
    //     var messages = chatDataMessages.Where(m => long.Parse(m.DateUnixtime) > unixTime);
    //     foreach (AveManiaBot.JsonData.Telegram.Message m in messages)
    //     {
    //         // verifica se è già nel db
    //         string message = m.Text!;
    //
    //         bool isAm = Helpers.IsAveMania(message);
    //         if (!isAm) continue;
    //
    //         int? existingMessageId = CheckPenalty(message);
    //         if (existingMessageId.HasValue)
    //         {
    //             Console.WriteLine($"{DateTime.Now:u} Message already exists in the database. Issuing a penalty for message ID: {existingMessageId.Value}");
    //             if (m is { Text: not null, From: not null })
    //             {
    //                 Insert(new Penalty(m.Text, m.From, ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(), DateTime.Now));
    //                 await MessageHelper.SendPenaltyMessage(botClient, cancellationToken, m.From, m.Text, this, existingMessageId);
    //             }
    //         }
    //         else
    //         {
    //             // se non esiste inserisci una ave mania
    //             if (m.From != null)
    //             {
    //                 Insert(new AveMania(message, m.From, ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(), DateTime.Now, m.MessageId));
    //             }
    //             Console.WriteLine($"{DateTime.Now:u} Message does not exist in the database. Adding an AveMania message.");
    //         }
    //     }
    // }
    private TelegramChatData? LoadTelegramChatDataFromJsonFiles()
    {
        string jsonContent = File.ReadAllText("jsondata/result.json");
        var mappedObjects = JsonSerializer.Deserialize<TelegramChatData>(jsonContent);
        return mappedObjects;
    }

    private static void ImportChatData(List<ChatData> chatData, SQLiteConnection connection)
    {
        int i = 0;
        Console.WriteLine("Importing chat data...");
        Console.WriteLine($"{DateTime.Now:u} Found {chatData.Count} files to be imported");
        foreach (ChatData data in chatData)
        {
            Console.WriteLine($"{DateTime.Now:u} File {i++} of {chatData.Count} - {data.messages.Count} to be imported");
            foreach (var mess in data.messages)
            {
                if (Helpers.IsAveMania(mess.content))
                {
                    AveMania aveMania = new(mess.content, mess.sender_name, mess.timestamp_ms,
                        DateTime.Now, mess.message_id);
                    using var command = new SQLiteCommand(InsertCommand, connection);
                    command.Parameters.AddWithValue("@message", aveMania.Message);
                    command.Parameters.AddWithValue("@author", aveMania.Author);
                    command.Parameters.AddWithValue("@datetime", DateTime.Now);
                    command.ExecuteNonQuery();
                    Console.WriteLine($"{DateTime.Now:u} Message {mess.content} added");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now:u} Skipping message {mess.content}");
                }
            }
        }
    }

    private static List<ChatData> LoadChatDataFromJsonFiles()
    {
        List<ChatData> chatData = new List<ChatData>();
        foreach (string file in Directory.GetFiles("./JsonData"))
        {
            string jsonContent = File.ReadAllText(file);
            var mappedObjects = JsonSerializer.Deserialize<ChatData>(jsonContent);
            if (mappedObjects != null)
            {
                chatData.AddRange(mappedObjects);
            }
        }

        return chatData;
    }

    public AveMania? Find(int entryId)
    {
        using var connection = new SQLiteConnection(ConnectionString);
        connection.Open();
        using var command = new SQLiteCommand(SelectWhereIdQuery, connection);
        command.Parameters.AddWithValue("@id", entryId);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            DateTime.TryParse(reader["datetime"].ToString(), out var dateTime);
            int.TryParse(reader["messageId"].ToString(), out var messageId);

            return new AveMania(
                reader["message"].ToString() ?? string.Empty,
                reader["author"].ToString() ?? string.Empty,
                0,
                dateTime,
                messageId
            );
        }

        return null;
    }

    public List<AveMania> FindMessagesContaining(string searchText)
    {
        List<AveMania> results = new();
        using var connection = new SQLiteConnection(ConnectionString);
        connection.Open();
        string query = $"SELECT * FROM {AmTableName} WHERE message LIKE '%' || @searchText || '%'";
        using var command = new SQLiteCommand(query, connection);
        command.Parameters.AddWithValue("@searchText", searchText);
        using SQLiteDataReader? reader = command.ExecuteReader();
        while (reader.Read())
        {
            DateTime dateTime = DateTime.MinValue;
            if (DateTime.TryParse(reader["datetime"].ToString(), out var parsedDateTime))
            {
                dateTime = parsedDateTime;
            }

            int.TryParse(reader["messageid"].ToString(), out var messageId);

            results.Add(new AveMania(
                reader["message"].ToString() ?? string.Empty,
                reader["author"].ToString() ?? string.Empty,
                0,
                dateTime,
                messageId
            ));
        }

        return results;
    }

    /// <summary>
    /// Returns the DateTimes of the last N messages (including penalties) for a given author, ordered from newest to oldest.
    /// </summary>
    private List<DateTime> GetLastNMessageAndPenaltyDates(string author, int n)
    {
        var dates = new List<DateTime>();
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            string query = $@"
                SELECT datetime FROM {AmTableName} WHERE author = @Author
                UNION ALL
                SELECT datetime FROM {PenaltyTableName} WHERE author = @Author
                ORDER BY datetime DESC
                LIMIT @Limit
            ";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Author", author);
                command.Parameters.AddWithValue("@Limit", n);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (DateTime.TryParse(reader["datetime"].ToString(), out var dt))
                        {
                            dates.Add(dt);
                        }
                    }
                }
            }
        }

        return dates;
    }

    public (bool hasExceeded, int count, DateTime? date, double timeSpan) HasAuthorExceededLimit(string author,
        int limit, DateTime messageDateTime)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            string query = $@"
            SELECT 
                (SELECT COUNT(*) 
                 FROM {AmTableName} 
                 WHERE author = @Author AND datetime >= @StartDate) 
                +
                (SELECT COUNT(*) 
                 FROM {PenaltyTableName} 
                 WHERE author = @Author AND datetime >= @StartDate) AS totalCount";

            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Author", author);
                var startDate = messageDateTime.AddHours(-ActivityTimeSpanHours);
                command.Parameters.AddWithValue("@StartDate", startDate);

                Console.WriteLine($"{DateTime.Now:u} {DateTime.Now} - Looking for AMs from {startDate}");

                var result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int count))
                {
                    bool exceeded = count > limit;
                    double? timeSpan = null;
                    var last5Dates = GetLastNMessageAndPenaltyDates(author, 5);
                    if (last5Dates.Count >= 5)
                    {
                        timeSpan = (last5Dates[0] - last5Dates[4]).TotalHours;
                    }

                    if (exceeded)
                    {
                        Console.WriteLine($"{DateTime.Now:u} Author {author} has exceeded the limit of {limit} messages");
                        DateTime? oldestMessageDateTimeInTimeSpan = GetOldestMessageDateForAuthorInLastHours(author, ActivityTimeSpanHours, messageDateTime);
                        return (exceeded, count, oldestMessageDateTimeInTimeSpan?.AddHours(ActivityTimeSpanHours), timeSpan ?? 0);
                    }

                    return (exceeded, count, null, timeSpan ?? 0);
                }
            }
        }

        return (false, 0, null, 0);
    }

    public async Task<(bool hasExceeded, int count)> HasAuthorExceededPenalLimit(string author,
        DateTime messageDateTime)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            string query = $@"             
                SELECT COUNT(*) 
                 FROM {PenaltyTableName} 
                 WHERE author = @Author AND datetime >= @StartDate";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Author", author);
                command.Parameters.AddWithValue("@StartDate", messageDateTime.AddHours(-PenaltyHoursTimeSpan));
                var result = await command.ExecuteScalarAsync();
                if (result != null && int.TryParse(result.ToString(), out int count))
                {
                    bool exceeded = count >= PenaltyLimit;
                    if (exceeded)
                    {
                        Console.WriteLine(
                            $"Author {author} has exceeded the limit of {PenaltyLimit} penalties in the last {PenaltyHoursTimeSpan} hours");
                        return (exceeded, count);
                    }

                    return (exceeded, count);
                }
            }
        }

        return (false, 0);
    }


    private DateTime? GetOldestMessageDateForAuthorInLastHours(string author, int hours, DateTime messageDateTime)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();

            string query = $@"
                SELECT MIN(datetime)
                FROM {AmTableName}
                WHERE author = @Author AND datetime >= @StartDate";

            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Author", author);
                command.Parameters.AddWithValue("@StartDate", messageDateTime.AddHours(-hours));
                var result = command.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                {
                    if (DateTime.TryParse(result.ToString(), out DateTime oldestDate))
                    {
                        return oldestDate;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a message with the specified text already exists in the database
    /// and retrieves its corresponding identifier.
    /// </summary>
    /// <param name="messageText">The text of the message to check for existence in the database.</param>
    /// <returns>
    /// The identifier of the existing message if found, or null if the message is not present in the database.
    /// </returns>
    public int? CheckPenalty(string messageText)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            try
            {
                connection.Open();
                using (var command = new SQLiteCommand(SelectWhereQuery, connection))
                {
                    command.Parameters.AddWithValue("@message", messageText);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse(reader["id"].ToString() ?? "-1");
                        }
                    }
                }
            }
            catch (SQLiteException ex) when (ex.ErrorCode == (int)SQLiteErrorCode.Busy)
            {
                // Handle database concurrency issues (e.g., database is locked)
                Console.WriteLine("Database is currently busy. Please try again later.");
                throw;
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Console.WriteLine($"{DateTime.Now:u} An error occurred: {ex.Message}");
            }
        }

        return null;
    }

    public static void Insert(AveMania aveMania)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(InsertCommand, connection))
            {
                command.Parameters.AddWithValue("@message", aveMania.Message);
                command.Parameters.AddWithValue("@author", aveMania.Author);
                command.Parameters.AddWithValue("@datetime", aveMania.DateTime);
                command.Parameters.AddWithValue("@messageId", aveMania.MessageId);
                command.ExecuteNonQuery();
            }
        }
    }

    public void Insert(Penalty penalty)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(InsertPenaltyCommand, connection))
            {
                command.Parameters.AddWithValue("@message", penalty.Message);
                command.Parameters.AddWithValue("@author", penalty.Author);
                command.Parameters.AddWithValue("@datetime", penalty.DateTime);
                command.ExecuteNonQuery();
            }
        }
    }

    public long Count()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(CountQuery, connection))
            {
                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt64(result) : 0;
            }
        }
    }

    public void DeleteDuplicates()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();

            // Deleting duplicate rows while keeping the one with the smallest id
            string deleteDuplicatesQuery = $@"
                DELETE FROM {AmTableName}
                WHERE id NOT IN (
                    SELECT MAX(id)
                    FROM {AmTableName}
                    GROUP BY message
                )";

            using (var command = new SQLiteCommand(deleteDuplicatesQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }


    public List<AveMania> GetRandom(int n)
    {
        var randomAveManias = new List<AveMania>();
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();

            string query = $@"
            SELECT * FROM {AmTableName}
            ORDER BY RANDOM()
            LIMIT @limit";

            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@limit", n);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime.TryParse(reader["datetime"].ToString(), out var dateTime);

                        int.TryParse(reader["messageId"].ToString(), out var messageId);
                        randomAveManias.Add(new AveMania(
                            reader["message"].ToString() ?? string.Empty,
                            reader["author"].ToString() ?? string.Empty,
                            0,
                            dateTime,
                            messageId
                        ));
                    }
                }
            }
        }

        return randomAveManias;
    }

    public Dictionary<string, int> GetDaysSinceLastMessageForAllAuthors()
    {
        Dictionary<string, int> daysSinceLastMessages = new Dictionary<string, int>();

        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();

            string query = $@"
            SELECT author, MAX(datetime) AS lastMessageDate 
            FROM {AmTableName} 
            GROUP BY author order by MAX(datetime) asc";

            using (var command = new SQLiteCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (DateTime.TryParse(reader["lastMessageDate"].ToString(), out var lastMessageDate))
                        {
                            int daysSinceLastMessage = (DateTime.Now - lastMessageDate).Days;
                            string author = reader["author"].ToString() ?? string.Empty;
                            daysSinceLastMessages[author] = daysSinceLastMessage;
                        }
                    }
                }
            }
        }

        return daysSinceLastMessages;
    }

    public List<AveMania> GetLast(int i)
    {
        var results = new List<AveMania>();
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            string query = $@"SELECT * FROM {AmTableName} WHERE datetime >= @fromTime ORDER BY datetime DESC";

            using (var command = new SQLiteCommand(query, connection))
            {
                DateTime fromTime = DateTime.Now.AddHours(-i);
                command.Parameters.AddWithValue("@fromTime", fromTime);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime.TryParse(reader["datetime"].ToString(), out var dateTime);
                        int.TryParse(reader["messageId"].ToString(), out var messageId);
                        results.Add(new AveMania(
                            reader["message"].ToString() ?? string.Empty,
                            reader["author"].ToString() ?? string.Empty,
                            0,
                            dateTime,
                            messageId
                        ));
                    }
                }
            }
        }

        return results;
    }

    public Dictionary<string, int> GetPenaltiesForAllAuthors()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            string query = $@"
            SELECT author, COUNT(*) AS penaltyCount
            FROM {PenaltyTableName}
            GROUP BY author ORDER BY count(*) DESC";

            using (var command = new SQLiteCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    var penalties = new Dictionary<string, int>();

                    while (reader.Read())
                    {
                        string author = reader["author"].ToString() ?? string.Empty;
                        int penaltyCount = Convert.ToInt32(reader["penaltyCount"]);
                        penalties[author] = penaltyCount;
                    }

                    return penalties;
                }
            }
        }
    }

    public void Execute(string messageText)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            var cmd = messageText.Replace("/sqlcmd ", "");
            using (var command = new SQLiteCommand(cmd, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("SQL command executed successfully.");
    }

    public async Task<Dictionary<string, int>> GetAveManiaCountPerAuthor()
    {
        Dictionary<string, int> authorCounts = new();

        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            string query = $@"
            SELECT author, COUNT(*) AS messageCount
            FROM {AmTableName}
            WHERE datetime >= @startDate
            GROUP BY author";

            using (var command = new SQLiteCommand(query, connection))
            {
                DateTime startDate = new DateTime(2025, 1, 15);
                command.Parameters.AddWithValue("@startDate", startDate);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string author = reader["author"].ToString() ?? string.Empty;
                        author = author.Replace(' ', '~');
                        int count = Convert.ToInt32(reader["messageCount"]);
                        authorCounts[author] = count;
                    }
                }
            }
        }

        return authorCounts;
    }


    public Dictionary<string, double> GetPenaltiesRatioStats()
    {
        Dictionary<string, double> authorRatios = new Dictionary<string, double>();

        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            string query = $@"
            SELECT 
                a.author, 
                COUNT(a.id) AS aveManiaCount, 
                IFNULL(p.penaltyCount, 0) AS penaltyCount
            FROM
                (SELECT * FROM {AmTableName} WHERE datetime >= '2025-01-01') a
            LEFT JOIN 
                (SELECT author, COUNT(*) AS penaltyCount FROM {PenaltyTableName} GROUP BY author) p
            ON a.author = p.author
            GROUP BY a.author";

            using (var command = new SQLiteCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string author = reader["author"].ToString() ?? string.Empty;
                        int aveManiaCount = Convert.ToInt32(reader["aveManiaCount"]);
                        int penaltyCount = Convert.ToInt32(reader["penaltyCount"]);

                        if (aveManiaCount < 10)
                        {
                            authorRatios[author] = 0;
                        }
                        else
                        {
                            double ratio = aveManiaCount > 0 ? penaltyCount / (double)aveManiaCount : 0;
                            authorRatios[author] = ratio;
                        }
                    }
                }
            }
        }

        return authorRatios.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public DateTime? GetLastMessageDateTime()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();

            string query = $@"SELECT MAX(datetime) AS lastMessageDateTime FROM {AmTableName}";

            using (var command = new SQLiteCommand(query, connection))
            {
                var result = command.ExecuteScalar();

                if (result != DBNull.Value && result != null)
                {
                    if (DateTime.TryParse(result.ToString(), out DateTime lastMessageDateTime))
                    {
                        return lastMessageDateTime;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Update the message column with the value of newAm where messageId matches the parameter
    /// </summary>
    /// <param name="messageId">The id of the message to be updated</param>
    /// <param name="newMessage">The new value to update the message column with</param>
    public async Task Update(int messageId, string newMessage)
    {
        await using var connection = new SQLiteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SQLiteCommand(UpdateMessageCommand, connection);
        command.Parameters.AddWithValue("@message", newMessage);
        command.Parameters.AddWithValue("@messageId", messageId);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Get the original text from am_table based on message id
    /// </summary>
    /// <param name="messageId"></param>
    /// <returns></returns>
    public string GetOriginalText(int messageId)
    {
        using var connection = new SQLiteConnection(ConnectionString);
        connection.Open();

        string query = $"SELECT message FROM {AmTableName} WHERE messageId = @messageId";
        using var command = new SQLiteCommand(query, connection);
        command.Parameters.AddWithValue("@messageId", messageId);

        var am = command.ExecuteScalar();
        return am?.ToString() ?? string.Empty;
    }

    public void EnsureSchemaAndUpdate()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();

            // Check if the `messageId` column exists, if not, add it
            var checkMessageIdColumnQuery = $"PRAGMA table_info({AmTableName});";
            using (var command = new SQLiteCommand(checkMessageIdColumnQuery, connection))
            using (var reader = command.ExecuteReader())
            {
                bool hasMessageIdColumn = false;
                while (reader.Read())
                {
                    if (reader["name"].ToString() == "messageId")
                    {
                        hasMessageIdColumn = true;
                        break;
                    }
                }

                if (!hasMessageIdColumn)
                {
                    var addMessageIdColumnQuery = $"ALTER TABLE {AmTableName} ADD COLUMN messageId INTEGER;";
                    using (var addColumnCommand = new SQLiteCommand(addMessageIdColumnQuery, connection))
                    {
                        addColumnCommand.ExecuteNonQuery();
                    }
                }
            }

            // Check if the `score` column exists, if not, add it
            using (var command = new SQLiteCommand(checkMessageIdColumnQuery, connection))
            using (var reader = command.ExecuteReader())
            {
                bool hasScoreColumn = false;
                while (reader.Read())
                {
                    if (reader["name"].ToString() == "score")
                    {
                        hasScoreColumn = true;
                        break;
                    }
                }

                if (!hasScoreColumn)
                {
                    var addScoreColumnQuery = $"ALTER TABLE {AmTableName} ADD COLUMN score INTEGER;";
                    using (var addColumnCommand = new SQLiteCommand(addScoreColumnQuery, connection))
                    {
                        addColumnCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}