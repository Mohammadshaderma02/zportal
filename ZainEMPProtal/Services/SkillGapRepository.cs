using Microsoft.Data.SqlClient;
using System.Data;
using ZainEMPProtal.Models.SkillGapAnalysis;

namespace ZainEMPProtal.Services
{
    public interface ISkillGapRepository
    {
        Task<SkillGapViewModel> GetViewDataAsync(string employeeNumber);
        Task<SkillGapViewModel> GetEmployeeCoursesDataAsync(SkillGapViewModel model);
        Task<int> CheckPreviousPhaseAsync(string employeeNumber);
        Task<bool> AddCoursesAsync(List<AddCourse> data, string managerPF);
        Task<List<Profiles>> GetProfilesAsync(string directorPF);
        Task<Profiles> GetMyProfileAsync(string directorPF);
        Task<List<EmployeeCourses>> GetProfilesCoursesAsync(List<Profiles> profiles);
        Task<SkillGapViewModel> GetSubEmployeesListAsync(SkillGapViewModel model);
        Task SubmitProfileAsync(string employeeNumber, string supervisorNumber, string managerFullName, bool isDirector);
        Task<SkillGapViewModel> RejectProfileAsync(SkillGapViewModel viewModel, int id, string rejectionReason, bool sendToHR);
        Task<SkillGapViewModel> ApproveProfileAsync(SkillGapViewModel viewModel, int id);
        Task AddFeedbackAsync(string employeeNumber, string employeeName, string feedback, string area, string skill, string course);
        Task<ApiResponse<PaginatedCourseResult>> GetCoursesAsync(CourseSearchRequest request);
        Task<ApiResponse<Course>> GetCourseByIdAsync(int courseId);
        Task<ApiResponse<Course>> CreateCourseAsync(CreateCourseRequest request);
        Task<ApiResponse<Course>> UpdateCourseAsync(UpdateCourseRequest request);
        Task<ApiResponse<bool>> DeleteCourseAsync(int courseId);
        Task<ApiResponse<List<string>>> GetDepartmentsAsync();
        Task<ApiResponse<List<string>>> GetAreasAsync(string? department = null);
        Task<ApiResponse<List<string>>> GetSkillsAsync(string? area = null);
        Task<ApiResponse<List<string>>> GetLevelsAsync();
    }
    public class SkillGapRepository : ISkillGapRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<SkillGapRepository> _logger;

        public SkillGapRepository(IConfiguration configuration, ILogger<SkillGapRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task<SkillGapViewModel> GetViewDataAsync(string employeeNumber)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var employee = await GetEmployeeAsync(connection, employeeNumber);
                if (employee == null || string.IsNullOrEmpty(employee.EmployeeNumber))
                    return null;

                var model = new SkillGapViewModel { Employee = employee };

                // Get employees list
                model.EmployeesList = await GetEmployeesListAsync(connection, employee.EmployeeNumber);

                // If manager, get sub-employees
                if (model.Employee.Job.isManager())
                {
                    var subEmployees = new List<Employee>();
                    foreach (var item in model.EmployeesList)
                    {
                        var subs = await GetEmployeesListAsync(connection, item.EmployeeNumber);
                        subEmployees.AddRange(subs);
                    }
                    model.EmployeesList.AddRange(subEmployees);
                }

                var employeesPFs = model.EmployeesList.Select(x => x.EmployeeNumber).ToList();

                // Get employee courses
                model.EmployeeCourses = await GetEmployeeCoursesAsync(connection, employee.EmployeeNumber, employeesPFs);

                // Calculate progress
                if (employeesPFs?.Count > 0)
                {
                    var countEmployeesWithCourses = model.EmployeeCourses
                        .Where(x => employeesPFs.Contains(x.EmployeePF))
                        .GroupBy(x => x.EmployeePF)
                        .Count();
                    model.Progress = $"{countEmployeesWithCourses}/{model.EmployeesList.Count}";
                }

                // Get courses list
                if (model.isDirector)
                {
                    model.CoursesList = await GetCoursesAsync(connection, employee.Department, null);
                }
                else
                {
                    model.CoursesList = await GetCoursesAsync(connection, employee.Department, employee.Division);
                }

                // Check if can submit
                var profile = await GetProfileAsync(connection, employee.EmployeeNumber, employee.SupervisorNumber);
                model.CanSubmit = profile == null || !profile.isSubmitted;

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetViewDataAsync for employee: {EmployeeNumber}", employeeNumber);
                return null;
            }
        }

        public async Task<SkillGapViewModel> GetEmployeeCoursesDataAsync(SkillGapViewModel model)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var employeesPFs = await GetEmployeeNumbersAsync(connection, model.Employee.EmployeeNumber);
                model.EmployeeCourses = await GetEmployeeCoursesAsync(connection, model.Employee.EmployeeNumber, employeesPFs);

                if (employeesPFs?.Count > 0)
                {
                    var countEmployeesWithCourses = model.EmployeeCourses.Count;
                    model.Progress = $"{countEmployeesWithCourses}/{model.EmployeesList.Count}";
                }

                var profile = await GetProfileAsync(connection, model.Employee.EmployeeNumber, model.Employee.SupervisorNumber);
                model.CanSubmit = profile == null || !profile.isSubmitted;

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEmployeeCoursesDataAsync");
                return model;
            }
        }
        private async Task<List<EmployeeCourses>> GetEmployeeCoursesAsync(SqlConnection connection, string managerPF, List<string> employeesPFs)
        {
            var courses = new List<EmployeeCourses>();

            // 1. Courses managed by this manager but not for himself
            var command1 = new SqlCommand(@"
        SELECT Id, EmployeePF, CourseID, ManagerPF 
        FROM EmployeeCourses 
        WHERE ManagerPF = @ManagerPF AND ManagerPF != EmployeePF", connection);
            command1.Parameters.AddWithValue("@ManagerPF", managerPF);

            await using (var reader = await command1.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    courses.Add(new EmployeeCourses
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        EmployeePF = reader.GetString(reader.GetOrdinal("EmployeePF")),
                        CourseID = reader.GetInt32(reader.GetOrdinal("CourseID")),
                        ManagerPF = reader.GetString(reader.GetOrdinal("ManagerPF"))
                    });
                }
            }

            // 2. Courses of the employees in employeesPFs
            if (employeesPFs?.Any() == true)
            {
                foreach (var pf in employeesPFs.Distinct())
                {
                    var command2 = new SqlCommand(@"
                SELECT Id, EmployeePF, CourseID, ManagerPF 
                FROM EmployeeCourses 
                WHERE EmployeePF = @EmployeePF", connection);
                    command2.Parameters.AddWithValue("@EmployeePF", pf);

                    await using (var reader = await command2.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            courses.Add(new EmployeeCourses
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                EmployeePF = reader.GetString(reader.GetOrdinal("EmployeePF")),
                                CourseID = reader.GetInt32(reader.GetOrdinal("CourseID")),
                                ManagerPF = reader.GetString(reader.GetOrdinal("ManagerPF"))
                            });
                        }
                    }
                }
            }

            // 3. Courses for the manager himself
            var command3 = new SqlCommand(@"
        SELECT Id, EmployeePF, CourseID, ManagerPF 
        FROM EmployeeCourses 
        WHERE EmployeePF = @EmployeePF", connection);
            command3.Parameters.AddWithValue("@EmployeePF", managerPF);

            await using (var reader = await command3.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    courses.Add(new EmployeeCourses
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        EmployeePF = reader.GetString(reader.GetOrdinal("EmployeePF")),
                        CourseID = reader.GetInt32(reader.GetOrdinal("CourseID")),
                        ManagerPF = reader.GetString(reader.GetOrdinal("ManagerPF"))
                    });
                }
            }

            return courses;
        }

        public async Task<int> CheckPreviousPhaseAsync(string employeeNumber)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand("SELECT COUNT(*) FROM PreviousPhaseEmployees WHERE EmployeeNumber = @EmployeeNumber", connection);
                command.Parameters.AddWithValue("@EmployeeNumber", employeeNumber);

                var count = (int)await command.ExecuteScalarAsync();
                return count > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckPreviousPhaseAsync");
                return 0;
            }
        }

        public async Task<bool> AddCoursesAsync(List<AddCourse> data, string managerPF)
        {
            try
            {
                if (data == null || !data.Any()) return false;

                var addList = data.Where(x => x.Add).ToList();
                if (!addList.Any()) return false;

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                try
                {
                    var empPF = addList.First().EmployeePF;

                    // Delete existing courses for employee
                    var deleteCmd = new SqlCommand("DELETE FROM EmployeeCourses WHERE EmployeePF = @EmployeePF", connection, transaction);
                    deleteCmd.Parameters.AddWithValue("@EmployeePF", empPF);
                    await deleteCmd.ExecuteNonQueryAsync();

                    // Add new course
                    var item = addList.First();
                    var insertCmd = new SqlCommand(
                        "INSERT INTO EmployeeCourses (EmployeePF, CourseID, ManagerPF) VALUES (@EmployeePF, @CourseID, @ManagerPF)",
                        connection, transaction);
                    insertCmd.Parameters.AddWithValue("@EmployeePF", item.EmployeePF);
                    insertCmd.Parameters.AddWithValue("@CourseID", int.Parse(item.CourseID));
                    insertCmd.Parameters.AddWithValue("@ManagerPF", managerPF);
                    await insertCmd.ExecuteNonQueryAsync();

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddCoursesAsync");
                return false;
            }
        }

        public async Task<List<Profiles>> GetProfilesAsync(string directorPF)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
            SELECT ID, ManagerPF, DirectorPF, isApproved, isSubmitted
            FROM Profiles
            WHERE DirectorPF = @DirectorPF AND isSubmitted = 1", connection);

                command.Parameters.AddWithValue("@DirectorPF", directorPF);

                var profiles = new List<Profiles>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    profiles.Add(new Profiles
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("ID")),
                        ManagerPF = reader.IsDBNull(reader.GetOrdinal("ManagerPF")) ? null : reader.GetString(reader.GetOrdinal("ManagerPF")),
                        DirectorPF = reader.IsDBNull(reader.GetOrdinal("DirectorPF")) ? null : reader.GetString(reader.GetOrdinal("DirectorPF")),
                        isApproved = !reader.IsDBNull(reader.GetOrdinal("isApproved")) && reader.GetBoolean(reader.GetOrdinal("isApproved")),
                        isSubmitted = !reader.IsDBNull(reader.GetOrdinal("isSubmitted")) && reader.GetBoolean(reader.GetOrdinal("isSubmitted"))
                    });
                }

                return profiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProfilesAsync for DirectorPF: {DirectorPF}", directorPF);
                return new List<Profiles>();
            }
        }

        public async Task<Profiles> GetMyProfileAsync(string directorPF)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
            SELECT ID, ManagerPF, DirectorPF, isApproved, isSubmitted 
            FROM Profiles 
            WHERE ManagerPF = @ManagerPF AND isSubmitted = 1", connection);

                command.Parameters.AddWithValue("@ManagerPF", directorPF);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Profiles
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("ID")),
                        ManagerPF = reader.IsDBNull(reader.GetOrdinal("ManagerPF")) ? null : reader.GetString(reader.GetOrdinal("ManagerPF")),
                        DirectorPF = reader.IsDBNull(reader.GetOrdinal("DirectorPF")) ? null : reader.GetString(reader.GetOrdinal("DirectorPF")),
                        isApproved = !reader.IsDBNull(reader.GetOrdinal("isApproved")) && reader.GetBoolean(reader.GetOrdinal("isApproved")),
                        isSubmitted = !reader.IsDBNull(reader.GetOrdinal("isSubmitted")) && reader.GetBoolean(reader.GetOrdinal("isSubmitted"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyProfileAsync for ManagerPF: {ManagerPF}", directorPF);
                return null;
            }
        }

        public async Task<List<EmployeeCourses>> GetProfilesCoursesAsync(List<Profiles> profiles)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var employeeCourses = new List<EmployeeCourses>();

                foreach (var profile in profiles)
                {
                    var command = new SqlCommand(@"
                SELECT Id, EmployeePF, CourseID, ManagerPF
                FROM EmployeeCourses
                WHERE ManagerPF = @ManagerPF", connection);

                    command.Parameters.AddWithValue("@ManagerPF", profile.ManagerPF);

                    await using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        employeeCourses.Add(new EmployeeCourses
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            EmployeePF = reader.IsDBNull(reader.GetOrdinal("EmployeePF")) ? null : reader.GetString(reader.GetOrdinal("EmployeePF")),
                            CourseID = reader.GetInt32(reader.GetOrdinal("CourseID")),
                            ManagerPF = reader.IsDBNull(reader.GetOrdinal("ManagerPF")) ? null : reader.GetString(reader.GetOrdinal("ManagerPF"))
                        });
                    }

                    await reader.CloseAsync(); // Close reader before next iteration
                }

                return employeeCourses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProfilesCoursesAsync");
                return new List<EmployeeCourses>();
            }
        }

        public async Task<SkillGapViewModel> GetSubEmployeesListAsync(SkillGapViewModel model)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var employees = new List<Employee>();
                var employeeCourses = new List<EmployeeCourses>();

                var directReports = await GetEmployeesListAsync(connection, model.Employee.EmployeeNumber);

                if (directReports?.Any() == true)
                {
                    // Add non-managers
                    employees.AddRange(directReports.Where(x => !x.Job.isManager() && !x.Job.isDirector()));

                    // Get courses for direct manager
                    var managerCourses = await GetManagerCoursesAsync(connection, model.Employee.EmployeeNumber);
                    employeeCourses.AddRange(managerCourses);

                    // Handle managers in the list
                    foreach (var item in directReports.Where(x => x.Job.isManager()))
                    {
                        employees.Add(item);
                        var subEmployees = await GetEmployeesListAsync(connection, item.EmployeeNumber);
                        employees.AddRange(subEmployees);

                        var subManagerCourses = await GetManagerCoursesAsync(connection, item.EmployeeNumber);
                        employeeCourses.AddRange(subManagerCourses);
                    }

                    model.SubEmployeesList = employees;
                    model.EmployeeCourses = employeeCourses;
                }

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSubEmployeesListAsync");
                return model;
            }
        }

        public async Task SubmitProfileAsync(string employeeNumber, string supervisorNumber, string managerFullName, bool isDirector)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                try
                {
                    var director = await GetEmployeeAsync(connection, supervisorNumber, transaction);
                    var profile = await GetProfileAsync(connection, employeeNumber, supervisorNumber, transaction);

                    int profileId;
                    if (profile == null)
                    {
                        var insertCmd = new SqlCommand(
                            "INSERT INTO Profiles (ManagerPF, DirectorPF, isApproved, isSubmitted) OUTPUT INSERTED.ID VALUES (@ManagerPF, @DirectorPF, @isApproved, @isSubmitted)",
                            connection, transaction);
                        insertCmd.Parameters.AddWithValue("@ManagerPF", employeeNumber);
                        insertCmd.Parameters.AddWithValue("@DirectorPF", supervisorNumber);
                        insertCmd.Parameters.AddWithValue("@isApproved", isDirector);
                        insertCmd.Parameters.AddWithValue("@isSubmitted", true);
                        profileId = (int)await insertCmd.ExecuteScalarAsync();
                    }
                    else
                    {
                        var updateCmd = new SqlCommand(
                            "UPDATE Profiles SET isApproved = @isApproved, isSubmitted = @isSubmitted WHERE ID = @ID",
                            connection, transaction);
                        updateCmd.Parameters.AddWithValue("@isApproved", isDirector);
                        updateCmd.Parameters.AddWithValue("@isSubmitted", true);
                        updateCmd.Parameters.AddWithValue("@ID", profile.ID);
                        await updateCmd.ExecuteNonQueryAsync();
                        profileId = profile.ID;
                    }

                    // Insert profile status
                    var statusCmd = new SqlCommand(
                        "INSERT INTO ProfileStatus (ProfileID, Status, CreatedOn) VALUES (@ProfileID, @Status, @CreatedOn)",
                        connection, transaction);
                    statusCmd.Parameters.AddWithValue("@ProfileID", profileId);
                    statusCmd.Parameters.AddWithValue("@Status", "Submit");
                    statusCmd.Parameters.AddWithValue("@CreatedOn", DateTime.Now.ToString());
                    await statusCmd.ExecuteNonQueryAsync();

                    transaction.Commit();

                    // Send email notification (implement email service)
                    if (director != null && !director.Job.Title.ToLower().Contains("ceo"))
                    {
                        // TODO: Implement email service
                        // await _emailService.SendSubmitProfileEmailAsync(director.EmailAddress, director.FirstName, managerFullName);
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitProfileAsync");
                throw;
            }
        }

        public async Task<SkillGapViewModel> RejectProfileAsync(SkillGapViewModel viewModel, int id, string rejectionReason, bool sendToHR)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                try
                {
                    var updateCmd = new SqlCommand(
                        "UPDATE Profiles SET isApproved = 0, isSubmitted = 0 WHERE ID = @ID",
                        connection, transaction);
                    updateCmd.Parameters.AddWithValue("@ID", id);
                    await updateCmd.ExecuteNonQueryAsync();

                    var statusCmd = new SqlCommand(
                        "INSERT INTO ProfileStatus (ProfileID, Status, StatusReason, CreatedOn) VALUES (@ProfileID, @Status, @StatusReason, @CreatedOn)",
                        connection, transaction);
                    statusCmd.Parameters.AddWithValue("@ProfileID", id);
                    statusCmd.Parameters.AddWithValue("@Status", "Reject");
                    statusCmd.Parameters.AddWithValue("@StatusReason", rejectionReason);
                    statusCmd.Parameters.AddWithValue("@CreatedOn", DateTime.Now.ToString());
                    await statusCmd.ExecuteNonQueryAsync();

                    transaction.Commit();

                    // Get manager for email notification
                    var manager = await GetManagerByDirectorAsync(connection, viewModel.Employee.EmployeeNumber);
                    if (manager != null && !manager.Job.Title.ToLower().Contains("ceo"))
                    {
                        // TODO: Implement email service
                        // await _emailService.SendRejectProfileEmailAsync(manager.EmailAddress, manager.FirstName, rejectionReason);
                        // if (sendToHR)
                        //     await _emailService.SendRejectProfileHREmailAsync(manager.FirstName, rejectionReason);
                    }

                    // Refresh data
                    viewModel.Profiles = await GetProfilesAsync(viewModel.Employee.EmployeeNumber);
                    viewModel.MyProfile = await GetMyProfileAsync(viewModel.Employee.EmployeeNumber);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RejectProfileAsync");
                return viewModel;
            }
        }

        public async Task<SkillGapViewModel> ApproveProfileAsync(SkillGapViewModel viewModel, int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                try
                {
                    var updateCmd = new SqlCommand(
                        "UPDATE Profiles SET isApproved = 1, isSubmitted = 1 WHERE ID = @ID",
                        connection, transaction);
                    updateCmd.Parameters.AddWithValue("@ID", id);
                    await updateCmd.ExecuteNonQueryAsync();

                    var statusCmd = new SqlCommand(
                        "INSERT INTO ProfileStatus (ProfileID, Status, CreatedOn) VALUES (@ProfileID, @Status, @CreatedOn)",
                        connection, transaction);
                    statusCmd.Parameters.AddWithValue("@ProfileID", id);
                    statusCmd.Parameters.AddWithValue("@Status", "Approve");
                    statusCmd.Parameters.AddWithValue("@CreatedOn", DateTime.Now.ToString());
                    await statusCmd.ExecuteNonQueryAsync();

                    transaction.Commit();

                    // Get manager for email notification
                    var manager = await GetManagerByDirectorAsync(connection, viewModel.Employee.EmployeeNumber);
                    if (manager != null && !manager.Job.Title.ToLower().Contains("ceo"))
                    {
                        // TODO: Implement email service
                        // await _emailService.SendApproveProfileEmailAsync(manager.EmailAddress, manager.FirstName);
                    }

                    // Refresh data
                    viewModel.Profiles = await GetProfilesAsync(viewModel.Employee.EmployeeNumber);
                    viewModel.MyProfile = await GetMyProfileAsync(viewModel.Employee.EmployeeNumber);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApproveProfileAsync");
                return viewModel;
            }
        }

        public async Task AddFeedbackAsync(string employeeNumber, string employeeName, string feedback, string area, string skill, string course)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(
                    "INSERT INTO Feedbacks (EmployeeNumber, EmployeeName, Comment, CourseArea, CourseSkill, CourseName, CreatedOn) VALUES (@EmployeeNumber, @EmployeeName, @Comment, @CourseArea, @CourseSkill, @CourseName, @CreatedOn)",
                    connection);
                command.Parameters.AddWithValue("@EmployeeNumber", employeeNumber);
                command.Parameters.AddWithValue("@EmployeeName", employeeName);
                command.Parameters.AddWithValue("@Comment", feedback);
                command.Parameters.AddWithValue("@CourseArea", area ?? string.Empty);
                command.Parameters.AddWithValue("@CourseSkill", skill ?? string.Empty);
                command.Parameters.AddWithValue("@CourseName", course ?? string.Empty);
                command.Parameters.AddWithValue("@CreatedOn", DateTime.Now.ToString());

                await command.ExecuteNonQueryAsync();

                // Send email notification
                bool hasCourse = !string.IsNullOrEmpty(area);
                // TODO: Implement email service
                // await _emailService.SendFeedbackHREmailAsync(employeeName, hasCourse, area, skill, course, feedback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddFeedbackAsync");
                throw;
            }
        }

        // Helper methods
        private async Task<Employee> GetEmployeeAsync(SqlConnection connection, string employeeNumber, SqlTransaction transaction = null)
        {
            var command = new SqlCommand(@"
        SELECT e.Id, e.EmployeeNumber, e.FirstName, e.LastName, e.EmailAddress,
               e.SupervisorNumber, e.Department, e.Division,
               e.Job as JobTitle 
        FROM Employees e 
  
        WHERE LOWER(e.EmployeeNumber) = LOWER(@EmployeeNumber)", connection, transaction);

            command.Parameters.AddWithValue("@EmployeeNumber", employeeNumber);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Employee
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EmployeeNumber = reader.GetString(reader.GetOrdinal("EmployeeNumber")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    EmailAddress = reader.GetString(reader.GetOrdinal("EmailAddress")),
                    SupervisorNumber = reader.IsDBNull(reader.GetOrdinal("SupervisorNumber")) ? null : reader.GetString(reader.GetOrdinal("SupervisorNumber")),
                    Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? null : reader.GetString(reader.GetOrdinal("Department")),
                    Division = reader.IsDBNull(reader.GetOrdinal("Division")) ? null : reader.GetString(reader.GetOrdinal("Division")),
                    Job = new Job
                    {
                        Title = reader.IsDBNull(reader.GetOrdinal("JobTitle")) ? null : reader.GetString(reader.GetOrdinal("JobTitle"))
                    }
                };
            }

            return null;
        }
        private async Task<List<Employee>> GetEmployeesListAsync(SqlConnection connection, string supervisorNumber)
        {
            var command = new SqlCommand(@"
        SELECT e.Id, e.EmployeeNumber, e.FirstName, e.LastName, e.EmailAddress,
               e.SupervisorNumber, e.Department, e.Division,
               e.Job as JobTitle 
        FROM Employees e 
        WHERE e.SupervisorNumber = @SupervisorNumber 
        ORDER BY e.FirstName", connection);

            command.Parameters.AddWithValue("@SupervisorNumber", supervisorNumber);

            var employees = new List<Employee>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                employees.Add(new Employee
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EmployeeNumber = reader.GetString(reader.GetOrdinal("EmployeeNumber")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    EmailAddress = reader.GetString(reader.GetOrdinal("EmailAddress")),
                    SupervisorNumber = reader.IsDBNull(reader.GetOrdinal("SupervisorNumber")) ? null : reader.GetString(reader.GetOrdinal("SupervisorNumber")),
                    Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? null : reader.GetString(reader.GetOrdinal("Department")),
                    Division = reader.IsDBNull(reader.GetOrdinal("Division")) ? null : reader.GetString(reader.GetOrdinal("Division")),
                    Job = new Job
                    {
                        Title = reader.IsDBNull(reader.GetOrdinal("JobTitle")) ? null : reader.GetString(reader.GetOrdinal("JobTitle"))
                    }
                });
            }

            return employees;
        }
        private async Task<List<string>> GetEmployeeNumbersAsync(SqlConnection connection, string supervisorNumber)
        {
            var command = new SqlCommand("SELECT EmployeeNumber FROM Employees WHERE SupervisorNumber = @SupervisorNumber", connection);
            command.Parameters.AddWithValue("@SupervisorNumber", supervisorNumber);

            var numbers = new List<string>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                numbers.Add(reader.GetString(reader.GetOrdinal("EmployeeNumber")));
            }

            return numbers;
        }

        private async Task<List<EmployeeCourses>> GetManagerCoursesAsync(SqlConnection connection, string managerPF)
        {
            var command = new SqlCommand(@"
        SELECT Id, EmployeePF, CourseID, ManagerPF 
        FROM EmployeeCourses 
        WHERE ManagerPF = @ManagerPF", connection);

            command.Parameters.AddWithValue("@ManagerPF", managerPF);

            var courses = new List<EmployeeCourses>();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                courses.Add(new EmployeeCourses
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EmployeePF = reader.IsDBNull(reader.GetOrdinal("EmployeePF")) ? null : reader.GetString(reader.GetOrdinal("EmployeePF")),
                    CourseID = reader.GetInt32(reader.GetOrdinal("CourseID")),
                    ManagerPF = reader.IsDBNull(reader.GetOrdinal("ManagerPF")) ? null : reader.GetString(reader.GetOrdinal("ManagerPF"))
                });
            }

            return courses;
        }
        private async Task<List<Course>> GetCoursesAsync(SqlConnection connection, string department, string division = null)
        {
            try
            {


                var sql = division == null
                    ? "SELECT  ID,CourseNo,MainDepartment,Area,Skill,CourseName,Level,Link,CourseDivision FROM Courses WHERE MainDepartment = @Department"
                    : "SELECT  ID,CourseNo,MainDepartment,Area,Skill,CourseName,Level,Link,CourseDivision FROM Courses WHERE MainDepartment = @Department AND CourseDivision = @Division";

                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Department", department);
                if (division != null)
                    command.Parameters.AddWithValue("@Division", division);

                var courses = new List<Course>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    courses.Add(new Course
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("CourseName")),
                        MainDepartment = reader.IsDBNull(reader.GetOrdinal("MainDepartment")) ? null : reader.GetString(reader.GetOrdinal("MainDepartment")),
                        CourseDivision = reader.IsDBNull(reader.GetOrdinal("CourseDivision")) ? null : reader.GetString(reader.GetOrdinal("CourseDivision")),
                        Area = reader.IsDBNull(reader.GetOrdinal("Area")) ? null : reader.GetString(reader.GetOrdinal("Area")),
                        Skill = reader.IsDBNull(reader.GetOrdinal("Skill")) ? null : reader.GetString(reader.GetOrdinal("Skill")),
                        Link = reader.IsDBNull(reader.GetOrdinal("Link")) ? null : reader.GetString(reader.GetOrdinal("Link")),
                        Level = reader.IsDBNull(reader.GetOrdinal("Level")) ? null : reader.GetString(reader.GetOrdinal("Level"))
                    });
                }

                return courses;
            } catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        private async Task<Profiles> GetProfileAsync(SqlConnection connection, string managerPF, string directorPF, SqlTransaction transaction = null)
        {
            var command = new SqlCommand(@"
        SELECT ID, ManagerPF, DirectorPF, isApproved, isSubmitted 
        FROM Profiles 
        WHERE ManagerPF = @ManagerPF AND DirectorPF = @DirectorPF", connection, transaction);

            command.Parameters.AddWithValue("@ManagerPF", managerPF);
            command.Parameters.AddWithValue("@DirectorPF", directorPF);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Profiles
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    ManagerPF = reader.IsDBNull(reader.GetOrdinal("ManagerPF")) ? null : reader.GetString(reader.GetOrdinal("ManagerPF")),
                    DirectorPF = reader.IsDBNull(reader.GetOrdinal("DirectorPF")) ? null : reader.GetString(reader.GetOrdinal("DirectorPF")),
                    isApproved = !reader.IsDBNull(reader.GetOrdinal("isApproved")) && reader.GetBoolean(reader.GetOrdinal("isApproved")),
                    isSubmitted = !reader.IsDBNull(reader.GetOrdinal("isSubmitted")) && reader.GetBoolean(reader.GetOrdinal("isSubmitted"))
                };
            }

            return null;
        }

        private async Task<Employee> GetManagerByDirectorAsync(SqlConnection connection, string directorNumber)
        {
            var command = new SqlCommand(@"
        SELECT e.Id, e.EmployeeNumber, e.FirstName, e.LastName, e.EmailAddress,
               e.SupervisorNumber, e.Department, e.Division,
               j.Title AS JobTitle
        FROM Employees e
        LEFT JOIN Jobs j ON e.JobId = j.Id
        WHERE e.SupervisorNumber = @DirectorNumber", connection);

            command.Parameters.AddWithValue("@DirectorNumber", directorNumber);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Employee
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EmployeeNumber = reader.IsDBNull(reader.GetOrdinal("EmployeeNumber")) ? null : reader.GetString(reader.GetOrdinal("EmployeeNumber")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                    EmailAddress = reader.IsDBNull(reader.GetOrdinal("EmailAddress")) ? null : reader.GetString(reader.GetOrdinal("EmailAddress")),
                    SupervisorNumber = reader.IsDBNull(reader.GetOrdinal("SupervisorNumber")) ? null : reader.GetString(reader.GetOrdinal("SupervisorNumber")),
                    Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? null : reader.GetString(reader.GetOrdinal("Department")),
                    Division = reader.IsDBNull(reader.GetOrdinal("Division")) ? null : reader.GetString(reader.GetOrdinal("Division")),
                    Job = new Job
                    {
                        Title = reader.IsDBNull(reader.GetOrdinal("JobTitle")) ? null : reader.GetString(reader.GetOrdinal("JobTitle"))
                    }
                };
            }

            return null;
        }
    
     /// <summary>
    /// Get courses with search and pagination
    /// </summary>
    public async Task<ApiResponse<PaginatedCourseResult>> GetCoursesAsync(CourseSearchRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var whereConditions = new List<string>();
                var parameters = new List<SqlParameter>();

                // Build dynamic WHERE clause
                if (!string.IsNullOrWhiteSpace(request.CourseNo))
                {
                    whereConditions.Add("CourseNo LIKE @CourseNo");
                    parameters.Add(new SqlParameter("@CourseNo", $"%{request.CourseNo.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(request.MainDepartment))
                {
                    whereConditions.Add("MainDepartment LIKE @MainDepartment");
                    parameters.Add(new SqlParameter("@MainDepartment", $"%{request.MainDepartment.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(request.Area))
                {
                    whereConditions.Add("Area LIKE @Area");
                    parameters.Add(new SqlParameter("@Area", $"%{request.Area.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(request.Skill))
                {
                    whereConditions.Add("Skill LIKE @Skill");
                    parameters.Add(new SqlParameter("@Skill", $"%{request.Skill.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(request.CourseName))
                {
                    whereConditions.Add("CourseName LIKE @CourseName");
                    parameters.Add(new SqlParameter("@CourseName", $"%{request.CourseName.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(request.Level))
                {
                    whereConditions.Add("Level = @Level");
                    parameters.Add(new SqlParameter("@Level", request.Level.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(request.CourseDivision))
                {
                    whereConditions.Add("CourseDivision LIKE @CourseDivision");
                    parameters.Add(new SqlParameter("@CourseDivision", $"%{request.CourseDivision.Trim()}%"));
                }

                var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

                // Count query
                var countSql = $"SELECT COUNT(*) FROM Courses {whereClause}";
                var countCommand = new SqlCommand(countSql, connection);
                foreach (var param in parameters)
                {
                    countCommand.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                }

                var totalCount = (int)await countCommand.ExecuteScalarAsync();

                // Data query with pagination
                var offset = (request.PageNumber - 1) * request.PageSize;
                var dataSql = $@"
                SELECT ID, CourseNo, MainDepartment, Area, Skill, CourseName, Level, Link, CourseDivision
                FROM Courses 
                {whereClause}
                ORDER BY CourseName
                OFFSET @Offset ROWS 
                FETCH NEXT @PageSize ROWS ONLY";

                var dataCommand = new SqlCommand(dataSql, connection);
                foreach (var param in parameters)
                {
                    dataCommand.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                }
                dataCommand.Parameters.Add(new SqlParameter("@Offset", offset));
                dataCommand.Parameters.Add(new SqlParameter("@PageSize", request.PageSize));

                var courses = new List<Course>();
                using var reader = await dataCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    courses.Add(new Course
                    {
                        Id = reader.GetInt32("ID"),
                        CourseNo = reader.IsDBNull("CourseNo") ? string.Empty : reader.GetString("CourseNo"),
                        MainDepartment = reader.IsDBNull("MainDepartment") ? string.Empty : reader.GetString("MainDepartment"),
                        Area = reader.IsDBNull("Area") ? string.Empty : reader.GetString("Area"),
                        Skill = reader.IsDBNull("Skill") ? string.Empty : reader.GetString("Skill"),
                        Name = reader.IsDBNull("CourseName") ? string.Empty : reader.GetString("CourseName"),
                        Level = reader.IsDBNull("Level") ? string.Empty : reader.GetString("Level"),
                        Link = reader.IsDBNull("Link") ? string.Empty : reader.GetString("Link"),
                        CourseDivision = reader.IsDBNull("CourseDivision") ? string.Empty : reader.GetString("CourseDivision")
                    });
                }

                var result = new PaginatedCourseResult
                {
                    Courses = courses,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                return new ApiResponse<PaginatedCourseResult>
                {
                    Success = true,
                    Message = $"Retrieved {courses.Count} courses from {totalCount} total",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCoursesAsync");
                return new ApiResponse<PaginatedCourseResult>
                {
                    Success = false,
                    Message = "An error occurred while retrieving courses",
                    Data = new PaginatedCourseResult()
                };
            }
        }

        /// <summary>
        /// Get course by ID
        /// </summary>
        public async Task<ApiResponse<Course>> GetCourseByIdAsync(int courseId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                SELECT ID, CourseNo, MainDepartment, Area, Skill, CourseName, Level, Link, CourseDivision
                FROM Courses 
                WHERE ID = @CourseId";

                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var course = new Course
                    {
                        Id = reader.GetInt32("ID"),
                        CourseNo = reader.IsDBNull("CourseNo") ? string.Empty : reader.GetString("CourseNo"),
                        MainDepartment = reader.IsDBNull("MainDepartment") ? string.Empty : reader.GetString("MainDepartment"),
                        Area = reader.IsDBNull("Area") ? string.Empty : reader.GetString("Area"),
                        Skill = reader.IsDBNull("Skill") ? string.Empty : reader.GetString("Skill"),
                        Name = reader.IsDBNull("CourseName") ? string.Empty : reader.GetString("CourseName"),
                        Level = reader.IsDBNull("Level") ? string.Empty : reader.GetString("Level"),
                        Link = reader.IsDBNull("Link") ? string.Empty : reader.GetString("Link"),
                        CourseDivision = reader.IsDBNull("CourseDivision") ? string.Empty : reader.GetString("CourseDivision")
                    };

                    return new ApiResponse<Course>
                    {
                        Success = true,
                        Message = "Course retrieved successfully",
                        Data = course
                    };
                }

                return new ApiResponse<Course>
                {
                    Success = false,
                    Message = "Course not found",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCourseByIdAsync for ID: {CourseId}", courseId);
                return new ApiResponse<Course>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the course",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Create new course
        /// </summary>
        public async Task<ApiResponse<Course>> CreateCourseAsync(CreateCourseRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if course number already exists
                var checkSql = "SELECT COUNT(*) FROM Courses WHERE CourseNo = @CourseNo";
                var checkCommand = new SqlCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@CourseNo", request.CourseNo.Trim());

                var existingCount = (int)await checkCommand.ExecuteScalarAsync();
                if (existingCount > 0)
                {
                    return new ApiResponse<Course>
                    {
                        Success = false,
                        Message = "Course number already exists",
                        Data = null
                    };
                }

                var sql = @"
                INSERT INTO Courses (CourseNo, MainDepartment, Area, Skill, CourseName, Level, Link, CourseDivision)
                OUTPUT INSERTED.ID, INSERTED.CourseNo, INSERTED.MainDepartment, INSERTED.Area, 
                       INSERTED.Skill, INSERTED.CourseName, INSERTED.Level, INSERTED.Link, INSERTED.CourseDivision
                VALUES (@CourseNo, @MainDepartment, @Area, @Skill, @CourseName, @Level, @Link, @CourseDivision)";

                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@CourseNo", request.CourseNo.Trim());
                command.Parameters.AddWithValue("@MainDepartment", request.MainDepartment.Trim());
                command.Parameters.AddWithValue("@Area", request.Area.Trim());
                command.Parameters.AddWithValue("@Skill", request.Skill.Trim());
                command.Parameters.AddWithValue("@CourseName", request.CourseName.Trim());
                command.Parameters.AddWithValue("@Level", string.IsNullOrWhiteSpace(request.Level) ? DBNull.Value : request.Level.Trim());
                command.Parameters.AddWithValue("@Link", string.IsNullOrWhiteSpace(request.Link) ? DBNull.Value : request.Link.Trim());
                command.Parameters.AddWithValue("@CourseDivision", string.IsNullOrWhiteSpace(request.CourseDivision) ? DBNull.Value : request.CourseDivision.Trim());

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var course = new Course
                    {
                        Id = reader.GetInt32("ID"),
                        CourseNo = reader.GetString("CourseNo"),
                        MainDepartment = reader.GetString("MainDepartment"),
                        Area = reader.GetString("Area"),
                        Skill = reader.GetString("Skill"),
                        Name = reader.GetString("CourseName"),
                        Level = reader.IsDBNull("Level") ? string.Empty : reader.GetString("Level"),
                        Link = reader.IsDBNull("Link") ? string.Empty : reader.GetString("Link"),
                        CourseDivision = reader.IsDBNull("CourseDivision") ? string.Empty : reader.GetString("CourseDivision")
                    };

                    return new ApiResponse<Course>
                    {
                        Success = true,
                        Message = "Course created successfully",
                        Data = course
                    };
                }

                return new ApiResponse<Course>
                {
                    Success = false,
                    Message = "Failed to create course",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateCourseAsync");
                return new ApiResponse<Course>
                {
                    Success = false,
                    Message = "An error occurred while creating the course",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Update existing course
        /// </summary>
        public async Task<ApiResponse<Course>> UpdateCourseAsync(UpdateCourseRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if course exists
                var checkSql = "SELECT COUNT(*) FROM Courses WHERE ID = @ID";
                var checkCommand = new SqlCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@ID", request.ID);

                var existingCount = (int)await checkCommand.ExecuteScalarAsync();
                if (existingCount == 0)
                {
                    return new ApiResponse<Course>
                    {
                        Success = false,
                        Message = "Course not found",
                        Data = null
                    };
                }

                // Check if course number is taken by another course
                var duplicateCheckSql = "SELECT COUNT(*) FROM Courses WHERE CourseNo = @CourseNo AND ID != @ID";
                var duplicateCheckCommand = new SqlCommand(duplicateCheckSql, connection);
                duplicateCheckCommand.Parameters.AddWithValue("@CourseNo", request.CourseNo.Trim());
                duplicateCheckCommand.Parameters.AddWithValue("@ID", request.ID);

                var duplicateCount = (int)await duplicateCheckCommand.ExecuteScalarAsync();
                if (duplicateCount > 0)
                {
                    return new ApiResponse<Course>
                    {
                        Success = false,
                        Message = "Course number already exists for another course",
                        Data = null
                    };
                }

                var sql = @"
                UPDATE Courses 
                SET CourseNo = @CourseNo, 
                    MainDepartment = @MainDepartment, 
                    Area = @Area, 
                    Skill = @Skill, 
                    CourseName = @CourseName, 
                    Level = @Level, 
                    Link = @Link, 
                    CourseDivision = @CourseDivision
                WHERE ID = @ID;
                
                SELECT ID, CourseNo, MainDepartment, Area, Skill, CourseName, Level, Link, CourseDivision
                FROM Courses WHERE ID = @ID";

                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ID", request.ID);
                command.Parameters.AddWithValue("@CourseNo", request.CourseNo.Trim());
                command.Parameters.AddWithValue("@MainDepartment", request.MainDepartment.Trim());
                command.Parameters.AddWithValue("@Area", request.Area.Trim());
                command.Parameters.AddWithValue("@Skill", request.Skill.Trim());
                command.Parameters.AddWithValue("@CourseName", request.CourseName.Trim());
                command.Parameters.AddWithValue("@Level", string.IsNullOrWhiteSpace(request.Level) ? DBNull.Value : request.Level.Trim());
                command.Parameters.AddWithValue("@Link", string.IsNullOrWhiteSpace(request.Link) ? DBNull.Value : request.Link.Trim());
                command.Parameters.AddWithValue("@CourseDivision", string.IsNullOrWhiteSpace(request.CourseDivision) ? DBNull.Value : request.CourseDivision.Trim());

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var course = new Course
                    {
                        Id = reader.GetInt32("ID"),
                        CourseNo = reader.GetString("CourseNo"),
                        MainDepartment = reader.GetString("MainDepartment"),
                        Area = reader.GetString("Area"),
                        Skill = reader.GetString("Skill"),
                        Name = reader.GetString("CourseName"),
                        Level = reader.IsDBNull("Level") ? string.Empty : reader.GetString("Level"),
                        Link = reader.IsDBNull("Link") ? string.Empty : reader.GetString("Link"),
                        CourseDivision = reader.IsDBNull("CourseDivision") ? string.Empty : reader.GetString("CourseDivision")
                    };

                    return new ApiResponse<Course>
                    {
                        Success = true,
                        Message = "Course updated successfully",
                        Data = course
                    };
                }

                return new ApiResponse<Course>
                {
                    Success = false,
                    Message = "Failed to update course",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateCourseAsync for ID: {CourseId}", request.ID);
                return new ApiResponse<Course>
                {
                    Success = false,
                    Message = "An error occurred while updating the course",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Delete course (soft delete or hard delete based on business rules)
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteCourseAsync(int courseId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if course exists
                var checkSql = "SELECT COUNT(*) FROM Courses WHERE ID = @ID";
                var checkCommand = new SqlCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@ID", courseId);

                var existingCount = (int)await checkCommand.ExecuteScalarAsync();
                if (existingCount == 0)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Course not found",
                        Data = false
                    };
                }

                // Check if course is being used in EmployeeCourses
                var usageCheckSql = "SELECT COUNT(*) FROM EmployeeCourses WHERE CourseID = @CourseID";
                var usageCheckCommand = new SqlCommand(usageCheckSql, connection);
                usageCheckCommand.Parameters.AddWithValue("@CourseID", courseId);

                var usageCount = (int)await usageCheckCommand.ExecuteScalarAsync();
                if (usageCount > 0)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Cannot delete course as it is currently assigned to employees",
                        Data = false
                    };
                }

                // Hard delete the course
                var deleteSql = "DELETE FROM Courses WHERE ID = @ID";
                var deleteCommand = new SqlCommand(deleteSql, connection);
                deleteCommand.Parameters.AddWithValue("@ID", courseId);

                var rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

                return new ApiResponse<bool>
                {
                    Success = rowsAffected > 0,
                    Message = rowsAffected > 0 ? "Course deleted successfully" : "Failed to delete course",
                    Data = rowsAffected > 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteCourseAsync for ID: {CourseId}", courseId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the course",
                    Data = false
                };
            }
        }

        /// <summary>
        /// Get distinct departments
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetDepartmentsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT DISTINCT MainDepartment FROM Courses WHERE MainDepartment IS NOT NULL AND MainDepartment != '' ORDER BY MainDepartment";
                var command = new SqlCommand(sql, connection);

                var departments = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    departments.Add(reader.GetString("MainDepartment"));
                }

                return new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = $"Retrieved {departments.Count} departments",
                    Data = departments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDepartmentsAsync");
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving departments",
                    Data = new List<string>()
                };
            }
        }

        /// <summary>
        /// Get distinct areas, optionally filtered by department
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetAreasAsync(string? department = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT DISTINCT Area FROM Courses WHERE Area IS NOT NULL AND Area != ''";
                var command = new SqlCommand(sql, connection);

                if (!string.IsNullOrWhiteSpace(department))
                {
                    sql += " AND MainDepartment = @Department";
                    command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@Department", department);
                }

                sql += " ORDER BY Area";
                command = new SqlCommand(sql, connection);
                if (!string.IsNullOrWhiteSpace(department))
                {
                    command.Parameters.AddWithValue("@Department", department);
                }

                var areas = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    areas.Add(reader.GetString("Area"));
                }

                return new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = $"Retrieved {areas.Count} areas",
                    Data = areas
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAreasAsync");
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving areas",
                    Data = new List<string>()
                };
            }
        }

        /// <summary>
        /// Get distinct skills, optionally filtered by area
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetSkillsAsync(string? area = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT DISTINCT Skill FROM Courses WHERE Skill IS NOT NULL AND Skill != ''";
                var command = new SqlCommand(sql, connection);

                if (!string.IsNullOrWhiteSpace(area))
                {
                    sql += " AND Area = @Area";
                    command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@Area", area);
                }

                sql += " ORDER BY Skill";
                command = new SqlCommand(sql, connection);
                if (!string.IsNullOrWhiteSpace(area))
                {
                    command.Parameters.AddWithValue("@Area", area);
                }

                var skills = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    skills.Add(reader.GetString("Skill"));
                }

                return new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = $"Retrieved {skills.Count} skills",
                    Data = skills
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSkillsAsync");
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving skills",
                    Data = new List<string>()
                };
            }
        }

        /// <summary>
        /// Get distinct levels
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetLevelsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT DISTINCT Level FROM Courses WHERE Level IS NOT NULL AND Level != '' ORDER BY Level";
                var command = new SqlCommand(sql, connection);

                var levels = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    levels.Add(reader.GetString("Level"));
                }

                return new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = $"Retrieved {levels.Count} levels",
                    Data = levels
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLevelsAsync");
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving levels",
                    Data = new List<string>()
                };
            }
        } }
    }
