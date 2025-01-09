using System.Data.SQLite;
using System.Text.Json;

namespace AveManiaBot;

public class DbRepo
{
    private const string DbPath = "ave_mania.db";
    private const string ConnectionString = $"Data Source={DbPath};Version=3;";
    private const string TableName = "ave_mania";

    private const string CreateTableQuery =
        $"CREATE TABLE IF NOT EXISTS {TableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT, author TEXT, datetime DATETIME)";

    private const string InsertQuery =
        $"INSERT INTO {TableName} (message, author, datetime) VALUES (@message, @author, @datetime)";

    private const string SelectQuery = $"SELECT * FROM {TableName}";
    private const string CountQuery = $"SELECT COUNT(*) FROM {TableName}";
    private const string SelectWhereQuery = $"SELECT * FROM {TableName} WHERE message = @message";
    private const string SelectWhereIdQuery = $"SELECT * FROM {TableName} WHERE id = @id";

    public void InitDataBase()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(CreateTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            List<ChatData> chatData = LoadChatDataFromJsonFiles();
            ImportChatData(chatData, connection);
        }
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
                // if (mess.content != null && !mess.content.Any(c => c != null && c > 127))
                // {
                //     Console.WriteLine($"Skipping message {mess.content} (does not contains accented characters)");
                //     continue;
                // }

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
        string query = $"SELECT * FROM {TableName} WHERE message LIKE '%' || @searchText || '%'";
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

    public void DeleteDupicates()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();

            // Deleting duplicate rows while keeping the one with the smallest id
            string deleteDuplicatesQuery = $@"
                DELETE FROM {TableName}
                WHERE id NOT IN (
                    SELECT MAX(id)
                    FROM {TableName}
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
            SELECT * FROM {TableName}
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
            FROM {TableName} 
            GROUP BY author";

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
            string query = $@"SELECT * FROM {TableName} WHERE datetime >= @fromTime ORDER BY datetime DESC";

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
}