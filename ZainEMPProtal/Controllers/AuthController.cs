using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Principal;
using ZainEMPProtal.Models;
using ZainEMPProtal.Services;

namespace ZainEMPProtal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ServiceResponse<object>>> Login([FromBody] LoginRequest request)
        {
            ServiceResponse<object> oResponse = new();
            object oData = new object();

            try
            {
                _logger.LogInformation("Login attempt for user: {Username}", request.EmployeeNT);

            
                var windowsUser = HttpContext.User.Identity?.Name;
                var employeeNT = !string.IsNullOrEmpty(request.EmployeeNT)
                    ? request.EmployeeNT
                    : windowsUser ?? $"NT\\{request.Username}";

              
                var authResult = await _authService.AuthenticateAsync(employeeNT, request.Password);

                if (!authResult.IsSuccess)
                {
                    _logger.LogWarning("Authentication failed for user: {EmployeeNT}", employeeNT);
                    return Unauthorized(new { message = authResult.Message });
                }

                // إنشاء Token
                var tokenResponse = await _authService.GenerateTokenAsync(employeeNT);

                // الحصول على معلومات المستخدم
                var userInfo = await _authService.GetUserInfoAsync(employeeNT);

                _logger.LogInformation("Login successful for user: {EmployeeNT}", employeeNT);
                oData = new
                {
                    success = true,
                    token = tokenResponse.Token,
                    expiresAt = tokenResponse.ExpiresAt,
                    user = userInfo
                };
                oResponse.Data = oData;
                oResponse.StatusCode = 200;

                return Ok(oResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                oResponse.IsSuccess = false;
                oResponse.ErrorMessage = ex.Message;
                oResponse.ErrorMessageAr = ex.Message;

                return StatusCode(417, oResponse);
            }
        }
        //[HttpGet("windows-auth")]
        //[Authorize(Policy = "WindowsOnly")]
        //public async Task<ActionResult<ServiceResponse<object>>> WindowsAuth()
        //{
        //    ServiceResponse<object> oResponse = new();
        //    try
        //    {
        //        var windowsIdentity = (WindowsIdentity)User.Identity;
        //        if (windowsIdentity == null)
        //            return Unauthorized("Windows identity not found");
        //        var employeeNT = windowsIdentity.Name;

        //        _logger.LogInformation("Windows authentication for user: {EmployeeNT}", employeeNT);

        //        // Generate token for Windows user (no password needed)
        //        var tokenResponse = await _authService.GenerateTokenAsync(employeeNT);
        //        var userInfo = await _authService.GetUserInfoAsync(employeeNT);

        //        var oData = new
        //        {
        //            success = true,
        //            token = tokenResponse.Token,
        //            expiresAt = tokenResponse.ExpiresAt,
        //            user = userInfo
        //        };

        //        oResponse.Data = oData;
        //        oResponse.StatusCode = 200;
        //        return Ok(oResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error during Windows authentication");
        //        oResponse.IsSuccess = false;
        //        oResponse.ErrorMessage = ex.Message;
        //        return StatusCode(417, oResponse);
        //    }
        //}

[HttpGet("windows-auth")]
    [Authorize(Policy = "WindowsOnly")]
    public async Task<ActionResult<ServiceResponse<object>>> WindowsAuth()
    {
        ServiceResponse<object> oResponse = new();
        try
        {
            // ✅ Check identity exists and is authenticated
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
            {
                oResponse.IsSuccess = false;
                oResponse.ErrorMessage = "User is not authenticated via Windows.";
                return Unauthorized(oResponse);
            }

            // ✅ Safely cast to WindowsIdentity
            if (User.Identity is not WindowsIdentity windowsIdentity)
            {
                oResponse.IsSuccess = false;
                oResponse.ErrorMessage = "Unable to retrieve Windows identity.";
                return Unauthorized(oResponse);
            }

            var employeeNT = windowsIdentity.Name;
            _logger.LogInformation("Windows authentication for user: {EmployeeNT}", employeeNT);

            // ✅ Generate token for Windows user
            var tokenResponse = await _authService.GenerateTokenAsync(employeeNT);
            var userInfo = await _authService.GetUserInfoAsync(employeeNT);

            var oData = new
            {
                success = true,
                token = tokenResponse.Token,
                expiresAt = tokenResponse.ExpiresAt,
                user = userInfo
            };

            oResponse.Data = oData;
            oResponse.StatusCode = 200;
            return Ok(oResponse);
        }
        catch (Exception ex)
        {
            oResponse.IsSuccess = false;
            oResponse.ErrorMessage = ex.Message;
            return StatusCode(500, oResponse);
        }
    }

    [HttpGet("user-info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var employeeNT = User.Identity?.Name;
                if (string.IsNullOrEmpty(employeeNT))
                {
                    return Unauthorized(new { message = "غير مخول للوصول" });
                }

                var userInfo = await _authService.GetUserInfoAsync(employeeNT);

                return Ok(new
                {
                    success = true,
                    user = userInfo,
                    message = "تم الحصول على معلومات المستخدم"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return StatusCode(500, new
                {
                    success = false,
                    message = "خطأ في الحصول على معلومات المستخدم",
                    details = ex.Message
                });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // في JWT، الـ logout يتم من جهة العميل بحذف الـ token
            // يمكن إضافة blacklist للـ tokens هنا إذا أردت
            return Ok(new
            {
                success = true,
                message = "تم تسجيل الخروج بنجاح"
            });
        }
    }
}
