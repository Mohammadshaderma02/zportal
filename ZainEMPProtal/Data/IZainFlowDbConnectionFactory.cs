using Microsoft.Data.SqlClient;
using System.Data;

namespace ZainEMPProtal.Data
{
    public interface IZainFlowDbConnectionFactory
    {
        IDbConnection CreateConnection();
        Task<IDbConnection> CreateConnectionAsync();
    }
    public class ZainFlowDbConnectionFactory : IZainFlowDbConnectionFactory
    {
        private readonly string _connectionString;

        public ZainFlowDbConnectionFactory(string connectionString)
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
