using Microsoft.Data.SqlClient;
using CompanySite.Models;
using System.Data;

namespace CompanySite.Services
{
    public class DatabaseService
    {
        private readonly IConfiguration _configuration;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<Report>> GetReportsAsync()
        {
            var reports = new List<Report>();
            using var connection = new SqlConnection(_configuration.GetConnectionString("ControlDatabase"));
            await connection.OpenAsync();
            
            using var command = new SqlCommand("SELECT Title, Reference, Description, ReportExe, Updated FROM Reports", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                reports.Add(new Report
                {
                    Title = reader.GetString(0),
                    Reference = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    ReportExe = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Updated = reader.GetDateTime(4)
                });
            }
            
            return reports;
        }

        public async Task<List<DatabaseStatus>> GetDatabaseStatusAsync()
        {
            var statuses = new List<DatabaseStatus>();
            var servers = new Dictionary<string, string>();
            var companies = new List<ClientCompany>();
            
            using var connection = new SqlConnection(_configuration.GetConnectionString("ControlDatabase"));
            await connection.OpenAsync();
            
            using (var command = new SqlCommand("SELECT ServerName, ClientServer FROM ClientServer", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    servers[reader.GetString(0)] = reader.GetString(1);
                }
            }
            
            using (var command = new SqlCommand("SELECT ServerName, ClientName, ClientRegion, ClientDatabase, ClientDatabaseUser, ClientDatabasePassword, RefreshValue FROM ClientCompany", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    companies.Add(new ClientCompany
                    {
                        ServerName = reader.GetString(0),
                        ClientName = reader.GetString(1),
                        ClientRegion = reader.GetString(2),
                        ClientDatabase = reader.GetString(3),
                        ClientDatabaseUser = reader.GetString(4),
                        ClientDatabasePassword = reader.GetString(5),
                        RefreshValue = reader.GetInt32(6)
                    });
                }
            }
            
            var serverConnections = new Dictionary<string, SqlConnection>();
            
            foreach (var serverGroup in companies.GroupBy(c => c.ServerName))
            {
                if (servers.TryGetValue(serverGroup.Key, out var serverDns))
                {
                    foreach (var company in serverGroup)
                    {
                        try
                        {
                            var clientConnString = $"Server={serverDns};Database={company.ClientDatabase};User Id={company.ClientDatabaseUser};Password={company.ClientDatabasePassword};TrustServerCertificate=True;Encrypt=True;";
                            using var clientConnection = new SqlConnection(clientConnString);
                            await clientConnection.OpenAsync();
                            
                            DateTime? refreshDate = null;
                            using (var command = new SqlCommand("SELECT TOP 1 RefreshDate FROM ClientControl", clientConnection))
                            {
                                var result = await command.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                {
                                    refreshDate = (DateTime)result;
                                }
                            }
                            
                            var expectedDate = DateTime.Today.AddDays(company.RefreshValue);
                            var status = refreshDate.HasValue && refreshDate.Value.Date >= expectedDate.Date ? "OK" : "Out of date";
                            
                            statuses.Add(new DatabaseStatus
                            {
                                Company = company.ClientName,
                                Region = company.ClientRegion,
                                Database = company.ClientDatabase,
                                RefreshDate = refreshDate,
                                ExpectedRefreshDate = expectedDate,
                                Status = status
                            });
                        }
                        catch
                        {
                            statuses.Add(new DatabaseStatus
                            {
                                Company = company.ClientName,
                                Region = company.ClientRegion,
                                Database = company.ClientDatabase,
                                RefreshDate = null,
                                ExpectedRefreshDate = DateTime.Today.AddDays(company.RefreshValue),
                                Status = "Connection Error"
                            });
                        }
                    }
                }
            }
            
            return statuses;
        }

        public async Task<List<MyCompanyUser>> GetMyCompanyUsersAsync()
        {
            var users = new List<MyCompanyUser>();
            using var connection = new SqlConnection(_configuration.GetConnectionString("ControlDatabase"));
            await connection.OpenAsync();
            
            using var command = new SqlCommand("SELECT UserName, IsAuthorizer FROM MyCompanyUsers", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                users.Add(new MyCompanyUser
                {
                    UserName = reader.GetString(0),
                    IsAuthorizer = reader.GetString(1)
                });
            }
            
            return users;
        }

        public async Task<List<string>> GetCompaniesAsync()
        {
            var companies = new List<string>();
            using var connection = new SqlConnection(_configuration.GetConnectionString("ControlDatabase"));
            await connection.OpenAsync();
            
            using var command = new SqlCommand("SELECT DISTINCT ClientName FROM ClientCompany", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                companies.Add(reader.GetString(0));
            }
            
            return companies;
        }

        public async Task SubmitAccessRequestAsync(string requestType, string requestorName, string userName, string userId, string requestDetails)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("ControlDatabase"));
            await connection.OpenAsync();
            
            using var command = new SqlCommand(@"INSERT INTO UserAccessRequest (RequestType, RequestorName, UserName, UserId, RequestDetails, CreateDate, Status) 
                                               VALUES (@RequestType, @RequestorName, @UserName, @UserId, @RequestDetails, @CreateDate, @Status)", connection);
            
            command.Parameters.AddWithValue("@RequestType", requestType);
            command.Parameters.AddWithValue("@RequestorName", requestorName);
            command.Parameters.AddWithValue("@UserName", userName);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@RequestDetails", requestDetails);
            command.Parameters.AddWithValue("@CreateDate", DateTime.Now);
            command.Parameters.AddWithValue("@Status", "new");
            
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<UserAccessRequest>> GetAccessRequestsAsync(bool outstandingOnly)
        {
            var requests = new List<UserAccessRequest>();
            using var connection = new SqlConnection(_configuration.GetConnectionString("ControlDatabase"));
            await connection.OpenAsync();
            
            var query = "SELECT Id, Status, RequestType, RequestorName, CreateDate, UserName, RequestDetails FROM UserAccessRequest";
            if (outstandingOnly)
            {
                query += " WHERE Status = 'new'";
            }
            
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                requests.Add(new UserAccessRequest
                {
                    Id = reader.GetInt32(0),
                    Status = reader.GetString(1),
                    RequestType = reader.GetString(2),
                    RequestorName = reader.GetString(3),
                    CreateDate = reader.GetDateTime(4),
                    UserName = reader.GetString(5),
                    RequestDetails = reader.IsDBNull(6) ? "" : reader.GetString(6)
                });
            }
            
            return requests;
        }

        public async Task<(List<Schedule>, List<Holiday>)> GetSchedulesAndHolidaysAsync()
        {
            var schedules = new List<Schedule>();
            var holidays = new List<Holiday>();
            
            using var connection = new SqlConnection(_configuration.GetConnectionString("ControlDatabase"));
            await connection.OpenAsync();
            
            using (var command = new SqlCommand(@"SELECT Id, Server, LastRunDate, Frequency, ClientDatabase, DayOrDate, 
                                                       LastRunState, ReportName, ClientServer, OutputDirectory 
                                                FROM Schedules", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    schedules.Add(new Schedule
                    {
                        Id = reader.GetInt32(0),
                        Server = reader.GetString(1),
                        LastRunDate = reader.GetDateTime(2),
                        Frequency = reader.GetString(3),
                        ClientDatabase = reader.GetString(4),
                        DayOrDate = reader.IsDBNull(5) ? null : reader.GetString(5),
                        LastRunState = reader.GetString(6),
                        ReportName = reader.GetString(7),
                        ClientServer = reader.GetString(8),
                        OutputDirectory = reader.GetString(9)
                    });
                }
            }
            
            using (var command = new SqlCommand("SELECT HolidayDate, HolidayRegion FROM Holidays", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    holidays.Add(new Holiday
                    {
                        HolidayDate = reader.GetDateTime(0),
                        HolidayRegion = reader.GetString(1)
                    });
                }
            }
            
            return (schedules, holidays);
        }

        public async Task<Dictionary<string, string>> GetClientRegionsAsync()
        {
            var regions = new Dictionary<string, string>();
            using var connection = new SqlConnection(_configuration.GetConnectionString("ControlDatabase"));
            await connection.OpenAsync();
            
            using var command = new SqlCommand("SELECT ClientDatabase, ClientRegion FROM ClientCompany", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                regions[reader.GetString(0)] = reader.GetString(1);
            }
            
            return regions;
        }
    }
}