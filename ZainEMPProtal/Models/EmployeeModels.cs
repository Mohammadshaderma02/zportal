namespace ZainEMPProtal.Models
{
    public class EmployeeInfo
    {
        public string EmployeeNT { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime? JoinDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GroupInfo
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string GroupDescription { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public string AssignedBy { get; set; } = string.Empty;
    }
}
