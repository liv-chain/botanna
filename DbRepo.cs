using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AveManiaBot.JsonData.Telegram;
using Telegram.Bot;

namespace AveManiaBot;

public class DbRepo
{
    private const string DbPath = "ave_mania.db";
    private const string ConnectionString = $"Data Source={DbPath};Version=3;";
    private const string AmTableName = "ave_mania";
    private const string PenaltyTableName = "penalties";
    private const int Hours = 12; // Number of hours to check for author exceeding limit

    private const string CreateTableQuery =
        $"CREATE TABLE IF NOT EXISTS {AmTableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT, author TEXT, datetime DATETIME)";

    private const string CreatePenaltyTableQuery =
        $"CREATE TABLE IF NOT EXISTS {PenaltyTableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT, author TEXT, datetime DATETIME)";

    private const string InsertQuery =
        $"INSERT INTO {AmTableName} (message, author, datetime) VALUES (@message, @author, @datetime)";

    private const string InsertPenaltyQuery =
        $"INSERT INTO {PenaltyTableName} (message, author, datetime) VALUES (@message, @author, @datetime)";

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

    public async Task TelegramBonificone(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId)
    {
        Console.WriteLine("Importing data...");
        Root chatData = LoadTelegramChatDataFromJsonFiles();
        await ImportChatData(chatData.Messages, botClient, cancellationToken, chatId);
    }

    private async Task ImportChatData(List<AveManiaBot.JsonData.Telegram.Message> chatDataMessages, ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId)
    {
        var lastMessageDateTime = GetLastMessageDateTime();
        long unixTime = ((DateTimeOffset)lastMessageDateTime!).ToUnixTimeSeconds();

        var messages = chatDataMessages.Where(m => long.Parse(m.DateUnixtime) > unixTime);
        foreach (AveManiaBot.JsonData.Telegram.Message m in messages)
        {
            // verifica se è già nel db
            string message = m.Text;

            bool isAm = Helpers.IsAveMania(message);
            if (!isAm) continue;

            int? existingMessageId = Check(message);
            if (existingMessageId.HasValue)
            {
                // se esiste invia una multa
                Console.WriteLine($"Message already exists in the database. Issuing a penalty for message ID: {existingMessageId.Value}");
                Add(new Penalty(m.Text, m.Actor, ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(), DateTime.Now));
                await SendPenaltyMessage(botClient, cancellationToken, chatId, m.Actor, m.Text, this, existingMessageId);
            }
            else
            {
                // se non esiste inserisci una ave mania
                Console.WriteLine($"Message does not exist in the database. Adding an AveMania message.");
                // Add(new AveMania(message, m.From, m.DateUnixTime, DateTime.Now));
            }
        }

    }

    private static async Task<string> SendPenaltyMessage(ITelegramBotClient botClient,
        CancellationToken cancellationToken,
        long chatId, string senderName, string messageText, DbRepo repo, [DisallowNull] int? originalAveManiaId)
    {
        AveMania? am = repo.Find(originalAveManiaId.Value);

        string text =
            $"\ud83d\udc6e\u200d\u2642\ufe0f MULTA \u26a0\ufe0f per {senderName}! {messageText} era già stato scritto da {am?.Author} il {am?.DateTime:dd-MM-yyyy} \ud83d\udc6e\u200d\u2640\ufe0f";

        await botClient.SendMessage(
            chatId: chatId,
            text,
            cancellationToken: cancellationToken).ConfigureAwait(false);


        return text;
    }


    private Root LoadTelegramChatDataFromJsonFiles()
    {
        string jsonContent = File.ReadAllText("jsondata/result.json");
        var mappedObjects = JsonSerializer.Deserialize<Root>(jsonContent);
        return mappedObjects;
    }

    private static void ImportChatData(List<ChatData> chatData, SQLiteConnection connection)
    {
        int i = 0;
        Console.WriteLine("Importing chat data...");
        Console.WriteLine($"Found {chatData.Count} files to be imported");
        foreach (ChatData data in chatData)
        {
            Console.WriteLine($"File {i++} of {chatData.Count} - {data.messages.Count} to be imported");
            foreach (var mess in data.messages)
            {
                if (Helpers.IsAveMania(mess.content))
                {
                    AveMania aveMania = new(mess.content, mess.sender_name, mess.timestamp_ms,
                        DateTime.Now);
                    using var command = new SQLiteCommand(InsertQuery, connection);
                    command.Parameters.AddWithValue("@message", aveMania.Message);
                    command.Parameters.AddWithValue("@author", aveMania.Author);
                    command.Parameters.AddWithValue("@datetime", DateTime.Now);
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Message {mess.content} added");
                }
                else
                {
                    Console.WriteLine($"Skipping message {mess.content}");
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
            return new AveMania(
                reader["message"].ToString() ?? string.Empty,
                reader["author"].ToString() ?? string.Empty,
                0,
                dateTime
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

            results.Add(new AveMania(
                reader["message"].ToString() ?? string.Empty,
                reader["author"].ToString() ?? string.Empty,
                0,
                dateTime
            ));
        }

        return results;
    }

    public (bool hasExceeded, int count, DateTime?) HasAuthorExceededLimit(string author, int limit)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            string query = $@"
                SELECT COUNT(*)
                FROM {AmTableName}
                WHERE author = @Author AND datetime >= @StartDate";

            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Author", author);
                command.Parameters.AddWithValue("@StartDate", DateTime.Now.AddHours(-Hours));
                var result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int count))
                {
                    bool exceeded = count > limit;
                    if (exceeded)
                    {
                        Console.WriteLine($"Author {author} has exceeded the limit of {limit} messages");
                        DateTime? oldestMessageDateTimeInTimeSpan = GetOldestMessageDateForAuthorInLastHours(author, Hours);
                        if (oldestMessageDateTimeInTimeSpan != null) Console.WriteLine($"Last message date: {oldestMessageDateTimeInTimeSpan.Value:dd/MM/yyyy HH:mm:ss}");
                        return (exceeded, count, oldestMessageDateTimeInTimeSpan?.AddHours(Hours));
                    }

                    return (exceeded, count, null);
                }
            }
        }

        return (false, 0, null);
    }


    public DateTime? GetOldestMessageDateForAuthorInLastHours(string author, int hours)
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
                command.Parameters.AddWithValue("@StartDate", DateTime.Now.AddHours(-hours));
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

    public int? Check(string messageText)
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
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        return null;
    }

    public void Add(AveMania aveMania)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(InsertQuery, connection))
            {
                command.Parameters.AddWithValue("@message", aveMania.Message);
                command.Parameters.AddWithValue("@author", aveMania.Author);
                command.Parameters.AddWithValue("@datetime", aveMania.DateTime);
                command.ExecuteNonQuery();
            }
        }
    }

    public void Add(Penalty penalty)
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(InsertPenaltyQuery, connection))
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
                        randomAveManias.Add(new AveMania(
                            reader["message"].ToString() ?? string.Empty,
                            reader["author"].ToString() ?? string.Empty,
                            0,
                            dateTime
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
                        results.Add(new AveMania(
                            reader["message"].ToString() ?? string.Empty,
                            reader["author"].ToString() ?? string.Empty,
                            0,
                            dateTime
                        ));
                    }
                }
            }
        }

        return results;
    }

    public int ClearPenalties()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();

            string deleteQuery = $@"
                DELETE FROM {PenaltyTableName}
                WHERE message LIKE '%AVE MANIA%'";

            using (var command = new SQLiteCommand(deleteQuery, connection))
            {
                var b = command.ExecuteNonQuery();
                return b;
            }
        }
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

                        // Calculate the ratio;
                        // avoid division by zero
                        double ratio = aveManiaCount > 0 ? penaltyCount / (double)aveManiaCount : 0;
                        authorRatios[author] = ratio;
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
}