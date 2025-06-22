namespace CompanySite.Models
{
    public class Report
    {
        public string Title { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportExe { get; set; } = string.Empty;
        public DateTime Updated { get; set; }
    }

    public class DatabaseStatus
    {
        public string Company { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public DateTime? RefreshDate { get; set; }
        public DateTime ExpectedRefreshDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ClientServer
    {
        public string ServerName { get; set; } = string.Empty;
        public string ClientServerDns { get; set; } = string.Empty;
    }

    public class ClientCompany
    {
        public string ServerName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ClientRegion { get; set; } = string.Empty;
        public string ClientDatabase { get; set; } = string.Empty;
        public string ClientDatabaseUser { get; set; } = string.Empty;
        public string ClientDatabasePassword { get; set; } = string.Empty;
        public int RefreshValue { get; set; }
    }

    public class MyCompanyUser
    {
        public string UserName { get; set; } = string.Empty;
        public string IsAuthorizer { get; set; } = string.Empty;
    }

    public class AccessRequestViewModel
    {
        public string RequestType { get; set; } = "A";
        public string YourName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string RequestDetails { get; set; } = string.Empty;
        public List<MyCompanyUser> Users { get; set; } = new List<MyCompanyUser>();
    }

    public class ChangeRequestViewModel
    {
        public string RequestType { get; set; } = "chargeable";
        public string AuthorizedBy { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public List<MyCompanyUser> Authorizers { get; set; } = new List<MyCompanyUser>();
        public List<string> Companies { get; set; } = new List<string>();
    }

    public class UserAccessRequest
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string RequestorName { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string RequestDetails { get; set; } = string.Empty;
    }

    public class Schedule
    {
        public int Id { get; set; }
        public string Server { get; set; } = string.Empty;
        public DateTime LastRunDate { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public string ClientDatabase { get; set; } = string.Empty;
        public string? DayOrDate { get; set; }
        public string LastRunState { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string ClientServer { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public DateTime? NextRunDate { get; set; }
    }

    public class Holiday
    {
        public DateTime HolidayDate { get; set; }
        public string HolidayRegion { get; set; } = string.Empty;
    }

    public class ScheduleDashboardViewModel
    {
        public List<Schedule> Schedules { get; set; } = new List<Schedule>();
        public bool TodayOnly { get; set; }
        public string Filter { get; set; } = "All";
        public int DoneCount { get; set; }
        public int ToGoCount { get; set; }
        public int ErrorCount { get; set; }
    }
}