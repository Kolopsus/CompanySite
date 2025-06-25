using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CompanySite.Models
{
    public class Report
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Reference { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string ReportExe { get; set; } = string.Empty;
        
        [Required]
        public DateTime Updated { get; set; }
    }

    public class DatabaseStatus
    {
        [Required]
        [StringLength(100)]
        public string Company { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Region { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Database { get; set; } = string.Empty;
        
        public DateTime? RefreshDate { get; set; }
        
        [Required]
        public DateTime ExpectedRefreshDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;
    }

    public class ClientServer
    {
        [Required]
        [StringLength(100)]
        public string ServerName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string ClientServerDns { get; set; } = string.Empty;
    }

    public class ClientCompany
    {
        [Required]
        [StringLength(100)]
        public string ServerName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ClientName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string ClientRegion { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ClientDatabase { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        [JsonIgnore] // Don't serialize credentials
        public string ClientDatabaseUser { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        [JsonIgnore] // Don't serialize credentials
        public string ClientDatabasePassword { get; set; } = string.Empty;
        
        [Range(0, 365)]
        public int RefreshValue { get; set; }
    }

    public class MyCompanyUser
    {
        [Required]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Invalid characters in username")]
        public string UserName { get; set; } = string.Empty;
        
        [Required]
        [RegularExpression("^[YN]$", ErrorMessage = "IsAuthorizer must be Y or N")]
        public string IsAuthorizer { get; set; } = string.Empty;
    }

    public class AccessRequestViewModel
    {
        [Required]
        [RegularExpression("^[AR]$", ErrorMessage = "RequestType must be A (Add) or R (Remove)")]
        public string RequestType { get; set; } = "A";
        
        [Required]
        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Invalid characters in name")]
        public string YourName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Invalid characters in employee name")]
        public string EmployeeName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50, MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z0-9\-_\.]+$", ErrorMessage = "Invalid characters in employee ID")]
        public string EmployeeId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000, MinimumLength = 5)]
        public string RequestDetails { get; set; } = string.Empty;
        
        public List<MyCompanyUser> Users { get; set; } = new List<MyCompanyUser>();
    }

    public class ChangeRequestViewModel
    {
        [Required]
        [RegularExpression("^(chargeable|notChargeable)$", ErrorMessage = "Invalid request type")]
        public string RequestType { get; set; } = "chargeable";
        
        [Required]
        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Invalid characters in authorizer name")]
        public string AuthorizedBy { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Company { get; set; } = string.Empty;
        
        public List<MyCompanyUser> Authorizers { get; set; } = new List<MyCompanyUser>();
        public List<string> Companies { get; set; } = new List<string>();
    }

    public class UserAccessRequest
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;
        
        [Required]
        [RegularExpression("^[AR]$", ErrorMessage = "RequestType must be A or R")]
        public string RequestType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Invalid characters in requestor name")]
        public string RequestorName { get; set; } = string.Empty;
        
        [Required]
        public DateTime CreateDate { get; set; }
        
        [Required]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Invalid characters in username")]
        public string UserName { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string RequestDetails { get; set; } = string.Empty;
    }

    public class Schedule
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Server { get; set; } = string.Empty;
        
        [Required]
        public DateTime LastRunDate { get; set; }
        
        [Required]
        [StringLength(50)]
        [RegularExpression("^(daily|NBD|Monthly|FBD|Weekly)$", ErrorMessage = "Invalid frequency")]
        public string Frequency { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ClientDatabase { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? DayOrDate { get; set; }
        
        [Required]
        [StringLength(50)]
        [RegularExpression("^(Success|Error|Running|Pending)$", ErrorMessage = "Invalid run state")]
        public string LastRunState { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string ReportName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ClientServer { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string OutputDirectory { get; set; } = string.Empty;
        
        public DateTime? NextRunDate { get; set; }
    }

    public class Holiday
    {
        [Required]
        public DateTime HolidayDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string HolidayRegion { get; set; } = string.Empty;
    }

    public class ScheduleDashboardViewModel
    {
        public List<Schedule> Schedules { get; set; } = new List<Schedule>();
        public bool TodayOnly { get; set; }
        
        [RegularExpression("^(All|Errors|Running|To Go)$", ErrorMessage = "Invalid filter")]
        public string Filter { get; set; } = "All";
        
        [Range(0, int.MaxValue)]
        public int DoneCount { get; set; }
        
        [Range(0, int.MaxValue)]
        public int ToGoCount { get; set; }
        
        [Range(0, int.MaxValue)]
        public int ErrorCount { get; set; }
    }

    // Enhanced error handling view models
    public class ErrorViewModel
    {
        [StringLength(100)]
        public string? RequestId { get; set; }
        
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        
        [Required]
        [StringLength(500)]
        public string ErrorMessage { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ErrorType { get; set; } = "General";
        
        public bool ShowDetails { get; set; } = true;
    }

    public class OperationResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        
        [StringLength(500)]
        public string ErrorMessage { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string ErrorType { get; set; } = string.Empty;
    }

    // Security-related models
    public class SecurityAuditLog
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [Required]
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string Resource { get; set; } = string.Empty;
        
        [StringLength(45)] // IPv6 max length
        public string IpAddress { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;
        
        public bool Success { get; set; }
        
        [StringLength(500)]
        public string Details { get; set; } = string.Empty;
    }
}