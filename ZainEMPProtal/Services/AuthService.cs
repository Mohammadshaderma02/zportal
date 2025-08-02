using Dapper;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using ZainEMPProtal.Data;
using ZainEMPProtal.Models;

namespace ZainEMPProtal.Services
{
    public interface IAuthService
    {
        Task<AuthResult> AuthenticateAsync(string ntUsername, string password);
        Task<TokenResponse> GenerateTokenAsync(string employeeNT);
        Task<UserInfo> GetUserInfoAsync(string employeeNT);
        Task<ExternalAuthResult> CallExternalAuthAPIAsync(string ntUsername, string password);
    }
    public class AuthService : IAuthService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IZainFlowDbConnectionFactory _IZainFlowDbConnectionFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AuthService(IDbConnectionFactory connectionFactory, IConfiguration configuration, HttpClient httpClient, IZainFlowDbConnectionFactory iZainFlowDbConnectionFactory)
        {
            _connectionFactory = connectionFactory;
            _configuration = configuration;
            _httpClient = httpClient;
            _IZainFlowDbConnectionFactory = iZainFlowDbConnectionFactory;
        }

        public async Task<AuthResult> AuthenticateAsync(string ntUsername ,string password)
        {
            try
            {
                // استدعاء API الخارجي
                var externalResult = await CallExternalAuthAPIAsync(ntUsername, password);
                if (!externalResult.IsSuccess)
                {
                    return new AuthResult
                    {
                        IsSuccess = false,
                        Message = "فشل في التحقق من الهوية الخارجية"
                    };
                }

                // التحقق من وجود المستخدم في قاعدة البيانات
                using var connection = await _connectionFactory.CreateConnectionAsync();

                var employeeExists = await connection.QueryFirstOrDefaultAsync<bool>(
                    @"SELECT CASE WHEN EXISTS(
                        SELECT 1 FROM EmployeeGroups 
                        WHERE EmployeeNT = @EmployeeNT AND IsActive = 1
                    ) THEN 1 ELSE 0 END",
                    new { EmployeeNT = ntUsername }
                );

                if (!employeeExists)
                {
                    await AddUserToDefaultGroupAsync(ntUsername);
                }

                return new AuthResult
                {
                    IsSuccess = true,
                    EmployeeNT = ntUsername,
                    Message = "تم تسجيل الدخول بنجاح"
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    Message = $"خطأ في عملية التحقق: {ex.Message}"
                };
            }
        }
        public async Task<ExternalAuthResult> CallExternalAuthAPIAsync(string ntUsername, string password)
        {
            try
            {

                password = Base64Encode(password);

                string url = "http://192.168.185.66/HRRESTService/REST.svc/HR/HRLogin/" + ntUsername.Trim() + "/" + password;

                var response = _httpClient.GetStringAsync(new Uri(url)).Result;

                

                if (!string.IsNullOrEmpty(response))
                {
                    LoginObject loginObject = JsonConvert.DeserializeObject<LoginObject>(response);
                    if (loginObject != null)
                    {
                        if (loginObject.Data != null)
                        {
                            return new ExternalAuthResult
                            {
                                IsSuccess = true,
                                Message = loginObject.Data.pFNumberField.GetPF()
                            };
                        }
                        else if (loginObject.LoginData != null)
{
                            return new ExternalAuthResult
                            {
                                IsSuccess = true,
                                Message = loginObject.Data.pFNumberField.GetPF()
                            };
                        }                    }
                }


                return new ExternalAuthResult
                {
                    IsSuccess = false,
                    Message = $"API خارجي فشل: {response}"
                };
            }
            catch (Exception ex)
            {
                // في حالة فشل الـ API الخارجي، يمكن السماح بالدخول (حسب سياسة الشركة)
                return new ExternalAuthResult
                {
                    IsSuccess = true, // أو false حسب السياسة
                    Message = $"تحذير: فشل في الاتصال بالـ API الخارجي - {ex.Message}"
                };
            }
        }

        public async Task<TokenResponse> GenerateTokenAsync(string employeeNT)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, employeeNT),
                new Claim(ClaimTypes.NameIdentifier, employeeNT),
                new Claim("EmployeeNT", employeeNT)
            };

            var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");
            var expiryDate = DateTime.UtcNow.AddMinutes(expireMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiryDate,
                signingCredentials: creds
            );

            return new TokenResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiryDate
            };
        }

        //public async Task<UserInfo> GetUserInfoAsync(string employeeNT)
        //{
        //    using var connection = await _connectionFactory.CreateConnectionAsync();
        //    using var _IZainFlowDbConnectionFactory_connection = await _IZainFlowDbConnectionFactory.CreateConnectionAsync();

        //    // الحصول على معلومات المستخدم من جدول Employees (إذا كان موجود)
        //    var empInfo = await _IZainFlowDbConnectionFactory_connection.QueryFirstOrDefaultAsync<dynamic>(
        //        @"SELECT TOP 1 
        //            EmployeeName, EmployeeEmail, EmployeeDepartment, EmployeePosition
        //          FROM EmployeeInfo 
        //          WHERE EmployeeNT = @EmployeeNT AND EmployeeStatus = 'Active'",
        //        new { EmployeeNT = ExtractUsernameFromNT(employeeNT) }
        //    );

        //    // الحصول على المجموعات
        //    var groups = await connection.QueryAsync<string>(
        //        @"SELECT g.Name 
        //          FROM EmployeeGroups eg
        //          JOIN Groups g ON eg.GroupId = g.Id
        //          WHERE eg.EmployeeNT = @EmployeeNT 
        //            AND eg.IsActive = 1 AND g.IsActive = 1",
        //        new { EmployeeNT = ExtractUsernameFromNT(employeeNT) }
        //    );

        //    // الحصول على الأنظمة المتاحة
        //    var availableSystems = await GetAvailableSystemsAsync(employeeNT);

        //    // الحصول على جميع SecurityIds
        //    var permissions = await connection.QueryAsync<int>(
        //        @"SELECT DISTINCT sd.SecurityId
        //          FROM SecurityDefinitions sd
        //          WHERE sd.SecurityId IN (
        //              SELECT gsa.SecurityId 
        //              FROM EmployeeGroups eg
        //              JOIN GroupSecurityAssignments gsa ON eg.GroupId = gsa.GroupId
        //              WHERE eg.EmployeeNT = @EmployeeNT AND eg.IsActive = 1 AND gsa.IsActive = 1
        //              UNION
        //              SELECT SecurityId FROM EmployeeSecurityAssignments 
        //              WHERE EmployeeNT = @EmployeeNT AND IsActive = 1 
        //              AND (ExpiryDate IS NULL OR ExpiryDate > GETDATE())
        //          ) AND sd.IsActive = 1",
        //        new { EmployeeNT = ExtractUsernameFromNT(employeeNT) }
        //    );

        //    return new UserInfo
        //    {
        //        EmployeeNT = employeeNT,
        //        Name = empInfo?.EmployeeName ?? ExtractUsernameFromNT(employeeNT),
        //        Email = empInfo?.EmployeeEmail ?? "",
        //        Department = empInfo?.EmployeeDepartment ?? "",
        //        Groups = groups.ToList(),
        //        AvailableSystems = availableSystems,
        //        Permissions = permissions.ToList()
        //    };
        //}



        public async Task<UserInfo> GetUserInfoAsync(string employeeNT)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var zainFlowConnection = await _IZainFlowDbConnectionFactory.CreateConnectionAsync();

            var username = ExtractUsernameFromNT(employeeNT);
            username = username.ToLower();

            // الحصول على معلومات المستخدم من جدول Employees
            var empInfo = await zainFlowConnection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT TOP 1 
            EmployeeName, EmployeeEmail, EmployeeDepartment, EmployeePosition, EmployeeJob,EmployeeNewNumber 
          FROM EmployeeInfo 
          WHERE EmployeeNT = @EmployeeNT AND EmployeeStatus = 'Active'",
                new { EmployeeNT = username }
            );

            // Check if employee has manager-level job
            bool isManager = username == "tamara.ghatasheh" || username == "mohammad.shaderma";

            //IsManagerLevel(empInfo?.EmployeeJob?.ToString());

            // الحصول على المجموعات
            var groups = await connection.QueryAsync<string>(
                @"SELECT g.Name 
          FROM EmployeeGroups eg
          JOIN Groups g ON eg.GroupId = g.Id
          WHERE eg.EmployeeNT = @EmployeeNT 
            AND eg.IsActive = 1 AND g.IsActive = 1",
                new { EmployeeNT = username }
            );

            // الحصول على الأنظمة المتاحة
            var allAvailableSystems = await GetAvailableSystemsAsync(username);

            // فلترة الأنظمة بناءً على مستوى الوظيفة (إزالة الأنظمة التي تتطلب صلاحية مدير إذا لم يكن المستخدم مديراً)
            var availableSystems = allAvailableSystems.Where(system =>
                !system.RequiresManager || isManager
            ).ToList();

            // الحصول على الصلاحيات مع معرف النظام لكل صلاحية
            // فلترة الصلاحيات بناءً على الأنظمة المتاحة
            var availableSystemIds = availableSystems.Select(s => s.SystemId).ToList();

            var systemPermissions = await connection.QueryAsync<SystemPermission>(
                @"SELECT DISTINCT 
            sd.SecurityId,
            sd.Name as SecurityName,
            sd.SystemId,
            s.Name as SystemName,
            s.SystemCode,
            sd.Description,
            sd.Category,
            sd.ResourceType,
            sd.ResourcePath,
            sd.SortOrder
          FROM SecurityDefinitions sd
          LEFT JOIN Systems s ON sd.SystemId = s.Id
          WHERE sd.SecurityId IN (
              -- صلاحيات من المجموعات
              SELECT gsa.SecurityId 
              FROM EmployeeGroups eg
              JOIN GroupSecurityAssignments gsa ON eg.GroupId = gsa.GroupId
              WHERE eg.EmployeeNT = @EmployeeNT 
                AND eg.IsActive = 1 
                AND gsa.IsActive = 1
              UNION
              -- صلاحيات مباشرة للموظف
              SELECT SecurityId 
              FROM EmployeeSecurityAssignments 
              WHERE EmployeeNT = @EmployeeNT 
                AND IsActive = 1 
                AND (ExpiryDate IS NULL OR ExpiryDate > GETDATE())
          ) 
          AND sd.IsActive = 1
          AND (s.IsActive = 1 OR s.IsActive IS NULL)
          AND (sd.SystemId IS NULL OR sd.SystemId IN @AvailableSystemIds)
          ORDER BY s.Name, sd.Category, sd.SortOrder, sd.Name",
                new { EmployeeNT = username, AvailableSystemIds = availableSystemIds }
            );

            // تجميع الصلاحيات حسب النظام
            var permissionsBySystem = systemPermissions
                .GroupBy(p => new { p.SystemId, p.SystemName, p.SystemCode })
                .ToDictionary(
                    g => g.Key.SystemId ?? 0,
                    g => new SystemPermissionInfo
                    {
                        SystemId = g.Key.SystemId ?? 0,
                        SystemName = g.Key.SystemName ?? "Unknown System",
                        SystemCode = g.Key.SystemCode ?? "",
                        Permissions = g.Select(p => new PermissionDetail
                        {
                            SecurityId = p.SecurityId,
                            SecurityName = p.SecurityName,
                            Description = p.Description,
                            Category = p.Category,
                            ResourceType = p.ResourceType,
                            ResourcePath = p.ResourcePath,
                            SortOrder = p.SortOrder
                        }).OrderBy(p => p.SortOrder).ThenBy(p => p.SecurityName).ToList()
                    }
                );

            // الحصول على جميع SecurityIds فقط (للتوافق مع الكود القديم)
            var allPermissions = systemPermissions.Select(p => p.SecurityId).Distinct().ToList();

            return new UserInfo
            {
                EmployeeNT = employeeNT,
                Name = empInfo?.EmployeeName ?? username,
                EmployeePF = empInfo?.EmployeeNewNumber ?? username,
                Email = empInfo?.EmployeeEmail ?? "",
                Department = empInfo?.EmployeeDepartment ?? "",
                Position = empInfo?.EmployeePosition ?? "",
                Job = empInfo?.EmployeeJob ?? "",
                Groups = groups.ToList(),
                AvailableSystems = availableSystems,
                Permissions = allPermissions, // قائمة SecurityIds للتوافق مع الكود القديم
                SystemPermissions = permissionsBySystem, // صلاحيات مجمعة حسب النظام
                IsManager = isManager
            };
        }
        private bool IsManagerLevel(string job)
        {
            if (string.IsNullOrWhiteSpace(job))
                return false;

            var jobTitle = job.Trim().ToLower();

            return jobTitle == "manager"
                || jobTitle == "executive manager"
                || jobTitle == "senior manager"
                || jobTitle == "division leader"
                || jobTitle == "professional"
                || jobTitle == "senior division leader";
        }
        public List<PermissionDetail> GetPermissionsForSystem(UserInfo userInfo, int systemId)
        {
            return userInfo.SystemPermissions.ContainsKey(systemId)
                ? userInfo.SystemPermissions[systemId].Permissions
                : new List<PermissionDetail>();
        }
        // دالة مساعدة للتحقق من وجود صلاحية في نظام محدد
        public bool HasPermissionInSystem(UserInfo userInfo, int systemId, int securityId)
        {
            return userInfo.SystemPermissions.ContainsKey(systemId) &&
                   userInfo.SystemPermissions[systemId].Permissions.Any(p => p.SecurityId == securityId);
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }


        private async Task AddUserToDefaultGroupAsync(string employeeNT)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            await connection.ExecuteAsync(
                @"INSERT INTO EmployeeGroups (EmployeeNT, GroupId, AssignedBy, AssignedDate, IsActive)
                  SELECT @EmployeeNT, Id, 'SYSTEM', GETDATE(), 1
                  FROM Groups 
                  WHERE Name = 'المستخدمين العاديين' AND IsActive = 1",
                new { EmployeeNT = employeeNT }
            );
        }

        private async Task<List<SystemAccess>> GetAvailableSystemsAsync(string employeeNT)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var systems = await connection.QueryAsync<SystemAccess>(
                "sp_GetEmployeeAvailableSystems",
                new { EmployeeNT = employeeNT },
                commandType: CommandType.StoredProcedure
            );

            return systems.ToList();
        }

        private string ExtractUsernameFromNT(string ntUsername)
        {
            if (string.IsNullOrEmpty(ntUsername)) return "";

            var parts = ntUsername.Split('\\');
            return parts.Length > 1 ? parts[1] : ntUsername;
        }
    }
}
