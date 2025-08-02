namespace ZainEMPProtal.Models.SkillGapAnalysis
{
    public class Feedback
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; }
        public string EmployeeName { get; set; }
        public string Comment { get; set; }
        public string CourseArea { get; set; }
        public string CourseSkill { get; set; }
        public string CourseName { get; set; }
        public string CreatedOn { get; set; }
    }
    public class FeedbackRequest
    {
        public string EmployeeNumber { get; set; }
        public string EmployeeName { get; set; }
        public string Feedback { get; set; }
        public string Area { get; set; }
        public string Skill { get; set; }
        public string Course { get; set; }
    }
}
