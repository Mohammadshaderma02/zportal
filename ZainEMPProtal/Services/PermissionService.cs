using Dapper;
using System.Data;
using ZainEMPProtal.Data;
using ZainEMPProtal.Models;

namespace ZainEMPProtal.Services
{
    public interface IPermissionService
    {
        Task<List<SystemAccess>> GetEmployeeAvailableSystemsAsync(string employeeNT);
        Task<PermissionCheck> CheckEmployeeSecurityIdAsync(string employeeNT, int securityId);
        Task<List<EmployeePermission>> GetEmployeeSystemPermissionsAsync(string employeeNT, string systemCode);
        Task<List<EmployeeSecurityId>> GetEmployeeSecurityIdsAsync(string employeeNT);
        Task<Dictionary<int, PermissionCheck>> BatchCheckPermissionsAsync(string employeeNT, List<int> securityIds);
    }
    public class PermissionService : IPermissionService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PermissionService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<SystemAccess>> GetEmployeeAvailableSystemsAsync(string employeeNT)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var systems = await connection.QueryAsync<SystemAccess>(
                "sp_GetEmployeeAvailableSystems",
                new { EmployeeNT = employeeNT },
                commandType: CommandType.StoredProcedure
            );

            return systems.ToList();
        }

        public async Task<PermissionCheck> CheckEmployeeSecurityIdAsync(string employeeNT, int securityId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var result = await connection.QueryFirstOrDefaultAsync<PermissionCheck>(
                "sp_CheckEmployeeSecurityId",
                new { EmployeeNT = employeeNT, SecurityId = securityId },
                commandType: CommandType.StoredProcedure
            );

            return result ?? new PermissionCheck
            {
                HasAccess = false,
                AssignmentSource = "لا يوجد صلاحية",
                SystemCode = "",
                SystemName = "",
                DisplaySecurityId = ""
            };
        }

        public async Task<List<EmployeePermission>> GetEmployeeSystemPermissionsAsync(string employeeNT, string systemCode)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var permissions = await connection.QueryAsync<EmployeePermission>(
                "sp_GetEmployeeSystemPermissions",
                new { EmployeeNT = employeeNT, SystemCode = systemCode },
                commandType: CommandType.StoredProcedure
            );

            return permissions.ToList();
        }

        public async Task<List<EmployeeSecurityId>> GetEmployeeSecurityIdsAsync(string employeeNT)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var securityIds = await connection.QueryAsync<EmployeeSecurityId>(
                "sp_GetEmployeeSecurityIds",
                new { EmployeeNT = employeeNT },
                commandType: CommandType.StoredProcedure
            );

            return securityIds.ToList();
        }

        public async Task<Dictionary<int, PermissionCheck>> BatchCheckPermissionsAsync(string employeeNT, List<int> securityIds)
        {
            var results = new Dictionary<int, PermissionCheck>();

            using var connection = await _connectionFactory.CreateConnectionAsync();

            foreach (var securityId in securityIds)
            {
                var result = await connection.QueryFirstOrDefaultAsync<PermissionCheck>(
                    "sp_CheckEmployeeSecurityId",
                    new { EmployeeNT = employeeNT, SecurityId = securityId },
                    commandType: CommandType.StoredProcedure
                );

                results[securityId] = result ?? new PermissionCheck
                {
                    HasAccess = false,
                    AssignmentSource = "لا يوجد صلاحية"
                };
            }

            return results;
        }
    }
}
