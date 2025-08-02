using Microsoft.Data.SqlClient;
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
                ? "SELECT SELECT ID,CourseNo,MainDepartment,Area,Skill,CourseName,Level,Link,CourseDivision FROM Courses WHERE MainDepartment = @Department"
                : "SELECT SELECT ID,CourseNo,MainDepartment,Area,Skill,CourseName,Level,Link,CourseDivision FROM Courses WHERE MainDepartment = @Department AND CourseDivision = @Division";

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
            }catch(Exception ex)
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
    }
}
