using System.Data.SQLite;

namespace AveManiaBot;

public class DbRepo
{
    private const string DbPath = "ave_mania.db";
    private const string ConnectionString = $"Data Source={DbPath};Version=3;";
    private const string TableName = "ave_mania";
    private const string CreateTableQuery = $"CREATE TABLE IF NOT EXISTS {TableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT, author TEXT, datetime DATETIME)";
    private const string InsertQuery = $"INSERT INTO {TableName} (message, author, datetime) VALUES (@message, @author, @datetime)";
    private const string SelectQuery = $"SELECT * FROM {TableName}";
    private const string CountQuery = $"SELECT COUNT(*) FROM {TableName}";
    private const string SelectWhereQuery = $"SELECT * FROM {TableName} WHERE message = @message";
    private const string SelectWhereIdQuery = $"SELECT * FROM {TableName} WHERE id = @id";

    public void Init()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(CreateTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
    
    public AveMania Find(int entryId)
    {
        throw new NotImplementedException();
    }

    public int? Check(string messageText)
    {
        throw new NotImplementedException();
    }

    public void Add(AveMania aveMania)
    {
        throw new NotImplementedException();
    }
}