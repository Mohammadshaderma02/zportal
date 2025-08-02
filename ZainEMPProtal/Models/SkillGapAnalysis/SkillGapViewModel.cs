namespace ZainEMPProtal.Models.SkillGapAnalysis
{
    public class SkillGapViewModel
    {
        public Employee Employee { get; set; }
        public List<Employee> EmployeesList { get; set; } = new();
        public List<Employee> SubEmployeesList { get; set; } = new();
        public List<Course> CoursesList { get; set; } = new();
        public List<EmployeeCourses> EmployeeCourses { get; set; } = new();
        public List<EmployeeCourses> ProfilesCourses { get; set; } = new();
        public Profiles? MyProfile { get; set; }
        public List<Profiles> Profiles { get; set; } = new();
        public string Progress { get; set; }
        public bool CanSubmit { get; set; }

        public bool isManager
        {
            get
            {
                if (Employee?.Job != null)
                    return Employee.Job.isManager();
                return false;
            }
        }

        public bool isDirector
        {
            get
            {
                if (Employee?.Job != null)
                    return Employee.Job.isDirector();
                return false;
            }
        }

        public List<AddCourse> AddCourses { get; set; } = new();
    }

    public class AddCourse
    {
        public string EmployeePF { get; set; }
        public string CourseID { get; set; }
        public string DeleteID { get; set; }
        public bool Add { get; set; }
    }

    public class AddCourseResult
    {
        public List<EmployeeCourses>? EmployeeCourses { get; set; } = new();
        public string Progress { get; set; }
        public bool Result { get; set; }
    }
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
