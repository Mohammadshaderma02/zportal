using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZainEMPProtal.Services;

namespace ZainEMPProtal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                var employees = await _employeeService.GetAllEmployeesAsync();

                return Ok(new
                {
                    success = true,
                    data = employees,
                    count = employees.Count,
                    message = $"تم الحصول على {employees.Count} موظف"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all employees");
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على قائمة الموظفين",
                    details = ex.Message
                });
            }
        }

        [HttpGet("{employeeNT}")]
        public async Task<IActionResult> GetEmployee(string employeeNT)
        {
            try
            {
                // فك تشفير employeeNT إذا كان مُرمز
                employeeNT = Uri.UnescapeDataString(employeeNT);

                var employee = await _employeeService.GetEmployeeByNTAsync(employeeNT);

                if (employee == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "الموظف غير موجود"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = employee,
                    message = "تم الحصول على معلومات الموظف"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee: {EmployeeNT}", employeeNT);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على معلومات الموظف",
                    details = ex.Message
                });
            }
        }

        [HttpGet("{employeeNT}/groups")]
        public async Task<IActionResult> GetEmployeeGroups(string employeeNT)
        {
            try
            {
                employeeNT = Uri.UnescapeDataString(employeeNT);

                var groups = await _employeeService.GetEmployeeGroupsAsync(employeeNT);

                return Ok(new
                {
                    success = true,
                    data = groups,
                    count = groups.Count,
                    message = $"الموظف عضو في {groups.Count} مجموعة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee groups: {EmployeeNT}", employeeNT);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على مجموعات الموظف",
                    details = ex.Message
                });
            }
        }

        [HttpPost("{employeeNT}/groups/{groupId}")]
        public async Task<IActionResult> AddEmployeeToGroup(string employeeNT, int groupId)
        {
            try
            {
                employeeNT = Uri.UnescapeDataString(employeeNT);
                var assignedBy = User.Identity?.Name ?? "SYSTEM";

                var result = await _employeeService.AddEmployeeToGroupAsync(employeeNT, groupId, assignedBy);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "تم إضافة الموظف للمجموعة بنجاح"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "فشل في إضافة الموظف للمجموعة"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee to group: {EmployeeNT}, GroupId: {GroupId}", employeeNT, groupId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في إضافة الموظف للمجموعة",
                    details = ex.Message
                });
            }
        }

        [HttpDelete("{employeeNT}/groups/{groupId}")]
        public async Task<IActionResult> RemoveEmployeeFromGroup(string employeeNT, int groupId)
        {
            try
            {
                employeeNT = Uri.UnescapeDataString(employeeNT);
                var removedBy = User.Identity?.Name ?? "SYSTEM";

                var result = await _employeeService.RemoveEmployeeFromGroupAsync(employeeNT, groupId, removedBy);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "تم إزالة الموظف من المجموعة بنجاح"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "فشل في إزالة الموظف من المجموعة"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing employee from group: {EmployeeNT}, GroupId: {GroupId}", employeeNT, groupId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في إزالة الموظف من المجموعة",
                    details = ex.Message
                });
            }
        }
    }
}
