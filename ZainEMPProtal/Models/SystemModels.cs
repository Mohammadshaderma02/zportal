namespace ZainEMPProtal.Models
{
    public class SystemInfo
    {
        public int Id { get; set; }
        public string SystemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconBase64 { get; set; }
        public string? BaseUrl { get; set; }
        public bool IsInternal { get; set; }
        public bool RequiresManager { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class SecurityDefinitionInfo
    {
        public int SecurityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public string? ResourcePath { get; set; }
        public string? Category { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public string DisplaySecurityId { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
    }

    public class SystemGroupInfo
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupDescription { get; set; }
        public int PermissionCount { get; set; }
        public int MemberCount { get; set; }
    }

    public class SystemUserInfo
    {
        public string EmployeeNT { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int PermissionCount { get; set; }
        public string GroupNames { get; set; } = string.Empty;
    }

    public class CreateSystemRequest
    {
        public string SystemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconBase64 { get; set; }
        public string? BaseUrl { get; set; }
        public bool IsInternal { get; set; } = true;
        public bool RequiresManager { get; set; } = false;
    }

    public class UpdateSystemRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconBase64 { get; set; }
        public string? BaseUrl { get; set; }
        public bool IsInternal { get; set; }
        public bool RequiresManager { get; set; }
    }

    public class SystemStatsInfo
    {
        public int TotalSystems { get; set; }
        public int ActiveSystems { get; set; }
        public int InternalSystems { get; set; }
        public int ExternalSystems { get; set; }
        public int TotalSecurityDefinitions { get; set; }
        public int TotalUsers { get; set; }
        public int TotalGroups { get; set; }
    }
}
