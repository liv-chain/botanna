using System.Data.SQLite;

namespace AveManiaBot;

public interface IDbConnectionFactory
{
    SQLiteConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
       
    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SQLiteConnection CreateConnection()
    {
        var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}