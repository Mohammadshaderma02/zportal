using System.ComponentModel.DataAnnotations;

namespace ZainEMPProtal.Models.SkillGapAnalysis
{
    /// <summary>
    /// Course statistics model
    /// </summary>
    public class CourseStatistics
    {
        public int TotalCourses { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalAreas { get; set; }
        public int TotalSkills { get; set; }
        public int TotalLevels { get; set; }
        public List<string> Departments { get; set; } = new();
        public List<string> Areas { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public List<string> Levels { get; set; } = new();
    }

    /// <summary>
    /// Bulk import result model
    /// </summary>
    public class BulkImportResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<Course> ImportedCourses { get; set; } = new();
    }

    /// <summary>
    /// Course validation result model
    /// </summary>
    public class CourseValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Course export request model
    /// </summary>
    public class CourseExportRequest
    {
        public CourseSearchRequest SearchCriteria { get; set; } = new();
        public string Format { get; set; } = "csv"; // csv, excel
        public bool IncludeHeaders { get; set; } = true;
        public List<string> ColumnsToInclude { get; set; } = new();
    }
    /// <summary>
    /// Course entity model
    /// </summary>

    /// <summary>
    /// Create course request model
    /// </summary>
    public class CreateCourseRequest
    {
        [Required(ErrorMessage = "Course number is required")]
        [StringLength(50, ErrorMessage = "Course number cannot exceed 50 characters")]
        public string CourseNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Main department is required")]
        [StringLength(100, ErrorMessage = "Main department cannot exceed 100 characters")]
        public string MainDepartment { get; set; } = string.Empty;

        [Required(ErrorMessage = "Area is required")]
        [StringLength(100, ErrorMessage = "Area cannot exceed 100 characters")]
        public string Area { get; set; } = string.Empty;

        [Required(ErrorMessage = "Skill is required")]
        [StringLength(100, ErrorMessage = "Skill cannot exceed 100 characters")]
        public string Skill { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course name is required")]
        [StringLength(200, ErrorMessage = "Course name cannot exceed 200 characters")]
        public string CourseName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Level cannot exceed 50 characters")]
        public string Level { get; set; } = string.Empty;

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500, ErrorMessage = "Link cannot exceed 500 characters")]
        public string Link { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Course division cannot exceed 100 characters")]
        public string CourseDivision { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update course request model
    /// </summary>
    public class UpdateCourseRequest
    {
        [Required(ErrorMessage = "Course ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Course ID must be greater than 0")]
        public int ID { get; set; }

        [Required(ErrorMessage = "Course number is required")]
        [StringLength(50, ErrorMessage = "Course number cannot exceed 50 characters")]
        public string CourseNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Main department is required")]
        [StringLength(100, ErrorMessage = "Main department cannot exceed 100 characters")]
        public string MainDepartment { get; set; } = string.Empty;

        [Required(ErrorMessage = "Area is required")]
        [StringLength(100, ErrorMessage = "Area cannot exceed 100 characters")]
        public string Area { get; set; } = string.Empty;

        [Required(ErrorMessage = "Skill is required")]
        [StringLength(100, ErrorMessage = "Skill cannot exceed 100 characters")]
        public string Skill { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course name is required")]
        [StringLength(200, ErrorMessage = "Course name cannot exceed 200 characters")]
        public string CourseName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Level cannot exceed 50 characters")]
        public string Level { get; set; } = string.Empty;

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500, ErrorMessage = "Link cannot exceed 500 characters")]
        public string Link { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Course division cannot exceed 100 characters")]
        public string CourseDivision { get; set; } = string.Empty;
    }

    /// <summary>
    /// Course search/filter request model
    /// </summary>
    public class CourseSearchRequest
    {
        public string? CourseNo { get; set; }
        public string? MainDepartment { get; set; }
        public string? Area { get; set; }
        public string? Skill { get; set; }
        public string? CourseName { get; set; }
        public string? Level { get; set; }
        public string? CourseDivision { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Paginated course result model
    /// </summary>
    public class PaginatedCourseResult
    {
        public List<Course> Courses { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
