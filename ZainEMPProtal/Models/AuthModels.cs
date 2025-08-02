namespace ZainEMPProtal.Models
{
    public class LoginRequest
    {
        public string? EmployeeNT { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? EmployeeNT { get; set; }
    }

    public class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class UserInfo
    {
        public string EmployeeNT { get; set; }
        public string EmployeePF { get; set; }
        public string Job { get; set; }
        public bool IsManager { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public List<string> Groups { get; set; } = new List<string>();
        public List<SystemAccess> AvailableSystems { get; set; }
        public List<int> Permissions { get; set; } = new List<int>(); // للتوافق مع الكود القديم
        public Dictionary<int, SystemPermissionInfo> SystemPermissions { get; set; } = new Dictionary<int, SystemPermissionInfo>();
    }

    public class SystemPermission
    {
        public int SecurityId { get; set; }
        public string SecurityName { get; set; }
        public int? SystemId { get; set; }
        public string SystemName { get; set; }
        public string SystemCode { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string ResourceType { get; set; }
        public string ResourcePath { get; set; }
        public int? SortOrder { get; set; }
    }

    public class SystemPermissionInfo
    {
        public int SystemId { get; set; }
        public string SystemName { get; set; }
        public string SystemCode { get; set; }
        public List<PermissionDetail> Permissions { get; set; } = new List<PermissionDetail>();
    }

    public class PermissionDetail
    {
        public int SecurityId { get; set; }
        public string SecurityName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string ResourceType { get; set; }
        public string ResourcePath { get; set; }
        public int? SortOrder { get; set; }
    }
    // تحديث كلاس UserInfo ليشمل المعلومات الجديدة
    //public class UserInfo
    //{
    //    public string EmployeeNT { get; set; }
    //    public string Name { get; set; }
    //    public string Email { get; set; }
    //    public string Department { get; set; }
    //    public string Position { get; set; }
    //    public List<string> Groups { get; set; } = new List<string>();
    //    public List<string> AvailableSystems { get; set; } = new List<string>();
    //    public List<int> Permissions { get; set; } = new List<int>(); // للتوافق مع الكود القديم
    //    public Dictionary<int, SystemPermissionInfo> SystemPermissions { get; set; } = new Dictionary<int, SystemPermissionInfo>();
    //}
    public class ExternalAuthResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
