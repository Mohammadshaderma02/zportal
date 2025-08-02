namespace ZainEMPProtal.Models.SkillGapAnalysis
{
    public class Employee
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string EmailAddress { get; set; }
        public string SupervisorNumber { get; set; }
        public string Department { get; set; }
        public string Division { get; set; }
        public Job Job { get; set; }
    }

    public class Job
    {
        public string Title { get; set; }

        public bool isManager()
        {
            return Title?.ToLower().Contains("manager") == true ||
                   Title?.ToLower().Contains("supervisor") == true;
        }

        public bool isDirector()
        {
            return Title?.ToLower().Contains("director") == true ||
                   Title?.ToLower().Contains("ceo") == true;
        }
    }
}
