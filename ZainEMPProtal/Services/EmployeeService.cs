using Dapper;
using System.Data;
using ZainEMPProtal.Data;
using ZainEMPProtal.Models;

namespace ZainEMPProtal.Services
{
    public interface IEmployeeService
    {
        Task<List<EmployeeInfo>> GetAllEmployeesAsync();
        Task<EmployeeInfo?> GetEmployeeByNTAsync(string employeeNT);
        Task<List<GroupInfo>> GetEmployeeGroupsAsync(string employeeNT);
        Task<bool> AddEmployeeToGroupAsync(string employeeNT, int groupId, string assignedBy);
        Task<bool> RemoveEmployeeFromGroupAsync(string employeeNT, int groupId, string removedBy);
    }
    public class EmployeeService : IEmployeeService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EmployeeService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<EmployeeInfo>> GetAllEmployeesAsync()
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var employees = await connection.QueryAsync<EmployeeInfo>(
                @"SELECT DISTINCT 
                    eg.EmployeeNT,
                    COALESCE(e.EmployeeName, SUBSTRING(eg.EmployeeNT, CHARINDEX('\', eg.EmployeeNT) + 1, LEN(eg.EmployeeNT))) AS Name,
                    COALESCE(e.EmployeeEmail, '') AS Email,
                    COALESCE(e.EmployeeDepartment, '') AS Department,
                    eg.AssignedDate AS JoinDate,
                    CASE WHEN eg.IsActive = 1 THEN 'نشط' ELSE 'غير نشط' END AS Status
                  FROM EmployeeGroups eg
                  LEFT JOIN Employees e ON eg.EmployeeNT = e.EmployeeNT
                  WHERE eg.IsActive = 1
                  ORDER BY eg.AssignedDate DESC"
            );

            return employees.ToList();
        }

        public async Task<EmployeeInfo?> GetEmployeeByNTAsync(string employeeNT)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var employee = await connection.QueryFirstOrDefaultAsync<EmployeeInfo>(
                @"SELECT 
                    @EmployeeNT AS EmployeeNT,
                    COALESCE(e.EmployeeName, SUBSTRING(@EmployeeNT, CHARINDEX('\', @EmployeeNT) + 1, LEN(@EmployeeNT))) AS Name,
                    COALESCE(e.EmployeeEmail, '') AS Email,
                    COALESCE(e.EmployeeDepartment, '') AS Department,
                    COALESCE(e.EmployeePosition, '') AS Position,
                    e.EmployeeHireDate AS JoinDate,
                    CASE WHEN EXISTS(SELECT 1 FROM EmployeeGroups WHERE EmployeeNT = @EmployeeNT AND IsActive = 1) 
                         THEN 'نشط' ELSE 'غير نشط' END AS Status
                  FROM Employees e
                  WHERE e.EmployeeNT = @EmployeeNT",
                new { EmployeeNT = employeeNT }
            );

            return employee;
        }

        public async Task<List<GroupInfo>> GetEmployeeGroupsAsync(string employeeNT)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var groups = await connection.QueryAsync<GroupInfo>(
                @"SELECT 
                    g.Id AS GroupId,
                    g.Name AS GroupName,
                    g.Description AS GroupDescription,
                    eg.AssignedDate,
                    eg.AssignedBy
                  FROM EmployeeGroups eg
                  JOIN Groups g ON eg.GroupId = g.Id
                  WHERE eg.EmployeeNT = @EmployeeNT 
                    AND eg.IsActive = 1 AND g.IsActive = 1
                  ORDER BY eg.AssignedDate DESC",
                new { EmployeeNT = employeeNT }
            );

            return groups.ToList();
        }

        public async Task<bool> AddEmployeeToGroupAsync(string employeeNT, int groupId, string assignedBy)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var result = await connection.ExecuteAsync(
                "sp_AddEmployeeToGroup",
                new { EmployeeNT = employeeNT, GroupId = groupId, AssignedBy = assignedBy },
                commandType: CommandType.StoredProcedure
            );

            return result > 0;
        }

        public async Task<bool> RemoveEmployeeFromGroupAsync(string employeeNT, int groupId, string removedBy)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var result = await connection.ExecuteAsync(
                @"UPDATE EmployeeGroups 
                  SET IsActive = 0, RemovedDate = GETDATE(), RemovedBy = @RemovedBy
                  WHERE EmployeeNT = @EmployeeNT AND GroupId = @GroupId AND IsActive = 1",
                new { EmployeeNT = employeeNT, GroupId = groupId, RemovedBy = removedBy }
            );

            return result > 0;
        }
    }
}
