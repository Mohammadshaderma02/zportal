namespace ZainEMPProtal.Models
{
    public class SystemAccess
    {
        public int SystemId { get; set; }
        public string SystemCode { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconBase64 { get; set; }
        public string? BaseUrl { get; set; }
        public bool IsInternal { get; set; }
        public bool RequiresManager { get; set; }
        public int TotalPermissions { get; set; }
        public int ScreenPermissions { get; set; }
        public int ButtonPermissions { get; set; }
        public int ControllerPermissions { get; set; }
        public string AccessLevel { get; set; } = string.Empty;
    }

    public class PermissionCheck
    {
        public bool HasAccess { get; set; }
        public string AssignmentSource { get; set; } = string.Empty;
        public string SystemCode { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string DisplaySecurityId { get; set; } = string.Empty;
    }

    public class EmployeePermission
    {
        public int SecurityId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string ResourcePath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DisplaySecurityId { get; set; } = string.Empty;
        public string AssignmentSource { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public int SortOrder { get; set; }
    }

    public class EmployeeSecurityId
    {
        public int SecurityId { get; set; }
        public string SecurityName { get; set; } = string.Empty;
        public string SecurityDescription { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string ResourcePath { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string SystemCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string AssignmentSource { get; set; } = string.Empty;
        public string DisplaySecurityId { get; set; } = string.Empty;
    }
}
