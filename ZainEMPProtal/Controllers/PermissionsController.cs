using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZainEMPProtal.Services;

namespace ZainEMPProtal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(IPermissionService permissionService, ILogger<PermissionsController> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        [HttpGet("available-systems")]
        public async Task<IActionResult> GetAvailableSystems()
        {
            try
            {
                var employeeNT = User.Identity?.Name;
                if (string.IsNullOrEmpty(employeeNT))
                {
                    return Unauthorized(new { message = "غير مخول للوصول" });
                }

                var systems = await _permissionService.GetEmployeeAvailableSystemsAsync(employeeNT);

                _logger.LogInformation("Retrieved {Count} available systems for user: {EmployeeNT}",
                    systems.Count, employeeNT);

                return Ok(new
                {
                    success = true,
                    data = systems,
                    count = systems.Count,
                    message = $"تم الحصول على {systems.Count} نظام متاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available systems");
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على الأنظمة المتاحة",
                    details = ex.Message
                });
            }
        }

        [HttpGet("check/{securityId}")]
        public async Task<IActionResult> CheckPermission(int securityId)
        {
            try
            {
                var employeeNT = User.Identity?.Name;
                if (string.IsNullOrEmpty(employeeNT))
                {
                    return Unauthorized(new { message = "غير مخول للوصول" });
                }

                var result = await _permissionService.CheckEmployeeSecurityIdAsync(employeeNT, securityId);

                _logger.LogInformation("Permission check for SecurityId {SecurityId}, User: {EmployeeNT}, Result: {HasAccess}",
                    securityId, employeeNT, result.HasAccess);

                return Ok(new
                {
                    success = true,
                    hasAccess = result.HasAccess,
                    securityId = securityId,
                    assignmentSource = result.AssignmentSource,
                    systemCode = result.SystemCode,
                    systemName = result.SystemName,
                    displaySecurityId = result.DisplaySecurityId,
                    message = result.HasAccess ? "لديك صلاحية الوصول" : "ليس لديك صلاحية الوصول"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for SecurityId: {SecurityId}", securityId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في فحص الصلاحية",
                    details = ex.Message
                });
            }
        }

        [HttpGet("system/{systemCode}")]
        public async Task<IActionResult> GetSystemPermissions(string systemCode)
        {
            try
            {
                var employeeNT = User.Identity?.Name;
                if (string.IsNullOrEmpty(employeeNT))
                {
                    return Unauthorized(new { message = "غير مخول للوصول" });
                }

                var permissions = await _permissionService.GetEmployeeSystemPermissionsAsync(employeeNT, systemCode);

                return Ok(new
                {
                    success = true,
                    systemCode = systemCode,
                    data = permissions,
                    count = permissions.Count,
                    message = $"تم الحصول على {permissions.Count} صلاحية في نظام {systemCode}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system permissions for system: {SystemCode}", systemCode);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على صلاحيات النظام",
                    details = ex.Message
                });
            }
        }

        [HttpGet("user-permissions")]
        public async Task<IActionResult> GetUserPermissions()
        {
            try
            {
                var employeeNT = User.Identity?.Name;
                if (string.IsNullOrEmpty(employeeNT))
                {
                    return Unauthorized(new { message = "غير مخول للوصول" });
                }

                var permissions = await _permissionService.GetEmployeeSecurityIdsAsync(employeeNT);

                return Ok(new
                {
                    success = true,
                    data = permissions,
                    count = permissions.Count,
                    message = $"تم الحصول على {permissions.Count} صلاحية"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions");
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على الصلاحيات",
                    details = ex.Message
                });
            }
        }

        [HttpPost("batch-check")]
        public async Task<IActionResult> BatchCheckPermissions([FromBody] List<int> securityIds)
        {
            try
            {
                var employeeNT = User.Identity?.Name;
                if (string.IsNullOrEmpty(employeeNT))
                {
                    return Unauthorized(new { message = "غير مخول للوصول" });
                }

                if (securityIds == null || !securityIds.Any())
                {
                    return BadRequest(new { message = "يجب تمرير قائمة SecurityIds" });
                }

                var results = await _permissionService.BatchCheckPermissionsAsync(employeeNT, securityIds);

                var accessibleCount = results.Values.Count(r => r.HasAccess);

                return Ok(new
                {
                    success = true,
                    data = results,
                    totalChecked = securityIds.Count,
                    accessibleCount = accessibleCount,
                    deniedCount = securityIds.Count - accessibleCount,
                    message = $"تم فحص {securityIds.Count} صلاحية - متاح: {accessibleCount}, غير متاح: {securityIds.Count - accessibleCount}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch permission check");
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في فحص الصلاحيات المتعددة",
                    details = ex.Message
                });
            }
        }

        [HttpGet("security-definitions")]
        public async Task<IActionResult> GetAllSecurityDefinitions()            
        {
            try
            {
                // هذا endpoint للمطورين أو المدراء لرؤية جميع SecurityIds المتاحة
                using var connection = await new Data.SqlConnectionFactory(
                    "your-connection-string").CreateConnectionAsync();

                var definitions = await Dapper.SqlMapper.QueryAsync(connection,
                    @"SELECT SecurityId, Name, Description, ResourceType, ResourcePath, Category,
                             s.SystemCode + '.' + CAST(sd.SecurityId AS NVARCHAR(10)) AS DisplaySecurityId
                      FROM SecurityDefinitions sd
                      JOIN Systems s ON sd.SystemId = s.Id
                      WHERE sd.IsActive = 1 AND s.IsActive = 1
                      ORDER BY s.SystemCode, sd.ResourceType, sd.SortOrder, sd.SecurityId");

                return Ok(new
                {
                    success = true,
                    data = definitions,
                    count = definitions.Count(),
                    message = $"تم الحصول على {definitions.Count()} تعريف أمان"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security definitions");
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على تعريفات الأمان",
                    details = ex.Message
                });
            }
        }
    }
}
