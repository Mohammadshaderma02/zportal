using Microsoft.Data.SqlClient;
using System.Data;

namespace ZainEMPProtal.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
        Task<IDbConnection> CreateConnectionAsync();
    }
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
