using Dapper;
using WebAppsMoodle.Models;

public class RoomRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public RoomRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<object>> GetAllRoomsAsync()
    {
        const string query = "SELECT RoomId, RoomNumber FROM Rooms";

        using (var connection = _dbConnectionFactory.CreateConnection())
        {
            return await connection.QueryAsync<object>(query);
        }
    }


    public async Task<string> GetRoomIdByRoomNumberAsync(string roomNumber)
    {
        const string query = "SELECT RoomId FROM Rooms WHERE RoomNumber = @RoomNumber";

        using (var connection = _dbConnectionFactory.CreateConnection())
        {
            return await connection.QuerySingleOrDefaultAsync<string>(query, new { RoomNumber = roomNumber });
        }
    }

    public async Task<object> GetRoomByIdAsync(string roomId)
    {
        const string query = "SELECT RoomId, RoomNumber FROM Rooms WHERE RoomId = @RoomId";

        using (var connection = _dbConnectionFactory.CreateConnection())
        {
            return await connection.QueryFirstOrDefaultAsync<object>(query, new { RoomId = roomId });
        }
    }

    
        public async Task<int> AddRoomAsync(string roomNumber)
    {
        const string query = "INSERT INTO Rooms (RoomNumber) VALUES (@RoomNumber); SELECT last_insert_rowid();";

        using (var connection = _dbConnectionFactory.CreateConnection())
        {
            return await connection.ExecuteScalarAsync<int>(query, new { RoomNumber = roomNumber });
        }
    }

    public async Task<int> UpdateRoomAsync(int roomId, string roomNumber)
    {
        const string query = "UPDATE Rooms SET RoomNumber = @RoomNumber WHERE RoomId = @RoomId";

        using (var connection = _dbConnectionFactory.CreateConnection())
        {
            return await connection.ExecuteAsync(query, new { RoomId = roomId, RoomNumber = roomNumber });
        }
    }

    public async Task<int> DeleteRoomAsync(int roomId)
    {
        const string query = "DELETE FROM Rooms WHERE RoomId = @RoomId";

        using (var connection = _dbConnectionFactory.CreateConnection())
        {
            return await connection.ExecuteAsync(query, new { RoomId = roomId });
        }
    }
}
