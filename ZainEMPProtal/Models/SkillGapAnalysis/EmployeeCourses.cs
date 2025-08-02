namespace ZainEMPProtal.Models.SkillGapAnalysis
{
    public class EmployeeCourses
    {
        public int Id { get; set; }
        public string EmployeePF { get; set; }
        public int CourseID { get; set; }
        public string ManagerPF { get; set; }
        public Course? Course { get; set; }
        public Employee? Employee { get; set; }
    }
}
