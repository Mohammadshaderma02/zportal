using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZainEMPProtal.Data;
using ZainEMPProtal.Models;

namespace ZainEMPProtal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SystemsController : ControllerBase
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<SystemsController> _logger;

        public SystemsController(IDbConnectionFactory connectionFactory, ILogger<SystemsController> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // =============================================
        // GET: api/systems - Get all systems
        // =============================================
        [HttpGet]
        public async Task<IActionResult> GetAllSystems()
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var systems = await connection.QueryAsync<SystemInfo>(
                    @"SELECT 
                        Id, SystemCode, Name, Description, IconBase64, BaseUrl, 
                        IsInternal, RequiresManager, IsActive, CreatedDate, CreatedBy,
                        ModifiedDate, ModifiedBy
                      FROM Systems 
                      WHERE IsActive = 1
                      ORDER BY SystemCode");

                _logger.LogInformation("Retrieved {Count} systems", systems.Count());

                return Ok(new
                {
                    success = true,
                    data = systems,
                    count = systems.Count(),
                    message = $"تم الحصول على {systems.Count()} نظام"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all systems");
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على الأنظمة",
                    details = ex.Message
                });
            }
        }

        // =============================================
        // GET: api/systems/{systemCode} - Get specific system
        // =============================================
        [HttpGet("{systemCode}")]
        public async Task<IActionResult> GetSystem(string systemCode)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var system = await connection.QueryFirstOrDefaultAsync<SystemInfo>(
                    @"SELECT 
                        Id, SystemCode, Name, Description, IconBase64, BaseUrl, 
                        IsInternal, RequiresManager, IsActive, CreatedDate, CreatedBy,
                        ModifiedDate, ModifiedBy
                      FROM Systems 
                      WHERE SystemCode = @SystemCode AND IsActive = 1",
                    new { SystemCode = systemCode });

                if (system == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "النظام غير موجود"
                    });
                }

                _logger.LogInformation("Retrieved system: {SystemCode}", systemCode);

                return Ok(new
                {
                    success = true,
                    data = system,
                    message = "تم الحصول على معلومات النظام"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system: {SystemCode}", systemCode);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على معلومات النظام",
                    details = ex.Message
                });
            }
        }

        // =============================================
        // GET: api/systems/{systemCode}/security-definitions
        // =============================================
        [HttpGet("{systemCode}/security-definitions")]
        public async Task<IActionResult> GetSystemSecurityDefinitions(string systemCode)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var definitions = await connection.QueryAsync<SecurityDefinitionInfo>(
                    @"SELECT 
                        sd.SecurityId, sd.Name, sd.Description, sd.ResourceType, 
                        sd.ResourcePath, sd.Category, sd.SortOrder, sd.IsActive,
                        sd.CreatedDate, sd.CreatedBy, sd.ModifiedDate, sd.ModifiedBy,
                        s.SystemCode + '.' + CAST(sd.SecurityId AS NVARCHAR(10)) AS DisplaySecurityId,
                        s.Name AS SystemName
                      FROM SecurityDefinitions sd
                      JOIN Systems s ON sd.SystemId = s.Id
                      WHERE s.SystemCode = @SystemCode AND sd.IsActive = 1 AND s.IsActive = 1
                      ORDER BY sd.ResourceType, sd.SortOrder, sd.SecurityId",
                    new { SystemCode = systemCode });

                _logger.LogInformation("Retrieved {Count} security definitions for system: {SystemCode}",
                    definitions.Count(), systemCode);

                return Ok(new
                {
                    success = true,
                    systemCode = systemCode,
                    data = definitions,
                    count = definitions.Count(),
                    message = $"تم الحصول على {definitions.Count()} تعريف أمان للنظام {systemCode}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system security definitions: {SystemCode}", systemCode);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على تعريفات الأمان للنظام",
                    details = ex.Message
                });
            }
        }

        // =============================================
        // GET: api/systems/{systemCode}/groups
        // =============================================
        [HttpGet("{systemCode}/groups")]
        public async Task<IActionResult> GetSystemGroups(string systemCode)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var groups = await connection.QueryAsync<SystemGroupInfo>(
                    @"SELECT DISTINCT
                        g.Id AS GroupId,
                        g.Name AS GroupName,
                        g.Description AS GroupDescription,
                        COUNT(DISTINCT gsa.SecurityId) AS PermissionCount,
                        COUNT(DISTINCT eg.EmployeeNT) AS MemberCount
                      FROM Groups g
                      JOIN GroupSecurityAssignments gsa ON g.Id = gsa.GroupId
                      JOIN SecurityDefinitions sd ON gsa.SecurityId = sd.SecurityId
                      JOIN Systems s ON sd.SystemId = s.Id
                      LEFT JOIN EmployeeGroups eg ON g.Id = eg.GroupId AND eg.IsActive = 1
                      WHERE s.SystemCode = @SystemCode 
                        AND g.IsActive = 1 
                        AND gsa.IsActive = 1 
                        AND sd.IsActive = 1
                      GROUP BY g.Id, g.Name, g.Description
                      ORDER BY g.Name",
                    new { SystemCode = systemCode });

                return Ok(new
                {
                    success = true,
                    systemCode = systemCode,
                    data = groups,
                    count = groups.Count(),
                    message = $"تم الحصول على {groups.Count()} مجموعة لها صلاحيات في نظام {systemCode}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system groups: {SystemCode}", systemCode);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على مجموعات النظام",
                    details = ex.Message
                });
            }
        }

        // =============================================
        // GET: api/systems/{systemCode}/users
        // =============================================
        [HttpGet("{systemCode}/users")]
        public async Task<IActionResult> GetSystemUsers(string systemCode)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var users = await connection.QueryAsync<SystemUserInfo>(
                    @"SELECT DISTINCT
                        e.EmployeeNT,
                        COALESCE(emp.EmployeeName, SUBSTRING(e.EmployeeNT, CHARINDEX('\', e.EmployeeNT) + 1, LEN(e.EmployeeNT))) AS Name,
                        COALESCE(emp.EmployeeEmail, '') AS Email,
                        COALESCE(emp.EmployeeDepartment, '') AS Department,
                        COUNT(DISTINCT sd.SecurityId) AS PermissionCount,
                        STRING_AGG(g.Name, ', ') AS GroupNames
                      FROM EmployeeGroups e
                      JOIN GroupSecurityAssignments gsa ON e.GroupId = gsa.GroupId
                      JOIN SecurityDefinitions sd ON gsa.SecurityId = sd.SecurityId
                      JOIN Systems s ON sd.SystemId = s.Id
                      JOIN Groups g ON e.GroupId = g.Id
                      LEFT JOIN Employees emp ON e.EmployeeNT = emp.EmployeeNT
                      WHERE s.SystemCode = @SystemCode 
                        AND e.IsActive = 1 
                        AND gsa.IsActive = 1 
                        AND sd.IsActive = 1
                        AND g.IsActive = 1
                      GROUP BY 
                        e.EmployeeNT, emp.EmployeeName, emp.EmployeeEmail, emp.EmployeeDepartment
                      ORDER BY Name",
                    new { SystemCode = systemCode });

                return Ok(new
                {
                    success = true,
                    systemCode = systemCode,
                    data = users,
                    count = users.Count(),
                    message = $"تم الحصول على {users.Count()} مستخدم لديهم صلاحيات في نظام {systemCode}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system users: {SystemCode}", systemCode);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على مستخدمي النظام",
                    details = ex.Message
                });
            }
        }

        // =============================================
        // POST: api/systems - Create new system
        // =============================================
        [HttpPost]
        public async Task<IActionResult> CreateSystem([FromBody] CreateSystemRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SystemCode) || string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "رمز النظام والاسم مطلوبان"
                    });
                }

                using var connection = await _connectionFactory.CreateConnectionAsync();

                // Check if system code already exists
                var exists = await connection.QueryFirstOrDefaultAsync<bool>(
                    "SELECT CASE WHEN EXISTS(SELECT 1 FROM Systems WHERE SystemCode = @SystemCode) THEN 1 ELSE 0 END",
                    new { SystemCode = request.SystemCode });

                if (exists)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "رمز النظام موجود مسبقاً"
                    });
                }

                var createdBy = User.Identity?.Name ?? "SYSTEM";

                var systemId = await connection.QuerySingleAsync<int>(
                    @"INSERT INTO Systems (SystemCode, Name, Description, IconBase64, BaseUrl, IsInternal, RequiresManager, CreatedBy)
                      OUTPUT INSERTED.Id
                      VALUES (@SystemCode, @Name, @Description, @IconBase64, @BaseUrl, @IsInternal, @RequiresManager, @CreatedBy)",
                    new
                    {
                        SystemCode = request.SystemCode,
                        Name = request.Name,
                        Description = request.Description,
                        IconBase64 = request.IconBase64,
                        BaseUrl = request.BaseUrl,
                        IsInternal = request.IsInternal,
                        RequiresManager = request.RequiresManager,
                        CreatedBy = createdBy
                    });

                _logger.LogInformation("Created new system: {SystemCode} with ID: {SystemId}", request.SystemCode, systemId);

                return CreatedAtAction(
                    nameof(GetSystem),
                    new { systemCode = request.SystemCode },
                    new
                    {
                        success = true,
                        data = new { Id = systemId, SystemCode = request.SystemCode },
                        message = "تم إنشاء النظام بنجاح"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system: {SystemCode}", request.SystemCode);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في إنشاء النظام",
                    details = ex.Message
                });
            }
        }

        // =============================================
        // PUT: api/systems/{systemCode} - Update system
        // =============================================
        [HttpPut("{systemCode}")]
        public async Task<IActionResult> UpdateSystem(string systemCode, [FromBody] UpdateSystemRequest request)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var modifiedBy = User.Identity?.Name ?? "SYSTEM";

                var rowsAffected = await connection.ExecuteAsync(
                    @"UPDATE Systems 
                      SET Name = @Name, 
                          Description = @Description, 
                          IconBase64 = @IconBase64, 
                          BaseUrl = @BaseUrl, 
                          IsInternal = @IsInternal, 
                          RequiresManager = @RequiresManager,
                          ModifiedDate = GETDATE(),
                          ModifiedBy = @ModifiedBy
                      WHERE SystemCode = @SystemCode AND IsActive = 1",
                    new
                    {
                        SystemCode = systemCode,
                        Name = request.Name,
                        Description = request.Description,
                        IconBase64 = request.IconBase64,
                        BaseUrl = request.BaseUrl,
                        IsInternal = request.IsInternal,
                        RequiresManager = request.RequiresManager,
                        ModifiedBy = modifiedBy
                    });

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "النظام غير موجود"
                    });
                }

                _logger.LogInformation("Updated system: {SystemCode}", systemCode);

                return Ok(new
                {
                    success = true,
                    message = "تم تحديث النظام بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system: {SystemCode}", systemCode);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في تحديث النظام",
                    details = ex.Message
                });
            }
        }

        // =============================================
        // DELETE: api/systems/{systemCode} - Soft delete system
        // =============================================
        [HttpDelete("{systemCode}")]
        public async Task<IActionResult> DeleteSystem(string systemCode)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var modifiedBy = User.Identity?.Name ?? "SYSTEM";

                var rowsAffected = await connection.ExecuteAsync(
                    @"UPDATE Systems 
                      SET IsActive = 0, 
                          ModifiedDate = GETDATE(),
                          ModifiedBy = @ModifiedBy
                      WHERE SystemCode = @SystemCode AND IsActive = 1",
                    new { SystemCode = systemCode, ModifiedBy = modifiedBy });

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "النظام غير موجود"
                    });
                }

                _logger.LogInformation("Deleted system: {SystemCode}", systemCode);

                return Ok(new
                {
                    success = true,
                    message = "تم حذف النظام بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system: {SystemCode}", systemCode);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في حذف النظام",
                    details = ex.Message
                });
            }
        }

        // =============================================
        // GET: api/systems/stats - Get system statistics
        // =============================================
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var stats = await connection.QueryFirstAsync<SystemStatsInfo>(
                    @"SELECT 
                        COUNT(*) AS TotalSystems,
                        COUNT(CASE WHEN IsActive = 1 THEN 1 END) AS ActiveSystems,
                        COUNT(CASE WHEN IsInternal = 1 AND IsActive = 1 THEN 1 END) AS InternalSystems,
                        COUNT(CASE WHEN IsInternal = 0 AND IsActive = 1 THEN 1 END) AS ExternalSystems,
                        (SELECT COUNT(*) FROM SecurityDefinitions WHERE IsActive = 1) AS TotalSecurityDefinitions,
                        (SELECT COUNT(DISTINCT EmployeeNT) FROM EmployeeGroups WHERE IsActive = 1) AS TotalUsers,
                        (SELECT COUNT(*) FROM Groups WHERE IsActive = 1) AS TotalGroups
                      FROM Systems");

                return Ok(new
                {
                    success = true,
                    data = stats,
                    message = "تم الحصول على إحصائيات الأنظمة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system stats");
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على الإحصائيات",
                    details = ex.Message
                });
            }
        }

        // =============================================
        // GET: api/systems/search?q={query}
        // =============================================
        [HttpGet("search")]
        public async Task<IActionResult> SearchSystems([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "يجب تمرير نص البحث"
                    });
                }

                using var connection = await _connectionFactory.CreateConnectionAsync();

                var systems = await connection.QueryAsync<SystemInfo>(
                    @"SELECT 
                        Id, SystemCode, Name, Description, IconBase64, BaseUrl, 
                        IsInternal, RequiresManager, IsActive, CreatedDate, CreatedBy
                      FROM Systems 
                      WHERE IsActive = 1 
                        AND (SystemCode LIKE @Query OR Name LIKE @Query OR Description LIKE @Query)
                      ORDER BY 
                        CASE 
                            WHEN SystemCode = @ExactQuery THEN 1
                            WHEN Name = @ExactQuery THEN 2
                            WHEN SystemCode LIKE @QueryStart THEN 3
                            WHEN Name LIKE @QueryStart THEN 4
                            ELSE 5
                        END, SystemCode",
                    new
                    {
                        Query = $"%{q}%",
                        ExactQuery = q,
                        QueryStart = $"{q}%"
                    });

                return Ok(new
                {
                    success = true,
                    query = q,
                    data = systems,
                    count = systems.Count(),
                    message = $"تم العثور على {systems.Count()} نظام"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching systems with query: {Query}", q);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في البحث",
                    details = ex.Message
                });
            }
        }
    }
}
