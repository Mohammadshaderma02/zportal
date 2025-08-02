namespace ZainEMPProtal.Models.SkillGapAnalysis
{
    public class Profiles
    {
        public int ID { get; set; }
        public string ManagerPF { get; set; }
        public string DirectorPF { get; set; }
        public bool isApproved { get; set; }
        public bool isSubmitted { get; set; }
        public DateTime CreatedOn { get; set; }
    }
    public class ProfileStatus
    {
        public int Id { get; set; }
        public int ProfileID { get; set; }
        public string Status { get; set; }
        public string StatusReason { get; set; }
        public string CreatedOn { get; set; }
    }

    public class RejectProfileRequest
    {
        public int Id { get; set; }
        public string RejectionReason { get; set; }
        public bool SendToHR { get; set; }
    }
    public class SubmitProfileRequest
    {
        public string EmployeeNumber { get; set; }
        public string SupervisorNumber { get; set; }
        public string ManagerFullName { get; set; }
        public bool IsDirector { get; set; }
    }
}
