using ZainEMPProtal.Models.SkillGapAnalysis;

namespace ZainEMPProtal.Services
{
    public interface ISkillGapService
    {
        Task<ApiResponse<SkillGapViewModel>> GetViewDataAsync(string employeeNumber);
        Task<ApiResponse<SkillGapViewModel>> GetEmployeeCoursesDataAsync(SkillGapViewModel model);
        Task<ApiResponse<int>> CheckPreviousPhaseAsync(string employeeNumber);
        Task<ApiResponse<AddCourseResult>> AddCoursesAsync(List<AddCourse> data, string managerPF);
        Task<ApiResponse<List<Profiles>>> GetProfilesAsync(string directorPF);
        Task<ApiResponse<Profiles>> GetMyProfileAsync(string directorPF);
        Task<ApiResponse<List<EmployeeCourses>>> GetProfilesCoursesAsync(List<Profiles> profiles);
        Task<ApiResponse<SkillGapViewModel>> GetSubEmployeesListAsync(SkillGapViewModel model);
        Task<ApiResponse<bool>> SubmitProfileAsync(SubmitProfileRequest request);
        Task<ApiResponse<SkillGapViewModel>> RejectProfileAsync(SkillGapViewModel viewModel, RejectProfileRequest request);
        Task<ApiResponse<SkillGapViewModel>> ApproveProfileAsync(SkillGapViewModel viewModel, int id);
        Task<ApiResponse<bool>> AddFeedbackAsync(FeedbackRequest request);
    }
    /// <summary>
    /// Service layer for SkillGap Analysis operations
    /// Implements business logic and coordinates between controller and repository
    /// </summary>
    public class SkillGapService : ISkillGapService
    {
        private readonly ISkillGapRepository _repository;
        private readonly ILogger<SkillGapService> _logger;

        /// <summary>
        /// Constructor for SkillGapService
        /// </summary>
        /// <param name="repository">Repository for data access</param>
        /// <param name="logger">Logger for error tracking</param>
        public SkillGapService(ISkillGapRepository repository, ILogger<SkillGapService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get complete view data for an employee including their team and courses
        /// </summary>
        /// <param name="employeeNumber">Employee identification number</param>
        /// <returns>Complete SkillGapViewModel with employee data</returns>
        public async Task<ApiResponse<SkillGapViewModel>> GetViewDataAsync(string employeeNumber)
        {
            try
            {
                _logger.LogInformation("Getting view data for employee: {EmployeeNumber}", employeeNumber);

                // Validate input
                if (string.IsNullOrWhiteSpace(employeeNumber))
                {
                    _logger.LogWarning("GetViewDataAsync called with empty employee number");
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Employee number cannot be empty",
                        Data = null
                    };
                }

                // Get data from repository
                var result = await _repository.GetViewDataAsync(employeeNumber.Trim());

                if (result == null)
                {
                    _logger.LogWarning("Employee not found: {EmployeeNumber}", employeeNumber);
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = $"Employee with number '{employeeNumber}' not found",
                        Data = null
                    };
                }

                _logger.LogInformation("Successfully retrieved view data for employee: {EmployeeNumber}", employeeNumber);
                return new ApiResponse<SkillGapViewModel>
                {
                    Success = true,
                    Message = "Data retrieved successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetViewDataAsync for employee: {EmployeeNumber}", employeeNumber);
                return new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving employee data",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get employee courses data with updated progress information
        /// </summary>
        /// <param name="model">Current SkillGapViewModel</param>
        /// <returns>Updated SkillGapViewModel with course information</returns>
        public async Task<ApiResponse<SkillGapViewModel>> GetEmployeeCoursesDataAsync(SkillGapViewModel model)
        {
            try
            {
                _logger.LogInformation("Getting employee courses data for: {EmployeeNumber}",
                    model?.Employee?.EmployeeNumber);

                // Validate input
                if (model?.Employee?.EmployeeNumber == null)
                {
                    _logger.LogWarning("GetEmployeeCoursesDataAsync called with invalid model");
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid employee model is required",
                        Data = null
                    };
                }

                // Get updated data from repository
                var result = await _repository.GetEmployeeCoursesDataAsync(model);

                _logger.LogInformation("Successfully retrieved employee courses data for: {EmployeeNumber}",
                    model.Employee.EmployeeNumber);

                return new ApiResponse<SkillGapViewModel>
                {
                    Success = true,
                    Message = "Employee courses data retrieved successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEmployeeCoursesDataAsync for employee: {EmployeeNumber}",
                    model?.Employee?.EmployeeNumber);
                return new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An error occurred while retrieving employee courses data",
                    Data = model ?? new SkillGapViewModel()
                };
            }
        }

        /// <summary>
        /// Check if an employee was part of the previous phase
        /// </summary>
        /// <param name="employeeNumber">Employee identification number</param>
        /// <returns>1 if employee was in previous phase, 0 otherwise</returns>
        public async Task<ApiResponse<int>> CheckPreviousPhaseAsync(string employeeNumber)
        {
            try
            {
                _logger.LogInformation("Checking previous phase for employee: {EmployeeNumber}", employeeNumber);

                // Validate input
                if (string.IsNullOrWhiteSpace(employeeNumber))
                {
                    _logger.LogWarning("CheckPreviousPhaseAsync called with empty employee number");
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Employee number cannot be empty",
                        Data = 0
                    };
                }

                // Check previous phase from repository
                var result = await _repository.CheckPreviousPhaseAsync(employeeNumber.Trim());

                _logger.LogInformation("Previous phase check completed for employee: {EmployeeNumber}, Result: {Result}",
                    employeeNumber, result);

                return new ApiResponse<int>
                {
                    Success = true,
                    Message = "Previous phase check completed successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckPreviousPhaseAsync for employee: {EmployeeNumber}", employeeNumber);
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = "An error occurred while checking previous phase",
                    Data = 0
                };
            }
        }

        /// <summary>
        /// Add courses for employees with validation and progress tracking
        /// </summary>
        /// <param name="data">List of courses to add/remove</param>
        /// <param name="managerPF">Manager's employee number</param>
        /// <returns>Result with updated course information</returns>
        public async Task<ApiResponse<AddCourseResult>> AddCoursesAsync(List<AddCourse> data, string managerPF)
        {
            try
            {
                _logger.LogInformation("Adding courses for manager: {ManagerPF}, Course count: {Count}",
                    managerPF, data?.Count ?? 0);

                // Validate input
                if (data == null || !data.Any())
                {
                    _logger.LogWarning("AddCoursesAsync called with empty course data");
                    return new ApiResponse<AddCourseResult>
                    {
                        Success = false,
                        Message = "Course data cannot be empty",
                        Data = new AddCourseResult { Result = false }
                    };
                }

                if (string.IsNullOrWhiteSpace(managerPF))
                {
                    _logger.LogWarning("AddCoursesAsync called with empty manager PF");
                    return new ApiResponse<AddCourseResult>
                    {
                        Success = false,
                        Message = "Manager employee number cannot be empty",
                        Data = new AddCourseResult { Result = false }
                    };
                }

                // Validate course data
                var addList = data.Where(x => x.Add).ToList();
                if (!addList.Any())
                {
                    _logger.LogWarning("No courses marked for addition in request");
                    return new ApiResponse<AddCourseResult>
                    {
                        Success = false,
                        Message = "No courses marked for addition",
                        Data = new AddCourseResult { Result = false }
                    };
                }

                // Validate course IDs are numeric
                foreach (var course in addList)
                {
                    if (string.IsNullOrWhiteSpace(course.CourseID) || !int.TryParse(course.CourseID, out _))
                    {
                        _logger.LogWarning("Invalid course ID provided: {CourseID}", course.CourseID);
                        return new ApiResponse<AddCourseResult>
                        {
                            Success = false,
                            Message = $"Invalid course ID: {course.CourseID}",
                            Data = new AddCourseResult { Result = false }
                        };
                    }

                    if (string.IsNullOrWhiteSpace(course.EmployeePF))
                    {
                        _logger.LogWarning("Empty employee PF provided in course data");
                        return new ApiResponse<AddCourseResult>
                        {
                            Success = false,
                            Message = "Employee number cannot be empty in course data",
                            Data = new AddCourseResult { Result = false }
                        };
                    }
                }

                // Add courses through repository
                var success = await _repository.AddCoursesAsync(data, managerPF.Trim());
                var result = new AddCourseResult { Result = success };

                if (success)
                {
                    _logger.LogInformation("Successfully added courses for manager: {ManagerPF}", managerPF);

                    // Get updated data for the response
                    try
                    {
                        var viewModel = new SkillGapViewModel
                        {
                            Employee = new Employee { EmployeeNumber = managerPF.Trim() }
                        };
                        var updatedModel = await _repository.GetEmployeeCoursesDataAsync(viewModel);
                        result.EmployeeCourses = updatedModel.EmployeeCourses ?? new List<EmployeeCourses>();
                        result.Progress = updatedModel.Progress ?? "0/0";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get updated course data after addition");
                        // Don't fail the entire operation, just log the warning
                    }

                    return new ApiResponse<AddCourseResult>
                    {
                        Success = true,
                        Message = "Courses added successfully",
                        Data = result
                    };
                }
                else
                {
                    _logger.LogWarning("Failed to add courses for manager: {ManagerPF}", managerPF);
                    return new ApiResponse<AddCourseResult>
                    {
                        Success = false,
                        Message = "Failed to add courses",
                        Data = result
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddCoursesAsync for manager: {ManagerPF}", managerPF);
                return new ApiResponse<AddCourseResult>
                {
                    Success = false,
                    Message = "An error occurred while adding courses",
                    Data = new AddCourseResult { Result = false }
                };
            }
        }

        /// <summary>
        /// Get all profiles managed by a director
        /// </summary>
        /// <param name="directorPF">Director's employee number</param>
        /// <returns>List of profiles under the director</returns>
        public async Task<ApiResponse<List<Profiles>>> GetProfilesAsync(string directorPF)
        {
            try
            {
                _logger.LogInformation("Getting profiles for director: {DirectorPF}", directorPF);

                // Validate input
                if (string.IsNullOrWhiteSpace(directorPF))
                {
                    _logger.LogWarning("GetProfilesAsync called with empty director PF");
                    return new ApiResponse<List<Profiles>>
                    {
                        Success = false,
                        Message = "Director employee number cannot be empty",
                        Data = new List<Profiles>()
                    };
                }

                // Get profiles from repository
                var result = await _repository.GetProfilesAsync(directorPF.Trim());

                _logger.LogInformation("Successfully retrieved {Count} profiles for director: {DirectorPF}",
                    result?.Count ?? 0, directorPF);

                return new ApiResponse<List<Profiles>>
                {
                    Success = true,
                    Message = $"Retrieved {result?.Count ?? 0} profiles successfully",
                    Data = result ?? new List<Profiles>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProfilesAsync for director: {DirectorPF}", directorPF);
                return new ApiResponse<List<Profiles>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving profiles",
                    Data = new List<Profiles>()
                };
            }
        }

        /// <summary>
        /// Get the current user's own profile
        /// </summary>
        /// <param name="directorPF">Director's employee number</param>
        /// <returns>The director's own profile</returns>
        public async Task<ApiResponse<Profiles>> GetMyProfileAsync(string directorPF)
        {
            try
            {
                _logger.LogInformation("Getting personal profile for director: {DirectorPF}", directorPF);

                // Validate input
                if (string.IsNullOrWhiteSpace(directorPF))
                {
                    _logger.LogWarning("GetMyProfileAsync called with empty director PF");
                    return new ApiResponse<Profiles>
                    {
                        Success = false,
                        Message = "Director employee number cannot be empty",
                        Data = null
                    };
                }

                // Get profile from repository
                var result = await _repository.GetMyProfileAsync(directorPF.Trim());

                if (result == null)
                {
                    _logger.LogInformation("No personal profile found for director: {DirectorPF}", directorPF);
                    return new ApiResponse<Profiles>
                    {
                        Success = true,
                        Message = "No profile found",
                        Data = null
                    };
                }

                _logger.LogInformation("Successfully retrieved personal profile for director: {DirectorPF}", directorPF);
                return new ApiResponse<Profiles>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyProfileAsync for director: {DirectorPF}", directorPF);
                return new ApiResponse<Profiles>
                {
                    Success = false,
                    Message = "An error occurred while retrieving personal profile",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Get all courses associated with given profiles
        /// </summary>
        /// <param name="profiles">List of profiles to get courses for</param>
        /// <returns>List of employee courses for the profiles</returns>
        public async Task<ApiResponse<List<EmployeeCourses>>> GetProfilesCoursesAsync(List<Profiles> profiles)
        {
            try
            {
                _logger.LogInformation("Getting courses for {Count} profiles", profiles?.Count ?? 0);

                // Validate input
                if (profiles == null)
                {
                    _logger.LogWarning("GetProfilesCoursesAsync called with null profiles");
                    return new ApiResponse<List<EmployeeCourses>>
                    {
                        Success = false,
                        Message = "Profiles list cannot be null",
                        Data = new List<EmployeeCourses>()
                    };
                }

                if (!profiles.Any())
                {
                    _logger.LogInformation("No profiles provided, returning empty course list");
                    return new ApiResponse<List<EmployeeCourses>>
                    {
                        Success = true,
                        Message = "No profiles provided",
                        Data = new List<EmployeeCourses>()
                    };
                }

                // Get courses from repository
                var result = await _repository.GetProfilesCoursesAsync(profiles);

                _logger.LogInformation("Successfully retrieved {Count} courses for profiles", result?.Count ?? 0);
                return new ApiResponse<List<EmployeeCourses>>
                {
                    Success = true,
                    Message = $"Retrieved {result?.Count ?? 0} courses successfully",
                    Data = result ?? new List<EmployeeCourses>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProfilesCoursesAsync");
                return new ApiResponse<List<EmployeeCourses>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving profile courses",
                    Data = new List<EmployeeCourses>()
                };
            }
        }

        /// <summary>
        /// Get list of sub-employees (direct reports and their reports)
        /// </summary>
        /// <param name="model">Current SkillGapViewModel</param>
        /// <returns>Updated model with sub-employees list</returns>
        public async Task<ApiResponse<SkillGapViewModel>> GetSubEmployeesListAsync(SkillGapViewModel model)
        {
            try
            {
                _logger.LogInformation("Getting sub-employees list for: {EmployeeNumber}",
                    model?.Employee?.EmployeeNumber);

                // Validate input
                if (model?.Employee?.EmployeeNumber == null)
                {
                    _logger.LogWarning("GetSubEmployeesListAsync called with invalid model");
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid employee model is required",
                        Data = null
                    };
                }

                // Get sub-employees from repository
                var result = await _repository.GetSubEmployeesListAsync(model);

                _logger.LogInformation("Successfully retrieved sub-employees list for: {EmployeeNumber}, Count: {Count}",
                    model.Employee.EmployeeNumber, result?.SubEmployeesList?.Count ?? 0);

                return new ApiResponse<SkillGapViewModel>
                {
                    Success = true,
                    Message = "Sub-employees list retrieved successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSubEmployeesListAsync for employee: {EmployeeNumber}",
                    model?.Employee?.EmployeeNumber);
                return new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An error occurred while retrieving sub-employees list",
                    Data = model ?? new SkillGapViewModel()
                };
            }
        }

        /// <summary>
        /// Submit a profile for approval
        /// </summary>
        /// <param name="request">Submit profile request details</param>
        /// <returns>Success status of the submission</returns>
        public async Task<ApiResponse<bool>> SubmitProfileAsync(SubmitProfileRequest request)
        {
            try
            {
                _logger.LogInformation("Submitting profile for employee: {EmployeeNumber}", request?.EmployeeNumber);

                // Validate input
                if (request == null)
                {
                    _logger.LogWarning("SubmitProfileAsync called with null request");
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Submit profile request cannot be null",
                        Data = false
                    };
                }

                if (string.IsNullOrWhiteSpace(request.EmployeeNumber))
                {
                    _logger.LogWarning("SubmitProfileAsync called with empty employee number");
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Employee number cannot be empty",
                        Data = false
                    };
                }

                if (string.IsNullOrWhiteSpace(request.SupervisorNumber))
                {
                    _logger.LogWarning("SubmitProfileAsync called with empty supervisor number");
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Supervisor number cannot be empty",
                        Data = false
                    };
                }

                if (string.IsNullOrWhiteSpace(request.ManagerFullName))
                {
                    _logger.LogWarning("SubmitProfileAsync called with empty manager name");
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Manager full name cannot be empty",
                        Data = false
                    };
                }

                // Submit profile through repository
                await _repository.SubmitProfileAsync(
                    request.EmployeeNumber.Trim(),
                    request.SupervisorNumber.Trim(),
                    request.ManagerFullName.Trim(),
                    request.IsDirector
                );

                _logger.LogInformation("Successfully submitted profile for employee: {EmployeeNumber}",
                    request.EmployeeNumber);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Profile submitted successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitProfileAsync for employee: {EmployeeNumber}",
                    request?.EmployeeNumber);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while submitting profile",
                    Data = false
                };
            }
        }

        /// <summary>
        /// Reject a submitted profile with reason
        /// </summary>
        /// <param name="viewModel">Current view model</param>
        /// <param name="request">Rejection request details</param>
        /// <returns>Updated view model after rejection</returns>
        public async Task<ApiResponse<SkillGapViewModel>> RejectProfileAsync(SkillGapViewModel viewModel, RejectProfileRequest request)
        {
            try
            {
                _logger.LogInformation("Rejecting profile ID: {ProfileId}, Reason: {Reason}",
                    request?.Id, request?.RejectionReason);

                // Validate input
                if (viewModel?.Employee?.EmployeeNumber == null)
                {
                    _logger.LogWarning("RejectProfileAsync called with invalid view model");
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid view model is required",
                        Data = null
                    };
                }

                if (request == null)
                {
                    _logger.LogWarning("RejectProfileAsync called with null request");
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Reject request cannot be null",
                        Data = viewModel
                    };
                }

                if (request.Id <= 0)
                {
                    _logger.LogWarning("RejectProfileAsync called with invalid profile ID: {Id}", request.Id);
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid profile ID is required",
                        Data = viewModel
                    };
                }

                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    _logger.LogWarning("RejectProfileAsync called with empty rejection reason");
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Rejection reason cannot be empty",
                        Data = viewModel
                    };
                }

                // Reject profile through repository
                var result = await _repository.RejectProfileAsync(
                    viewModel,
                    request.Id,
                    request.RejectionReason.Trim(),
                    request.SendToHR
                );

                _logger.LogInformation("Successfully rejected profile ID: {ProfileId}", request.Id);
                return new ApiResponse<SkillGapViewModel>
                {
                    Success = true,
                    Message = "Profile rejected successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RejectProfileAsync for profile ID: {ProfileId}", request?.Id);
                return new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An error occurred while rejecting profile",
                    Data = viewModel ?? new SkillGapViewModel()
                };
            }
        }

        /// <summary>
        /// Approve a submitted profile
        /// </summary>
        /// <param name="viewModel">Current view model</param>
        /// <param name="id">Profile ID to approve</param>
        /// <returns>Updated view model after approval</returns>
        public async Task<ApiResponse<SkillGapViewModel>> ApproveProfileAsync(SkillGapViewModel viewModel, int id)
        {
            try
            {
                _logger.LogInformation("Approving profile ID: {ProfileId}", id);

                // Validate input
                if (viewModel?.Employee?.EmployeeNumber == null)
                {
                    _logger.LogWarning("ApproveProfileAsync called with invalid view model");
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid view model is required",
                        Data = null
                    };
                }

                if (id <= 0)
                {
                    _logger.LogWarning("ApproveProfileAsync called with invalid profile ID: {Id}", id);
                    return new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid profile ID is required",
                        Data = viewModel
                    };
                }

                // Approve profile through repository
                var result = await _repository.ApproveProfileAsync(viewModel, id);

                _logger.LogInformation("Successfully approved profile ID: {ProfileId}", id);
                return new ApiResponse<SkillGapViewModel>
                {
                    Success = true,
                    Message = "Profile approved successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApproveProfileAsync for profile ID: {ProfileId}", id);
                return new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An error occurred while approving profile",
                    Data = viewModel ?? new SkillGapViewModel()
                };
            }
        }

        /// <summary>
        /// Add feedback from an employee
        /// </summary>
        /// <param name="request">Feedback request details</param>
        /// <returns>Success status of feedback submission</returns>
        public async Task<ApiResponse<bool>> AddFeedbackAsync(FeedbackRequest request)
        {
            try
            {
                _logger.LogInformation("Adding feedback for employee: {EmployeeNumber}", request?.EmployeeNumber);

                // Validate input
                if (request == null)
                {
                    _logger.LogWarning("AddFeedbackAsync called with null request");
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Feedback request cannot be null",
                        Data = false
                    };
                }

                if (string.IsNullOrWhiteSpace(request.EmployeeNumber))
                {
                    _logger.LogWarning("AddFeedbackAsync called with empty employee number");
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Employee number cannot be empty",
                        Data = false
                    };
                }

                if (string.IsNullOrWhiteSpace(request.EmployeeName))
                {
                    _logger.LogWarning("AddFeedbackAsync called with empty employee name");
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Employee name cannot be empty",
                        Data = false
                    };
                }

                if (string.IsNullOrWhiteSpace(request.Feedback))
                {
                    _logger.LogWarning("AddFeedbackAsync called with empty feedback");
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Feedback cannot be empty",
                        Data = false
                    };
                }

                // Clean and validate course-related fields
                var area = string.IsNullOrWhiteSpace(request.Area) ? string.Empty : request.Area.Trim();
                var skill = string.IsNullOrWhiteSpace(request.Skill) ? string.Empty : request.Skill.Trim();
                var course = string.IsNullOrWhiteSpace(request.Course) ? string.Empty : request.Course.Trim();

                // Add feedback through repository
                await _repository.AddFeedbackAsync(
                    request.EmployeeNumber.Trim(),
                    request.EmployeeName.Trim(),
                    request.Feedback.Trim(),
                    area,
                    skill,
                    course
                );

                _logger.LogInformation("Successfully added feedback for employee: {EmployeeNumber}",
                    request.EmployeeNumber);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Feedback added successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddFeedbackAsync for employee: {EmployeeNumber}",
                    request?.EmployeeNumber);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while adding feedback",
                    Data = false
                };
            }
        }
    }

}
