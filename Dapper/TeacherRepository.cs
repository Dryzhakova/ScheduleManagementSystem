using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

public class TeacherRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public TeacherRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<object>> GetAllTeachersAsync()
    {
        const string query = @"
            SELECT TeacherId, Username AS TeacherName, Title AS TeacherTitle
            FROM Teachers";

        using (var connection = _dbConnectionFactory.CreateConnection())
        {
            return await connection.QueryAsync<object>(query);
        }
    }
}
