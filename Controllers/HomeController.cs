using Microsoft.AspNetCore.Mvc;
using CompanySite.Models;
using CompanySite.Services;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;

namespace CompanySite.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseService _dbService;
        private readonly ExcelService _excelService;
        private readonly ILogger<HomeController> _logger;
        private readonly HtmlEncoder _htmlEncoder;

        public HomeController(DatabaseService dbService, ExcelService excelService, ILogger<HomeController> logger, HtmlEncoder htmlEncoder)
        {
            _dbService = dbService;
            _excelService = excelService;
            _logger = logger;
            _htmlEncoder = htmlEncoder;
        }

        public IActionResult Index()
        {
            ViewData["SubpageTitle"] = "Home";
            return View();
        }

        public async Task<IActionResult> AvailableReports()
        {
            ViewData["SubpageTitle"] = "Available Reports";
            try
            {
                var reports = await _dbService.GetReportsAsync();
                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available reports for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return View("ErrorPage", new ErrorViewModel 
                { 
                    ErrorMessage = "Unable to load reports. Please try again later.",
                    ErrorType = "Service Unavailable"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshReports()
        {
            try
            {
                var reports = await _dbService.GetReportsAsync();
                return Json(new { success = true, message = "Table has been refreshed", data = reports });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing reports for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return Json(new { success = false, message = "Error refreshing reports. Please try again later." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportReports()
        {
            try
            {
                var reports = await _dbService.GetReportsAsync();
                var excelData = _excelService.ExportReportsToExcel(reports);
                
                var fileName = $"Reports_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                
                _logger.LogInformation("Reports exported by user {UserId}", User.Identity?.Name ?? "Anonymous");
                return File(excelData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting reports for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return Json(new { success = false, message = "Error exporting to Excel. Please try again later." });
            }
        }

        public async Task<IActionResult> DatabaseStatus()
        {
            ViewData["SubpageTitle"] = "Database Status";
            try
            {
                var statuses = await _dbService.GetDatabaseStatusAsync();
                return View(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading database status for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return View("ErrorPage", new ErrorViewModel 
                { 
                    ErrorMessage = "Unable to load database status. Please try again later.",
                    ErrorType = "Service Unavailable"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshDatabaseStatus()
        {
            try
            {
                var statuses = await _dbService.GetDatabaseStatusAsync();
                return Json(new { success = true, message = "Table has been refreshed", data = statuses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing database status for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return Json(new { success = false, message = "Error refreshing database status. Please try again later." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportDatabaseStatus()
        {
            try
            {
                var statuses = await _dbService.GetDatabaseStatusAsync();
                var excelData = _excelService.ExportDatabaseStatusToExcel(statuses);
                
                var fileName = $"DatabaseStatus_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                
                _logger.LogInformation("Database status exported by user {UserId}", User.Identity?.Name ?? "Anonymous");
                return File(excelData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting database status for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return Json(new { success = false, message = "Error exporting to Excel. Please try again later." });
            }
        }

        public IActionResult ReportIncident()
        {
            ViewData["SubpageTitle"] = "Report an incident / request a re-run";
            return View();
        }

        public async Task<IActionResult> RequestReportChange()
        {
            ViewData["SubpageTitle"] = "Request a report change";
            try
            {
                var users = await _dbService.GetMyCompanyUsersAsync();
                var companies = await _dbService.GetCompaniesAsync();
                
                var viewModel = new ChangeRequestViewModel
                {
                    Authorizers = users.Where(u => u.IsAuthorizer == "Y").ToList(),
                    Companies = companies
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report change form for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return View("ErrorPage", new ErrorViewModel 
                { 
                    ErrorMessage = "Unable to load form data. Please try again later.",
                    ErrorType = "Service Unavailable"
                });
            }
        }

        public async Task<IActionResult> RequestAccessChange()
        {
            ViewData["SubpageTitle"] = "Request an access change";
            try
            {
                var users = await _dbService.GetMyCompanyUsersAsync();
                var viewModel = new AccessRequestViewModel
                {
                    Users = users
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading access change form for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return View("ErrorPage", new ErrorViewModel 
                { 
                    ErrorMessage = "Unable to load user list. Please try again later.",
                    ErrorType = "Service Unavailable"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAccessRequest([FromBody] AccessRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request data." });
            }

            // Input validation
            if (!ValidateAccessRequestInput(model))
            {
                return Json(new { success = false, message = "Invalid input data provided." });
            }

            try
            {
                // Sanitize inputs
                var sanitizedModel = SanitizeAccessRequestModel(model);
                
                await _dbService.SubmitAccessRequestAsync(
                    sanitizedModel.RequestType,
                    sanitizedModel.YourName,
                    sanitizedModel.EmployeeName,
                    sanitizedModel.EmployeeId,
                    sanitizedModel.RequestDetails
                );
                
                _logger.LogInformation("Access request submitted by user {UserId} for employee {EmployeeName}", 
                    User.Identity?.Name ?? "Anonymous", _htmlEncoder.Encode(model.EmployeeName));
                
                return Json(new { success = true, message = "Request is submitted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting access request for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return Json(new { success = false, message = "Error submitting request. Please try again later." });
            }
        }

        public async Task<IActionResult> SubmittedAccessRequests()
        {
            ViewData["SubpageTitle"] = "Submitted access requests";
            try
            {
                var requests = await _dbService.GetAccessRequestsAsync(true);
                return View(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading access requests for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return View("ErrorPage", new ErrorViewModel 
                { 
                    ErrorMessage = "Unable to load access requests. Please try again later.",
                    ErrorType = "Service Unavailable"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshAccessRequests([FromBody] bool outstandingOnly)
        {
            try
            {
                var requests = await _dbService.GetAccessRequestsAsync(outstandingOnly);
                return Json(new { success = true, message = "Table has been refreshed", data = requests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing access requests for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return Json(new { success = false, message = "Error refreshing access requests. Please try again later." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportAccessRequests([FromBody] bool outstandingOnly)
        {
            try
            {
                var requests = await _dbService.GetAccessRequestsAsync(outstandingOnly);
                var excelData = _excelService.ExportAccessRequestsToExcel(requests);
                
                var fileName = $"AccessRequests_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                
                _logger.LogInformation("Access requests exported by user {UserId}", User.Identity?.Name ?? "Anonymous");
                return File(excelData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting access requests for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return Json(new { success = false, message = "Error exporting to Excel. Please try again later." });
            }
        }

        public async Task<IActionResult> SchedulesDashboard()
        {
            ViewData["SubpageTitle"] = "Schedules dashboard";
            try
            {
                var (schedules, holidays) = await _dbService.GetSchedulesAndHolidaysAsync();
                var clientRegions = await _dbService.GetClientRegionsAsync();
                
                foreach (var schedule in schedules)
                {
                    schedule.NextRunDate = CalculateNextRunDate(schedule, holidays, clientRegions);
                }
                
                var viewModel = new ScheduleDashboardViewModel
                {
                    Schedules = schedules,
                    TodayOnly = false,
                    Filter = "All"
                };
                
                UpdateSummary(viewModel);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading schedules dashboard for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return View("ErrorPage", new ErrorViewModel 
                { 
                    ErrorMessage = "Unable to load schedules. Please try again later.",
                    ErrorType = "Service Unavailable"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshSchedules([FromBody] ScheduleFilterRequest request)
        {
            if (!ModelState.IsValid || !ValidateScheduleFilter(request))
            {
                return Json(new { success = false, message = "Invalid filter parameters." });
            }

            try
            {
                var (schedules, holidays) = await _dbService.GetSchedulesAndHolidaysAsync();
                var clientRegions = await _dbService.GetClientRegionsAsync();
                
                foreach (var schedule in schedules)
                {
                    schedule.NextRunDate = CalculateNextRunDate(schedule, holidays, clientRegions);
                }
                
                if (request.TodayOnly)
                {
                    schedules = schedules.Where(s => s.NextRunDate?.Date == DateTime.Today).ToList();
                }
                
                if (request.Filter != "All")
                {
                    schedules = request.Filter switch
                    {
                        "Errors" => schedules.Where(s => s.LastRunState == "Error").ToList(),
                        "Running" => schedules.Where(s => s.LastRunState == "Running").ToList(),
                        "To Go" => schedules.Where(s => s.NextRunDate?.Date >= DateTime.Today && s.LastRunState != "Running").ToList(),
                        _ => schedules
                    };
                }
                
                var viewModel = new ScheduleDashboardViewModel
                {
                    Schedules = schedules,
                    TodayOnly = request.TodayOnly,
                    Filter = request.Filter
                };
                
                UpdateSummary(viewModel);
                
                return Json(new { success = true, message = "Table has been refreshed", data = viewModel });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing schedules for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return Json(new { success = false, message = "Error refreshing schedules. Please try again later." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportSchedules([FromBody] ScheduleFilterRequest request)
        {
            if (!ModelState.IsValid || !ValidateScheduleFilter(request))
            {
                return Json(new { success = false, message = "Invalid filter parameters." });
            }

            try
            {
                var (schedules, holidays) = await _dbService.GetSchedulesAndHolidaysAsync();
                var clientRegions = await _dbService.GetClientRegionsAsync();
                
                foreach (var schedule in schedules)
                {
                    schedule.NextRunDate = CalculateNextRunDate(schedule, holidays, clientRegions);
                }
                
                if (request.TodayOnly)
                {
                    schedules = schedules.Where(s => s.NextRunDate?.Date == DateTime.Today).ToList();
                }
                
                if (request.Filter != "All")
                {
                    schedules = request.Filter switch
                    {
                        "Errors" => schedules.Where(s => s.LastRunState == "Error").ToList(),
                        "Running" => schedules.Where(s => s.LastRunState == "Running").ToList(),
                        "To Go" => schedules.Where(s => s.NextRunDate?.Date >= DateTime.Today && s.LastRunState != "Running").ToList(),
                        _ => schedules
                    };
                }
                
                var excelData = _excelService.ExportSchedulesToExcel(schedules);
                
                var fileName = $"Schedules_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                
                _logger.LogInformation("Schedules exported by user {UserId} with filter {Filter}", 
                    User.Identity?.Name ?? "Anonymous", _htmlEncoder.Encode(request.Filter));
                
                return File(excelData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting schedules for user {UserId}", User.Identity?.Name ?? "Anonymous");
                return Json(new { success = false, message = "Error exporting to Excel. Please try again later." });
            }
        }

        public IActionResult Help()
        {
            ViewData["SubpageTitle"] = "Help";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private DateTime? CalculateNextRunDate(Schedule schedule, List<Holiday> holidays, Dictionary<string, string> clientRegions)
        {
            try
            {
                var region = clientRegions.ContainsKey(schedule.ClientDatabase) ? clientRegions[schedule.ClientDatabase] : "";
                var regionHolidays = holidays.Where(h => h.HolidayRegion == region).Select(h => h.HolidayDate).ToList();
                
                switch (schedule.Frequency)
                {
                    case "daily":
                        return schedule.LastRunDate.AddDays(1);
                        
                    case "NBD":
                        var nextDate = schedule.LastRunDate.AddDays(1);
                        while (nextDate.DayOfWeek == DayOfWeek.Saturday || 
                               nextDate.DayOfWeek == DayOfWeek.Sunday || 
                               regionHolidays.Contains(nextDate.Date))
                        {
                            nextDate = nextDate.AddDays(1);
                        }
                        return nextDate;
                        
                    case "Monthly":
                        if (int.TryParse(schedule.DayOrDate, out int day))
                        {
                            var nextMonth = schedule.LastRunDate.AddMonths(1);
                            var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                            day = Math.Min(day, daysInMonth);
                            return new DateTime(nextMonth.Year, nextMonth.Month, day);
                        }
                        return null;
                        
                    case "FBD":
                        var firstDay = new DateTime(schedule.LastRunDate.AddMonths(1).Year, schedule.LastRunDate.AddMonths(1).Month, 1);
                        while (firstDay.DayOfWeek == DayOfWeek.Saturday || 
                               firstDay.DayOfWeek == DayOfWeek.Sunday || 
                               regionHolidays.Contains(firstDay.Date))
                        {
                            firstDay = firstDay.AddDays(1);
                        }
                        return firstDay;
                        
                    case "Weekly":
                        if (Enum.TryParse<DayOfWeek>(schedule.DayOrDate, true, out var targetDay))
                        {
                            var next = schedule.LastRunDate.AddDays(1);
                            while (next.DayOfWeek != targetDay)
                            {
                                next = next.AddDays(1);
                            }
                            return next;
                        }
                        return null;
                        
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating next run date for schedule {ScheduleId}", schedule.Id);
                return null;
            }
        }

        private void UpdateSummary(ScheduleDashboardViewModel viewModel)
        {
            var todaySchedules = viewModel.Schedules.Where(s => s.NextRunDate?.Date == DateTime.Today).ToList();
            viewModel.DoneCount = todaySchedules.Count(s => s.LastRunState == "Success" && s.LastRunDate.Date == DateTime.Today);
            viewModel.ToGoCount = todaySchedules.Count(s => s.LastRunState != "Running" && (s.LastRunDate.Date < DateTime.Today || s.LastRunState != "Success"));
            viewModel.ErrorCount = todaySchedules.Count(s => s.LastRunState == "Error");
        }

        private bool ValidateAccessRequestInput(AccessRequestViewModel model)
        {
            if (model == null) return false;
            
            // Validate request type
            if (model.RequestType != "A" && model.RequestType != "R") return false;
            
            // Validate name fields - allow only letters, spaces, hyphens, and apostrophes
            var namePattern = @"^[a-zA-Z\s\-']+$";
            if (!string.IsNullOrEmpty(model.YourName) && !Regex.IsMatch(model.YourName, namePattern)) return false;
            if (!string.IsNullOrEmpty(model.EmployeeName) && !Regex.IsMatch(model.EmployeeName, namePattern)) return false;
            
            // Validate employee ID - allow alphanumeric and common separators
            var idPattern = @"^[a-zA-Z0-9\-_\.]+$";
            if (!string.IsNullOrEmpty(model.EmployeeId) && !Regex.IsMatch(model.EmployeeId, idPattern)) return false;
            
            // Length validation
            if (!string.IsNullOrEmpty(model.YourName) && model.YourName.Length > 100) return false;
            if (!string.IsNullOrEmpty(model.EmployeeName) && model.EmployeeName.Length > 100) return false;
            if (!string.IsNullOrEmpty(model.EmployeeId) && model.EmployeeId.Length > 50) return false;
            if (!string.IsNullOrEmpty(model.RequestDetails) && model.RequestDetails.Length > 1000) return false;
            
            return true;
        }

        private AccessRequestViewModel SanitizeAccessRequestModel(AccessRequestViewModel model)
        {
            return new AccessRequestViewModel
            {
                RequestType = _htmlEncoder.Encode(model.RequestType ?? ""),
                YourName = _htmlEncoder.Encode(model.YourName ?? ""),
                EmployeeName = _htmlEncoder.Encode(model.EmployeeName ?? ""),
                EmployeeId = _htmlEncoder.Encode(model.EmployeeId ?? ""),
                RequestDetails = _htmlEncoder.Encode(model.RequestDetails ?? ""),
                Users = model.Users // Don't sanitize the users list as it comes from the database
            };
        }

        private bool ValidateScheduleFilter(ScheduleFilterRequest request)
        {
            if (request == null) return false;
            
            var validFilters = new[] { "All", "Errors", "Running", "To Go" };
            return validFilters.Contains(request.Filter);
        }
    }

    // Helper model for schedule filtering
    public class ScheduleFilterRequest
    {
        public bool TodayOnly { get; set; }
        
        [Required]
        [RegularExpression("^(All|Errors|Running|To Go)$")]
        public string Filter { get; set; } = "All";
    }
}