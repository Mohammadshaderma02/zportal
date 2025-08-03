using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ZainEMPProtal.Models.SkillGapAnalysis;
using ZainEMPProtal.Services;

namespace ZainEMPProtal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    /// <summary>
    /// SkillGap Analysis Web API Controller
    /// Handles all skill gap analysis operations including employee management, course assignments, and profile approvals
    /// </summary>

    public class SkillGapController : ControllerBase
    {
        private readonly ISkillGapService _skillGapService;
        private readonly ILogger<SkillGapController> _logger;

        /// <summary>
        /// Constructor for SkillGapController
        /// </summary>
        /// <param name="skillGapService">Service layer for business logic</param>
        /// <param name="logger">Logger for request tracking</param>
        public SkillGapController(ISkillGapService skillGapService, ILogger<SkillGapController> logger)
        {
            _skillGapService = skillGapService ?? throw new ArgumentNullException(nameof(skillGapService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get comprehensive view data for an employee including their team and courses
        /// </summary>
        /// <param name="employeeNumber">Employee identification number</param>
        /// <returns>Complete SkillGapViewModel with employee data</returns>
        /// <response code="200">Returns the employee view data</response>
        /// <response code="400">If the employee number is invalid</response>
        /// <response code="404">If the employee is not found</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("view-data/{employeeNumber}")]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<SkillGapViewModel>>> GetViewData(
            [FromRoute][Required] string employeeNumber)
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/view-data/{EmployeeNumber} - Request received", employeeNumber);

                if (string.IsNullOrWhiteSpace(employeeNumber))
                {
                    _logger.LogWarning("GetViewData called with empty employee number");
                    return BadRequest(new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Employee number is required and cannot be empty",
                        Data = null
                    });
                }

                var result = await _skillGapService.GetViewDataAsync(employeeNumber);

                if (!result.Success)
                {
                    _logger.LogWarning("GetViewData failed for employee: {EmployeeNumber}, Message: {Message}",
                        employeeNumber, result.Message);

                    if (result.Message.Contains("not found"))
                        return NotFound(result);

                    return BadRequest(result);
                }

                _logger.LogInformation("GetViewData successful for employee: {EmployeeNumber}", employeeNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetViewData for employee: {EmployeeNumber}", employeeNumber);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get employee courses data with progress information
        /// </summary>
        /// <param name="model">SkillGapViewModel containing employee information</param>
        /// <returns>Updated SkillGapViewModel with course data</returns>
        /// <response code="200">Returns the updated employee courses data</response>
        /// <response code="400">If the model is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("employee-courses")]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<SkillGapViewModel>>> GetEmployeeCoursesData(
            [FromBody] SkillGapViewModel model)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/employee-courses - Request received for employee: {EmployeeNumber}",
                    model?.Employee?.EmployeeNumber);

                if (model?.Employee?.EmployeeNumber == null)
                {
                    _logger.LogWarning("GetEmployeeCoursesData called with invalid model");
                    return BadRequest(new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid employee data is required in the model",
                        Data = null
                    });
                }

                var result = await _skillGapService.GetEmployeeCoursesDataAsync(model);

                _logger.LogInformation("GetEmployeeCoursesData completed for employee: {EmployeeNumber}",
                    model.Employee.EmployeeNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetEmployeeCoursesData for employee: {EmployeeNumber}",
                    model?.Employee?.EmployeeNumber);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Check if an employee was part of the previous phase
        /// </summary>
        /// <param name="employeeNumber">Employee identification number</param>
        /// <returns>1 if employee was in previous phase, 0 otherwise</returns>
        /// <response code="200">Returns the previous phase check result</response>
        /// <response code="400">If the employee number is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("check-previous-phase/{employeeNumber}")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<int>>> CheckPreviousPhase(
            [FromRoute][Required] string employeeNumber)
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/check-previous-phase/{EmployeeNumber} - Request received",
                    employeeNumber);

                if (string.IsNullOrWhiteSpace(employeeNumber))
                {
                    _logger.LogWarning("CheckPreviousPhase called with empty employee number");
                    return BadRequest(new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Employee number is required and cannot be empty",
                        Data = 0
                    });
                }

                var result = await _skillGapService.CheckPreviousPhaseAsync(employeeNumber);

                _logger.LogInformation("CheckPreviousPhase completed for employee: {EmployeeNumber}, Result: {Result}",
                    employeeNumber, result.Data);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CheckPreviousPhase for employee: {EmployeeNumber}",
                    employeeNumber);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<int>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = 0
                });
            }
        }

        /// <summary>
        /// Add or update courses for employees
        /// </summary>
        /// <param name="request">Course addition request containing course data and manager information</param>
        /// <returns>Result of the course addition operation</returns>
        /// <response code="200">Returns the course addition result</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("add-courses")]
        [ProducesResponseType(typeof(ApiResponse<AddCourseResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AddCourseResult>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<AddCourseResult>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AddCourseResult>>> AddCourses(
            [FromBody][Required] AddCoursesRequest request)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/add-courses - Request received for manager: {ManagerPF}, Course count: {Count}",
                    request?.ManagerPF, request?.Data?.Count ?? 0);

                if (request?.Data == null || !request.Data.Any() || string.IsNullOrWhiteSpace(request.ManagerPF))
                {
                    _logger.LogWarning("AddCourses called with invalid request data");
                    return BadRequest(new ApiResponse<AddCourseResult>
                    {
                        Success = false,
                        Message = "Valid course data and manager employee number are required",
                        Data = new AddCourseResult { Result = false }
                    });
                }

                var result = await _skillGapService.AddCoursesAsync(request.Data, request.ManagerPF);

                _logger.LogInformation("AddCourses completed for manager: {ManagerPF}, Success: {Success}",
                    request.ManagerPF, result.Success);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AddCourses for manager: {ManagerPF}",
                    request?.ManagerPF);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<AddCourseResult>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new AddCourseResult { Result = false }
                });
            }
        }

        /// <summary>
        /// Get all profiles managed by a director
        /// </summary>
        /// <param name="directorPF">Director's employee number</param>
        /// <returns>List of profiles under the director's management</returns>
        /// <response code="200">Returns the list of profiles</response>
        /// <response code="400">If the director PF is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("profiles/{directorPF}")]
        [ProducesResponseType(typeof(ApiResponse<List<Profiles>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<Profiles>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<List<Profiles>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<Profiles>>>> GetProfiles(
            [FromRoute][Required] string directorPF)
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/profiles/{DirectorPF} - Request received", directorPF);

                if (string.IsNullOrWhiteSpace(directorPF))
                {
                    _logger.LogWarning("GetProfiles called with empty director PF");
                    return BadRequest(new ApiResponse<List<Profiles>>
                    {
                        Success = false,
                        Message = "Director employee number is required and cannot be empty",
                        Data = new List<Profiles>()
                    });
                }

                var result = await _skillGapService.GetProfilesAsync(directorPF);

                _logger.LogInformation("GetProfiles completed for director: {DirectorPF}, Count: {Count}",
                    directorPF, result.Data?.Count ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetProfiles for director: {DirectorPF}", directorPF);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<Profiles>>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new List<Profiles>()
                });
            }
        }

        /// <summary>
        /// Get the current user's own profile
        /// </summary>
        /// <param name="directorPF">Director's employee number</param>
        /// <returns>The director's personal profile</returns>
        /// <response code="200">Returns the personal profile</response>
        /// <response code="400">If the director PF is invalid</response>
        /// <response code="404">If no profile is found</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("my-profile/{directorPF}")]
        [ProducesResponseType(typeof(ApiResponse<Profiles>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Profiles>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<Profiles>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<Profiles>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<Profiles>>> GetMyProfile(
            [FromRoute][Required] string directorPF)
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/my-profile/{DirectorPF} - Request received", directorPF);

                if (string.IsNullOrWhiteSpace(directorPF))
                {
                    _logger.LogWarning("GetMyProfile called with empty director PF");
                    return BadRequest(new ApiResponse<Profiles>
                    {
                        Success = false,
                        Message = "Director employee number is required and cannot be empty",
                        Data = null
                    });
                }

                var result = await _skillGapService.GetMyProfileAsync(directorPF);

                if (!result.Success)
                {
                    _logger.LogWarning("GetMyProfile failed for director: {DirectorPF}, Message: {Message}",
                        directorPF, result.Message);
                    return BadRequest(result);
                }

                if (result.Data == null)
                {
                    _logger.LogInformation("No profile found for director: {DirectorPF}", directorPF);
                    return NotFound(result);
                }

                _logger.LogInformation("GetMyProfile completed for director: {DirectorPF}", directorPF);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetMyProfile for director: {DirectorPF}", directorPF);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<Profiles>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get all courses associated with given profiles
        /// </summary>
        /// <param name="profiles">List of profiles to retrieve courses for</param>
        /// <returns>List of employee courses for the specified profiles</returns>
        /// <response code="200">Returns the list of profile courses</response>
        /// <response code="400">If the profiles list is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("profiles-courses")]
        [ProducesResponseType(typeof(ApiResponse<List<EmployeeCourses>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<EmployeeCourses>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<List<EmployeeCourses>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<EmployeeCourses>>>> GetProfilesCourses(
            [FromBody][Required] List<Profiles> profiles)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/profiles-courses - Request received for {Count} profiles",
                    profiles?.Count ?? 0);

                if (profiles == null)
                {
                    _logger.LogWarning("GetProfilesCourses called with null profiles list");
                    return BadRequest(new ApiResponse<List<EmployeeCourses>>
                    {
                        Success = false,
                        Message = "Profiles list cannot be null",
                        Data = new List<EmployeeCourses>()
                    });
                }

                var result = await _skillGapService.GetProfilesCoursesAsync(profiles);

                _logger.LogInformation("GetProfilesCourses completed, Course count: {Count}",
                    result.Data?.Count ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetProfilesCourses");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<EmployeeCourses>>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new List<EmployeeCourses>()
                });
            }
        }

        /// <summary>
        /// Get list of sub-employees (direct reports and their reports)
        /// </summary>
        /// <param name="model">SkillGapViewModel containing employee information</param>
        /// <returns>Updated model with sub-employees list</returns>
        /// <response code="200">Returns the updated model with sub-employees</response>
        /// <response code="400">If the model is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("sub-employees")]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<SkillGapViewModel>>> GetSubEmployeesList(
            [FromBody][Required] SkillGapViewModel model)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/sub-employees - Request received for employee: {EmployeeNumber}",
                    model?.Employee?.EmployeeNumber);

                if (model?.Employee?.EmployeeNumber == null)
                {
                    _logger.LogWarning("GetSubEmployeesList called with invalid model");
                    return BadRequest(new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid employee data is required in the model",
                        Data = null
                    });
                }

                var result = await _skillGapService.GetSubEmployeesListAsync(model);

                _logger.LogInformation("GetSubEmployeesList completed for employee: {EmployeeNumber}, Sub-employees count: {Count}",
                    model.Employee.EmployeeNumber, result.Data?.SubEmployeesList?.Count ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetSubEmployeesList for employee: {EmployeeNumber}",
                    model?.Employee?.EmployeeNumber);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Submit a profile for approval
        /// </summary>
        /// <param name="request">Submit profile request containing employee and supervisor information</param>
        /// <returns>Success status of the profile submission</returns>
        /// <response code="200">Returns the submission result</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("submit-profile")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> SubmitProfile(
            [FromBody][Required] SubmitProfileRequest request)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/submit-profile - Request received for employee: {EmployeeNumber}",
                    request?.EmployeeNumber);

                if (request == null || string.IsNullOrWhiteSpace(request.EmployeeNumber) ||
                    string.IsNullOrWhiteSpace(request.SupervisorNumber) || string.IsNullOrWhiteSpace(request.ManagerFullName))
                {
                    _logger.LogWarning("SubmitProfile called with invalid request data");
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Employee number, supervisor number, and manager full name are all required",
                        Data = false
                    });
                }

                var result = await _skillGapService.SubmitProfileAsync(request);

                _logger.LogInformation("SubmitProfile completed for employee: {EmployeeNumber}, Success: {Success}",
                    request.EmployeeNumber, result.Success);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SubmitProfile for employee: {EmployeeNumber}",
                    request?.EmployeeNumber);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = false
                });
            }
        }

        /// <summary>
        /// Reject a submitted profile with reason
        /// </summary>
        /// <param name="request">Reject profile request containing view model and rejection details</param>
        /// <returns>Updated view model after rejection</returns>
        /// <response code="200">Returns the updated view model</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("reject-profile")]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<SkillGapViewModel>>> RejectProfile(
            [FromBody][Required] RejectProfileViewModel request)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/reject-profile - Request received for profile ID: {ProfileId}",
                    request?.RejectRequest?.Id);

                if (request?.ViewModel?.Employee?.EmployeeNumber == null || request.RejectRequest == null ||
                    request.RejectRequest.Id <= 0 || string.IsNullOrWhiteSpace(request.RejectRequest.RejectionReason))
                {
                    _logger.LogWarning("RejectProfile called with invalid request data");
                    return BadRequest(new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid view model, profile ID, and rejection reason are required",
                        Data = null
                    });
                }

                var result = await _skillGapService.RejectProfileAsync(request.ViewModel, request.RejectRequest);

                _logger.LogInformation("RejectProfile completed for profile ID: {ProfileId}, Success: {Success}",
                    request.RejectRequest.Id, result.Success);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in RejectProfile for profile ID: {ProfileId}",
                    request?.RejectRequest?.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Approve a submitted profile
        /// </summary>
        /// <param name="request">Approve profile request containing view model and profile ID</param>
        /// <returns>Updated view model after approval</returns>
        /// <response code="200">Returns the updated view model</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("approve-profile")]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<SkillGapViewModel>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<SkillGapViewModel>>> ApproveProfile(
            [FromBody][Required] ApproveProfileViewModel request)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/approve-profile - Request received for profile ID: {ProfileId}",
                    request?.Id);

                if (request?.ViewModel?.Employee?.EmployeeNumber == null || request.Id <= 0)
                {
                    _logger.LogWarning("ApproveProfile called with invalid request data");
                    return BadRequest(new ApiResponse<SkillGapViewModel>
                    {
                        Success = false,
                        Message = "Valid view model and profile ID are required",
                        Data = null
                    });
                }

                var result = await _skillGapService.ApproveProfileAsync(request.ViewModel, request.Id);

                _logger.LogInformation("ApproveProfile completed for profile ID: {ProfileId}, Success: {Success}",
                    request.Id, result.Success);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ApproveProfile for profile ID: {ProfileId}",
                    request?.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<SkillGapViewModel>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Add feedback from an employee
        /// </summary>
        /// <param name="request">Feedback request containing employee information and feedback details</param>
        /// <returns>Success status of the feedback submission</returns>
        /// <response code="200">Returns the feedback submission result</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("feedback")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> AddFeedback(
            [FromBody][Required] FeedbackRequest request)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/feedback - Request received for employee: {EmployeeNumber}",
                    request?.EmployeeNumber);

                if (request == null || string.IsNullOrWhiteSpace(request.EmployeeNumber) ||
                    string.IsNullOrWhiteSpace(request.EmployeeName) || string.IsNullOrWhiteSpace(request.Feedback))
                {
                    _logger.LogWarning("AddFeedback called with invalid request data");
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Employee number, employee name, and feedback are all required",
                        Data = false
                    });
                }

                var result = await _skillGapService.AddFeedbackAsync(request);

                _logger.LogInformation("AddFeedback completed for employee: {EmployeeNumber}, Success: {Success}",
                    request.EmployeeNumber, result.Success);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AddFeedback for employee: {EmployeeNumber}",
                    request?.EmployeeNumber);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = false
                });
            }
        }

        /// <summary>
        /// Health check endpoint to verify API availability
        /// </summary>
        /// <returns>Health status of the API</returns>
        /// <response code="200">API is healthy and running</response>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public ActionResult GetHealth()
        {
            _logger.LogInformation("Health check requested");
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Service = "SkillGap Analysis API"
            });
        }




        /// <summary>
        /// Get courses with search and pagination
        /// </summary>
        /// <param name="request">Search and pagination parameters</param>
        /// <returns>Paginated list of courses</returns>
        /// <response code="200">Returns the paginated list of courses</response>
        /// <response code="400">If the request parameters are invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("courses/search")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedCourseResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedCourseResult>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedCourseResult>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PaginatedCourseResult>>> GetCourses(
            [FromBody] CourseSearchRequest request)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/courses/search - Request received");

                if (request == null)
                {
                    _logger.LogWarning("GetCourses called with null request");
                    return BadRequest(new ApiResponse<PaginatedCourseResult>
                    {
                        Success = false,
                        Message = "Search request cannot be null",
                        Data = new PaginatedCourseResult()
                    });
                }

                var result = await _skillGapService.GetCoursesAsync(request);

                _logger.LogInformation("GetCourses completed, returned {Count} courses",
                    result.Data?.Courses?.Count ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetCourses");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<PaginatedCourseResult>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new PaginatedCourseResult()
                });
            }
        }

        /// <summary>
        /// Get course by ID
        /// </summary>
        /// <param name="courseId">Course ID</param>
        /// <returns>Course details</returns>
        /// <response code="200">Returns the course details</response>
        /// <response code="400">If the course ID is invalid</response>
        /// <response code="404">If the course is not found</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("courses/{courseId}")]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<Course>>> GetCourseById(
            [FromRoute][Required] int courseId)
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/courses/{CourseId} - Request received", courseId);

                if (courseId <= 0)
                {
                    _logger.LogWarning("GetCourseById called with invalid course ID: {CourseId}", courseId);
                    return BadRequest(new ApiResponse<Course>
                    {
                        Success = false,
                        Message = "Course ID must be greater than 0",
                        Data = null
                    });
                }

                var result = await _skillGapService.GetCourseByIdAsync(courseId);

                if (!result.Success && result.Message.Contains("not found"))
                {
                    _logger.LogInformation("Course not found: {CourseId}", courseId);
                    return NotFound(result);
                }

                _logger.LogInformation("GetCourseById completed for course: {CourseId}", courseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetCourseById for course: {CourseId}", courseId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<Course>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Create new course
        /// </summary>
        /// <param name="request">Course creation data</param>
        /// <returns>Created course details</returns>
        /// <response code="201">Returns the created course</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="409">If the course number already exists</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost("courses")]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<Course>>> CreateCourse(
            [FromBody][Required] CreateCourseRequest request)
        {
            try
            {
                _logger.LogInformation("POST /api/skillgap/courses - Request received for course: {CourseNo}",
                    request?.CourseNo);

                if (request == null)
                {
                    _logger.LogWarning("CreateCourse called with null request");
                    return BadRequest(new ApiResponse<Course>
                    {
                        Success = false,
                        Message = "Course data is required",
                        Data = null
                    });
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
                        .ToList();

                    _logger.LogWarning("CreateCourse called with invalid model state: {Errors}",
                        string.Join("; ", errors));

                    return BadRequest(new ApiResponse<Course>
                    {
                        Success = false,
                        Message = $"Validation failed: {string.Join("; ", errors)}",
                        Data = null
                    });
                }

                var result = await _skillGapService.CreateCourseAsync(request);

                if (!result.Success)
                {
                    if (result.Message.Contains("already exists"))
                    {
                        return Conflict(result);
                    }
                    return BadRequest(result);
                }

                _logger.LogInformation("CreateCourse completed for course: {CourseNo} with ID: {CourseId}",
                    request.CourseNo, result.Data?.Id);

                return CreatedAtAction(
                    nameof(GetCourseById),
                    new { courseId = result.Data.Id },
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateCourse for course: {CourseNo}",
                    request?.CourseNo);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<Course>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Update existing course
        /// </summary>
        /// <param name="courseId">Course ID to update</param>
        /// <param name="request">Course update data</param>
        /// <returns>Updated course details</returns>
        /// <response code="200">Returns the updated course</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="404">If the course is not found</response>
        /// <response code="409">If the course number already exists for another course</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPut("courses/{courseId}")]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<Course>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<Course>>> UpdateCourse(
            [FromRoute][Required] int courseId,
            [FromBody][Required] UpdateCourseRequest request)
        {
            try
            {
                _logger.LogInformation("PUT /api/skillgap/courses/{CourseId} - Request received", courseId);

                if (courseId <= 0)
                {
                    _logger.LogWarning("UpdateCourse called with invalid course ID: {CourseId}", courseId);
                    return BadRequest(new ApiResponse<Course>
                    {
                        Success = false,
                        Message = "Course ID must be greater than 0",
                        Data = null
                    });
                }

                if (request == null)
                {
                    _logger.LogWarning("UpdateCourse called with null request");
                    return BadRequest(new ApiResponse<Course>
                    {
                        Success = false,
                        Message = "Course data is required",
                        Data = null
                    });
                }

                if (courseId != request.ID)
                {
                    _logger.LogWarning("UpdateCourse: Route ID {RouteId} doesn't match request ID {RequestId}",
                        courseId, request.ID);
                    return BadRequest(new ApiResponse<Course>
                    {
                        Success = false,
                        Message = "Course ID in route must match course ID in request body",
                        Data = null
                    });
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
                        .ToList();

                    _logger.LogWarning("UpdateCourse called with invalid model state: {Errors}",
                        string.Join("; ", errors));

                    return BadRequest(new ApiResponse<Course>
                    {
                        Success = false,
                        Message = $"Validation failed: {string.Join("; ", errors)}",
                        Data = null
                    });
                }

                var result = await _skillGapService.UpdateCourseAsync(request);

                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(result);
                    }
                    if (result.Message.Contains("already exists"))
                    {
                        return Conflict(result);
                    }
                    return BadRequest(result);
                }

                _logger.LogInformation("UpdateCourse completed for course ID: {CourseId}", courseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateCourse for course ID: {CourseId}", courseId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<Course>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Delete course
        /// </summary>
        /// <param name="courseId">Course ID to delete</param>
        /// <returns>Deletion result</returns>
        /// <response code="200">Course deleted successfully</response>
        /// <response code="400">If the course ID is invalid</response>
        /// <response code="404">If the course is not found</response>
        /// <response code="409">If the course cannot be deleted due to dependencies</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpDelete("courses/{courseId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCourse(
            [FromRoute][Required] int courseId)
        {
            try
            {
                _logger.LogInformation("DELETE /api/skillgap/courses/{CourseId} - Request received", courseId);

                if (courseId <= 0)
                {
                    _logger.LogWarning("DeleteCourse called with invalid course ID: {CourseId}", courseId);
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Course ID must be greater than 0",
                        Data = false
                    });
                }

                var result = await _skillGapService.DeleteCourseAsync(courseId);

                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(result);
                    }
                    if (result.Message.Contains("assigned to employees") || result.Message.Contains("dependencies"))
                    {
                        return Conflict(result);
                    }
                    return BadRequest(result);
                }

                _logger.LogInformation("DeleteCourse completed for course ID: {CourseId}", courseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteCourse for course ID: {CourseId}", courseId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = false
                });
            }
        }

        /// <summary>
        /// Get all available departments
        /// </summary>
        /// <returns>List of department names</returns>
        /// <response code="200">Returns the list of departments</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("courses/departments")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetDepartments()
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/courses/departments - Request received");

                var result = await _skillGapService.GetDepartmentsAsync();

                _logger.LogInformation("GetDepartments completed, returned {Count} departments",
                    result.Data?.Count ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetDepartments");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new List<string>()
                });
            }
        }

        /// <summary>
        /// Get areas, optionally filtered by department
        /// </summary>
        /// <param name="department">Optional department filter</param>
        /// <returns>List of area names</returns>
        /// <response code="200">Returns the list of areas</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("courses/areas")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetAreas(
            [FromQuery] string? department = null)
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/courses/areas - Request received for department: {Department}",
                    department ?? "All");

                var result = await _skillGapService.GetAreasAsync(department);

                _logger.LogInformation("GetAreas completed, returned {Count} areas",
                    result.Data?.Count ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAreas");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new List<string>()
                });
            }
        }

        /// <summary>
        /// Get skills, optionally filtered by area
        /// </summary>
        /// <param name="area">Optional area filter</param>
        /// <returns>List of skill names</returns>
        /// <response code="200">Returns the list of skills</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("courses/skills")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetSkills(
            [FromQuery] string? area = null)
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/courses/skills - Request received for area: {Area}",
                    area ?? "All");

                var result = await _skillGapService.GetSkillsAsync(area);

                _logger.LogInformation("GetSkills completed, returned {Count} skills",
                    result.Data?.Count ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetSkills");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new List<string>()
                });
            }
        }

        /// <summary>
        /// Get all available course levels
        /// </summary>
        /// <returns>List of level names</returns>
        /// <response code="200">Returns the list of levels</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("courses/levels")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetLevels()
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/courses/levels - Request received");

                var result = await _skillGapService.GetLevelsAsync();

                _logger.LogInformation("GetLevels completed, returned {Count} levels",
                    result.Data?.Count ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetLevels");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new List<string>()
                });
            }
        }

        /// <summary>
        /// Get course statistics and summary information
        /// </summary>
        /// <returns>Course statistics</returns>
        /// <response code="200">Returns the course statistics</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("courses/statistics")]
        [ProducesResponseType(typeof(ApiResponse<CourseStatistics>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CourseStatistics>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<CourseStatistics>>> GetCourseStatistics()
        {
            try
            {
                _logger.LogInformation("GET /api/skillgap/courses/statistics - Request received");

                // Get basic statistics
                var searchRequest = new CourseSearchRequest { PageSize = 1 };
                var coursesResult = await _skillGapService.GetCoursesAsync(searchRequest);

                var departmentsResult = await _skillGapService.GetDepartmentsAsync();
                var areasResult = await _skillGapService.GetAreasAsync();
                var skillsResult = await _skillGapService.GetSkillsAsync();
                var levelsResult = await _skillGapService.GetLevelsAsync();

                var statistics = new CourseStatistics
                {
                    TotalCourses = coursesResult.Data?.TotalCount ?? 0,
                    TotalDepartments = departmentsResult.Data?.Count ?? 0,
                    TotalAreas = areasResult.Data?.Count ?? 0,
                    TotalSkills = skillsResult.Data?.Count ?? 0,
                    TotalLevels = levelsResult.Data?.Count ?? 0,
                    Departments = departmentsResult.Data ?? new List<string>(),
                    Areas = areasResult.Data ?? new List<string>(),
                    Skills = skillsResult.Data ?? new List<string>(),
                    Levels = levelsResult.Data ?? new List<string>()
                };

                _logger.LogInformation("GetCourseStatistics completed");

                return Ok(new ApiResponse<CourseStatistics>
                {
                    Success = true,
                    Message = "Course statistics retrieved successfully",
                    Data = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetCourseStatistics");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<CourseStatistics>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new CourseStatistics()
                });
            }
        }

        /// <summary>
        /// Bulk import courses from CSV or Excel file
        /// </summary>
        /// <param name="file">CSV or Excel file containing course data</param>
        /// <returns>Import result with success/failure counts</returns>
        /// <response code="200">Returns the import result</response>
        /// <response code="400">If the file is invalid or missing</response>
        /// <response code="500">If an internal server error occurs</response>
        //[HttpPost("courses/bulk-import")]
        //[Consumes("multipart/form-data")]

        //[ProducesResponseType(typeof(ApiResponse<BulkImportResult>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ApiResponse<BulkImportResult>), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ApiResponse<BulkImportResult>), StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult<ApiResponse<BulkImportResult>>> BulkImportCourses(
        //    [FromForm][Required] IFormFile file)
        //{
        //    try
        //    {
        //        _logger.LogInformation("POST /api/skillgap/courses/bulk-import - File: {FileName}", file?.FileName);

        //        if (file == null || file.Length == 0)
        //        {
        //            return BadRequest(new ApiResponse<BulkImportResult>
        //            {
        //                Success = false,
        //                Message = "File is required",
        //                Data = new BulkImportResult()
        //            });
        //        }

        //        // Validate file type
        //        var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
        //        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        //        if (!allowedExtensions.Contains(fileExtension))
        //        {
        //            return BadRequest(new ApiResponse<BulkImportResult>
        //            {
        //                Success = false,
        //                Message = "Only CSV and Excel files are supported",
        //                Data = new BulkImportResult()
        //            });
        //        }

        //        // TODO: Implement bulk import logic
        //        // This would involve:
        //        // 1. Reading the file content
        //        // 2. Parsing CSV/Excel data
        //        // 3. Validating each row
        //        // 4. Creating courses in batch
        //        // 5. Returning import results

        //        _logger.LogInformation("BulkImportCourses - Feature not yet implemented");

        //        return Ok(new ApiResponse<BulkImportResult>
        //        {
        //            Success = false,
        //            Message = "Bulk import feature is not yet implemented",
        //            Data = new BulkImportResult
        //            {
        //                TotalProcessed = 0,
        //                SuccessCount = 0,
        //                FailureCount = 0,
        //                Errors = new List<string> { "Bulk import feature is not yet implemented" }
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Unexpected error in BulkImportCourses");
        //        return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<BulkImportResult>
        //        {
        //            Success = false,
        //            Message = "An unexpected error occurred",
        //            Data = new BulkImportResult()
        //        });
        //    }
        //}

    }

    // Additional DTOs for controller requests
    /// <summary>
    /// Request model for adding courses
    /// </summary>
    public class AddCoursesRequest
    {
        /// <summary>
        /// List of courses to add or remove
        /// </summary>
        [Required]
        public List<AddCourse> Data { get; set; } = new();

        /// <summary>
        /// Manager's employee number who is managing the courses
        /// </summary>
        [Required]
        public string ManagerPF { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for rejecting a profile
    /// </summary>
    public class RejectProfileViewModel
    {
        /// <summary>
        /// Current skill gap view model
        /// </summary>
        [Required]
        public SkillGapViewModel ViewModel { get; set; } = new();

        /// <summary>
        /// Rejection request details
        /// </summary>
        [Required]
        public RejectProfileRequest RejectRequest { get; set; } = new();
    }

    /// <summary>
    /// Request model for approving a profile
    /// </summary>
    public class ApproveProfileViewModel
    {
        /// <summary>
        /// Current skill gap view model
        /// </summary>
        [Required]
        public SkillGapViewModel ViewModel { get; set; } = new();

        /// <summary>
        /// Profile ID to approve
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Profile ID must be greater than 0")]
        public int Id { get; set; }
    }

}
