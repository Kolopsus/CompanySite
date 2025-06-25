using Microsoft.Data.SqlClient;
using CompanySite.Models;
using System.Data;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace CompanySite.Services
{
    public class DatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("ControlDatabase") 
                ?? throw new InvalidOperationException("Control database connection string is not configured");
        }

        public async Task<List<Report>> GetReportsAsync()
        {
            var reports = new List<Report>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // Use parameterized query even though no user input here
                const string sql = "SELECT Title, Reference, Description, ReportExe, Updated FROM Reports WHERE 1=1 ORDER BY Title";
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var report = new Report
                    {
                        Title = GetSafeString(reader, 0),
                        Reference = GetSafeString(reader, 1),
                        Description = GetSafeString(reader, 2),
                        ReportExe = GetSafeString(reader, 3),
                        Updated = reader.GetDateTime(4)
                    };

                    // Validate the model
                    if (ValidateModel(report))
                    {
                        reports.Add(report);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid report data found with Reference: {Reference}", report.Reference);
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching reports. Error Number: {ErrorNumber}", ex.Number);
                throw new InvalidOperationException("Database service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching reports");
                throw new InvalidOperationException("Service temporarily unavailable. Please try again later.");
            }
            
            return reports;
        }

        public async Task<List<DatabaseStatus>> GetDatabaseStatusAsync()
        {
            var statuses = new List<DatabaseStatus>();
            var servers = new Dictionary<string, string>();
            var companies = new List<ClientCompany>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // Get server information
                const string serverSql = "SELECT ServerName, ClientServer FROM ClientServer WHERE ServerName IS NOT NULL AND ClientServer IS NOT NULL";
                using (var command = new SqlCommand(serverSql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var serverName = GetSafeString(reader, 0);
                        var clientServer = GetSafeString(reader, 1);
                        
                        if (!string.IsNullOrWhiteSpace(serverName) && !string.IsNullOrWhiteSpace(clientServer))
                        {
                            servers[serverName] = clientServer;
                        }
                    }
                }
                
                // Get company information
                const string companySql = @"SELECT ServerName, ClientName, ClientRegion, ClientDatabase, 
                                           ClientDatabaseUser, ClientDatabasePassword, RefreshValue 
                                           FROM ClientCompany 
                                           WHERE ServerName IS NOT NULL AND ClientDatabase IS NOT NULL";
                using (var command = new SqlCommand(companySql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var company = new ClientCompany
                        {
                            ServerName = GetSafeString(reader, 0),
                            ClientName = GetSafeString(reader, 1),
                            ClientRegion = GetSafeString(reader, 2),
                            ClientDatabase = GetSafeString(reader, 3),
                            ClientDatabaseUser = GetSafeString(reader, 4),
                            ClientDatabasePassword = GetSafeString(reader, 5),
                            RefreshValue = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
                        };

                        if (ValidateModel(company) && ValidateClientCompany(company))
                        {
                            companies.Add(company);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid client company data found: {Database}", company.ClientDatabase);
                        }
                    }
                }
                
                // Process each server group
                foreach (var serverGroup in companies.GroupBy(c => c.ServerName))
                {
                    if (servers.TryGetValue(serverGroup.Key, out var serverDns))
                    {
                        foreach (var company in serverGroup)
                        {
                            var status = await GetDatabaseStatusForCompany(company, serverDns);
                            if (status != null)
                            {
                                statuses.Add(status);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Server DNS not found for server: {ServerName}", serverGroup.Key);
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching database status. Error Number: {ErrorNumber}", ex.Number);
                throw new InvalidOperationException("Database service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching database status");
                throw new InvalidOperationException("Service temporarily unavailable. Please try again later.");
            }
            
            return statuses;
        }

        private async Task<DatabaseStatus?> GetDatabaseStatusForCompany(ClientCompany company, string serverDns)
        {
            try
            {
                // Validate connection string components
                if (!ValidateConnectionStringComponents(serverDns, company.ClientDatabase, company.ClientDatabaseUser))
                {
                    _logger.LogWarning("Invalid connection string components for database: {Database}", company.ClientDatabase);
                    return CreateErrorStatus(company, "Configuration Error");
                }

                var clientConnString = BuildSecureConnectionString(serverDns, company.ClientDatabase, 
                    company.ClientDatabaseUser, company.ClientDatabasePassword);
                
                using var clientConnection = new SqlConnection(clientConnString);
                await clientConnection.OpenAsync();
                
                DateTime? refreshDate = null;
                const string refreshSql = "SELECT TOP 1 RefreshDate FROM ClientControl WHERE RefreshDate IS NOT NULL ORDER BY RefreshDate DESC";
                using (var command = new SqlCommand(refreshSql, clientConnection))
                {
                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        refreshDate = (DateTime)result;
                    }
                }
                
                var expectedDate = DateTime.Today.AddDays(-Math.Abs(company.RefreshValue));
                var status = refreshDate.HasValue && refreshDate.Value.Date >= expectedDate.Date ? "OK" : "Out of date";
                
                return new DatabaseStatus
                {
                    Company = SanitizeString(company.ClientName),
                    Region = SanitizeString(company.ClientRegion),
                    Database = SanitizeString(company.ClientDatabase),
                    RefreshDate = refreshDate,
                    ExpectedRefreshDate = expectedDate,
                    Status = status
                };
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "Failed to connect to client database: {Database}, Error: {ErrorNumber}", 
                    company.ClientDatabase, ex.Number);
                return CreateErrorStatus(company, "Connection Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error connecting to database: {Database}", company.ClientDatabase);
                return CreateErrorStatus(company, "Connection Error");
            }
        }

        private DatabaseStatus CreateErrorStatus(ClientCompany company, string status)
        {
            return new DatabaseStatus
            {
                Company = SanitizeString(company.ClientName),
                Region = SanitizeString(company.ClientRegion),
                Database = SanitizeString(company.ClientDatabase),
                RefreshDate = null,
                ExpectedRefreshDate = DateTime.Today.AddDays(-Math.Abs(company.RefreshValue)),
                Status = status
            };
        }

        public async Task<List<MyCompanyUser>> GetMyCompanyUsersAsync()
        {
            var users = new List<MyCompanyUser>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                const string sql = "SELECT UserName, IsAuthorizer FROM MyCompanyUsers WHERE UserName IS NOT NULL ORDER BY UserName";
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var user = new MyCompanyUser
                    {
                        UserName = GetSafeString(reader, 0),
                        IsAuthorizer = GetSafeString(reader, 1)
                    };

                    if (ValidateModel(user) && ValidateUserName(user.UserName))
                    {
                        users.Add(user);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid user data found: {UserName}", user.UserName);
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching users. Error Number: {ErrorNumber}", ex.Number);
                throw new InvalidOperationException("Database service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching users");
                throw new InvalidOperationException("Service temporarily unavailable. Please try again later.");
            }
            
            return users;
        }

        public async Task<List<string>> GetCompaniesAsync()
        {
            var companies = new List<string>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                const string sql = "SELECT DISTINCT ClientName FROM ClientCompany WHERE ClientName IS NOT NULL ORDER BY ClientName";
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var companyName = GetSafeString(reader, 0);
                    if (!string.IsNullOrWhiteSpace(companyName) && ValidateCompanyName(companyName))
                    {
                        companies.Add(SanitizeString(companyName));
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching companies. Error Number: {ErrorNumber}", ex.Number);
                throw new InvalidOperationException("Database service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching companies");
                throw new InvalidOperationException("Service temporarily unavailable. Please try again later.");
            }
            
            return companies;
        }

        public async Task SubmitAccessRequestAsync(string requestType, string requestorName, string userName, 
            string userId, string requestDetails)
        {
            // Validate inputs
            if (!ValidateAccessRequestInputs(requestType, requestorName, userName, userId, requestDetails))
            {
                throw new ArgumentException("Invalid input parameters provided");
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                const string sql = @"INSERT INTO UserAccessRequest (RequestType, RequestorName, UserName, UserId, RequestDetails, CreateDate, Status) 
                                   VALUES (@RequestType, @RequestorName, @UserName, @UserId, @RequestDetails, @CreateDate, @Status)";
                
                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add("@RequestType", SqlDbType.NVarChar, 1).Value = requestType;
                command.Parameters.Add("@RequestorName", SqlDbType.NVarChar, 100).Value = SanitizeString(requestorName);
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 100).Value = SanitizeString(userName);
                command.Parameters.Add("@UserId", SqlDbType.NVarChar, 50).Value = SanitizeString(userId);
                command.Parameters.Add("@RequestDetails", SqlDbType.NVarChar, 1000).Value = SanitizeString(requestDetails);
                command.Parameters.Add("@CreateDate", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = "new";
                
                await command.ExecuteNonQueryAsync();
                
                _logger.LogInformation("Access request submitted for user: {UserName} by requestor: {RequestorName}", 
                    SanitizeString(userName), SanitizeString(requestorName));
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while submitting access request. Error Number: {ErrorNumber}", ex.Number);
                throw new InvalidOperationException("Unable to submit request. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while submitting access request");
                throw new InvalidOperationException("Service temporarily unavailable. Please try again later.");
            }
        }

        public async Task<List<UserAccessRequest>> GetAccessRequestsAsync(bool outstandingOnly)
        {
            var requests = new List<UserAccessRequest>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"SELECT Id, Status, RequestType, RequestorName, CreateDate, UserName, RequestDetails 
                           FROM UserAccessRequest WHERE 1=1";
                
                if (outstandingOnly)
                {
                    sql += " AND Status = @Status";
                }
                
                sql += " ORDER BY CreateDate DESC";
                
                using var command = new SqlCommand(sql, connection);
                if (outstandingOnly)
                {
                    command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = "new";
                }
                
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var request = new UserAccessRequest
                    {
                        Id = reader.GetInt32(0),
                        Status = GetSafeString(reader, 1),
                        RequestType = GetSafeString(reader, 2),
                        RequestorName = GetSafeString(reader, 3),
                        CreateDate = reader.GetDateTime(4),
                        UserName = GetSafeString(reader, 5),
                        RequestDetails = GetSafeString(reader, 6)
                    };

                    if (ValidateModel(request))
                    {
                        requests.Add(request);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid access request data found with ID: {RequestId}", request.Id);
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching access requests. Error Number: {ErrorNumber}", ex.Number);
                throw new InvalidOperationException("Database service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching access requests");
                throw new InvalidOperationException("Service temporarily unavailable. Please try again later.");
            }
            
            return requests;
        }

        public async Task<(List<Schedule>, List<Holiday>)> GetSchedulesAndHolidaysAsync()
        {
            var schedules = new List<Schedule>();
            var holidays = new List<Holiday>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // Get schedules
                const string scheduleSql = @"SELECT Id, Server, LastRunDate, Frequency, ClientDatabase, DayOrDate, 
                                           LastRunState, ReportName, ClientServer, OutputDirectory 
                                    FROM Schedules WHERE Id IS NOT NULL ORDER BY Id";
                using (var command = new SqlCommand(scheduleSql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var schedule = new Schedule
                        {
                            Id = reader.GetInt32(0),
                            Server = GetSafeString(reader, 1),
                            LastRunDate = reader.GetDateTime(2),
                            Frequency = GetSafeString(reader, 3),
                            ClientDatabase = GetSafeString(reader, 4),
                            DayOrDate = reader.IsDBNull(5) ? null : GetSafeString(reader, 5),
                            LastRunState = GetSafeString(reader, 6),
                            ReportName = GetSafeString(reader, 7),
                            ClientServer = GetSafeString(reader, 8),
                            OutputDirectory = GetSafeString(reader, 9)
                        };

                        if (ValidateModel(schedule))
                        {
                            schedules.Add(schedule);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid schedule data found with ID: {ScheduleId}", schedule.Id);
                        }
                    }
                }
                
                // Get holidays
                const string holidaySql = "SELECT HolidayDate, HolidayRegion FROM Holidays WHERE HolidayDate IS NOT NULL ORDER BY HolidayDate";
                using (var command = new SqlCommand(holidaySql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var holiday = new Holiday
                        {
                            HolidayDate = reader.GetDateTime(0),
                            HolidayRegion = GetSafeString(reader, 1)
                        };

                        if (ValidateModel(holiday))
                        {
                            holidays.Add(holiday);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching schedules and holidays. Error Number: {ErrorNumber}", ex.Number);
                throw new InvalidOperationException("Database service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching schedules and holidays");
                throw new InvalidOperationException("Service temporarily unavailable. Please try again later.");
            }
            
            return (schedules, holidays);
        }

        public async Task<Dictionary<string, string>> GetClientRegionsAsync()
        {
            var regions = new Dictionary<string, string>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                const string sql = "SELECT ClientDatabase, ClientRegion FROM ClientCompany WHERE ClientDatabase IS NOT NULL AND ClientRegion IS NOT NULL";
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var database = GetSafeString(reader, 0);
                    var region = GetSafeString(reader, 1);
                    
                    if (!string.IsNullOrWhiteSpace(database) && !string.IsNullOrWhiteSpace(region))
                    {
                        regions[SanitizeString(database)] = SanitizeString(region);
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching client regions. Error Number: {ErrorNumber}", ex.Number);
                throw new InvalidOperationException("Database service is temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching client regions");
                throw new InvalidOperationException("Service temporarily unavailable. Please try again later.");
            }
            
            return regions;
        }

        // Helper methods for security and validation
        private static string GetSafeString(SqlDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : reader.GetString(index).Trim();
        }

        private static string SanitizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            
            // Remove potentially dangerous characters but preserve legitimate ones
            return Regex.Replace(input.Trim(), @"[<>""']", "", RegexOptions.None);
        }

        private static bool ValidateModel<T>(T model) where T : class
        {
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            return Validator.TryValidateObject(model, context, results, true);
        }

        private static bool ValidateUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName) || userName.Length > 100)
                return false;
            
            return Regex.IsMatch(userName, @"^[a-zA-Z\s\-'\.]+$", RegexOptions.None);
        }

        private static bool ValidateCompanyName(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName) || companyName.Length > 100)
                return false;
            
            return Regex.IsMatch(companyName, @"^[a-zA-Z0-9\s\-'\.&()]+$", RegexOptions.None);
        }

        private static bool ValidateClientCompany(ClientCompany company)
        {
            // Additional validation beyond data annotations
            if (string.IsNullOrWhiteSpace(company.ServerName) ||
                string.IsNullOrWhiteSpace(company.ClientDatabase) ||
                company.RefreshValue < 0 || company.RefreshValue > 365)
            {
                return false;
            }
            
            return true;
        }

        private static bool ValidateAccessRequestInputs(string requestType, string requestorName, 
            string userName, string userId, string requestDetails)
        {
            if (requestType != "A" && requestType != "R")
                return false;
            
            if (string.IsNullOrWhiteSpace(requestorName) || requestorName.Length > 100 ||
                !Regex.IsMatch(requestorName, @"^[a-zA-Z\s\-'\.]+$", RegexOptions.None))
                return false;
            
            if (string.IsNullOrWhiteSpace(userName) || userName.Length > 100 ||
                !Regex.IsMatch(userName, @"^[a-zA-Z\s\-'\.]+$", RegexOptions.None))
                return false;
            
            if (string.IsNullOrWhiteSpace(userId) || userId.Length > 50 ||
                !Regex.IsMatch(userId, @"^[a-zA-Z0-9\-_\.]+$", RegexOptions.None))
                return false;
            
            if (string.IsNullOrWhiteSpace(requestDetails) || requestDetails.Length > 1000)
                return false;
            
            return true;
        }

        private static bool ValidateConnectionStringComponents(string serverDns, string database, string user)
        {
            if (string.IsNullOrWhiteSpace(serverDns) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(user))
                return false;
            
            // Basic validation for SQL injection attempts in connection string components
            var dangerousPatterns = new[] { ";", "--", "/*", "*/", "xp_", "sp_", "drop", "delete", "insert", "update", "exec" };
            
            foreach (var pattern in dangerousPatterns)
            {
                if (serverDns.ToLowerInvariant().Contains(pattern) ||
                    database.ToLowerInvariant().Contains(pattern) ||
                    user.ToLowerInvariant().Contains(pattern))
                {
                    return false;
                }
            }
            
            return true;
        }

        private static string BuildSecureConnectionString(string server, string database, string user, string password)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                UserID = user,
                Password = password,
                TrustServerCertificate = true,
                Encrypt = true,
                ConnectTimeout = 5,
                CommandTimeout = 30,
                ApplicationName = "CompanySite",
                // Security settings
                Pooling = true,
                MaxPoolSize = 10,
                MinPoolSize = 1,
                // Prevent certain security risks
                IntegratedSecurity = false,
                PersistSecurityInfo = false
            };
            
            return builder.ConnectionString;
        }
    }
}