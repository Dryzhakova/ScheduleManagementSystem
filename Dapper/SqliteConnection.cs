using System.Data;
using Microsoft.Data.Sqlite;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqliteConnection : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnection(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public IDbConnection CreateConnection()
    {
        return new Microsoft.Data.Sqlite.SqliteConnection(_connectionString);
    }
}
