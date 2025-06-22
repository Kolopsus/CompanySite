using Microsoft.AspNetCore.Mvc;
using CompanySite.Models;
using CompanySite.Services;
using System.Diagnostics;

namespace CompanySite.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseService _dbService;
        private readonly ExcelService _excelService;

        public HomeController(DatabaseService dbService, ExcelService excelService)
        {
            _dbService = dbService;
            _excelService = excelService;
        }

        public IActionResult Index()
        {
            ViewData["SubpageTitle"] = "Home";
            return View();
        }

        public async Task<IActionResult> AvailableReports()
        {
            ViewData["SubpageTitle"] = "Available Reports";
            var reports = await _dbService.GetReportsAsync();
            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshReports()
        {
            var reports = await _dbService.GetReportsAsync();
            return Json(new { success = true, message = "Table has been refreshed", data = reports });
        }

        [HttpPost]
        public async Task<IActionResult> ExportReports()
        {
            var reports = await _dbService.GetReportsAsync();
            var excelData = _excelService.ExportReportsToExcel(reports);
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reports_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> DatabaseStatus()
        {
            ViewData["SubpageTitle"] = "Database Status";
            var statuses = await _dbService.GetDatabaseStatusAsync();
            return View(statuses);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshDatabaseStatus()
        {
            var statuses = await _dbService.GetDatabaseStatusAsync();
            return Json(new { success = true, message = "Table has been refreshed", data = statuses });
        }

        [HttpPost]
        public async Task<IActionResult> ExportDatabaseStatus()
        {
            var statuses = await _dbService.GetDatabaseStatusAsync();
            var excelData = _excelService.ExportDatabaseStatusToExcel(statuses);
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DatabaseStatus_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public IActionResult ReportIncident()
        {
            ViewData["SubpageTitle"] = "Report an incident / request a re-run";
            return View();
        }

        public async Task<IActionResult> RequestReportChange()
        {
            ViewData["SubpageTitle"] = "Request a report change";
            var users = await _dbService.GetMyCompanyUsersAsync();
            var companies = await _dbService.GetCompaniesAsync();
            
            var viewModel = new ChangeRequestViewModel
            {
                Authorizers = users.Where(u => u.IsAuthorizer == "Y").ToList(),
                Companies = companies
            };
            
            return View(viewModel);
        }

        public async Task<IActionResult> RequestAccessChange()
        {
            ViewData["SubpageTitle"] = "Request an access change";
            var users = await _dbService.GetMyCompanyUsersAsync();
            var viewModel = new AccessRequestViewModel
            {
                Users = users
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAccessRequest(AccessRequestViewModel model)
        {
            try
            {
                await _dbService.SubmitAccessRequestAsync(
                    model.RequestType,
                    model.YourName,
                    model.EmployeeName,
                    model.EmployeeId,
                    model.RequestDetails
                );
                return Json(new { success = true, message = "Request is submitted" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error submitting request: " + ex.Message });
            }
        }

        public async Task<IActionResult> SubmittedAccessRequests()
        {
            ViewData["SubpageTitle"] = "Submitted access requests";
            var requests = await _dbService.GetAccessRequestsAsync(true);
            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshAccessRequests(bool outstandingOnly)
        {
            var requests = await _dbService.GetAccessRequestsAsync(outstandingOnly);
            return Json(new { success = true, message = "Table has been refreshed", data = requests });
        }

        [HttpPost]
        public async Task<IActionResult> ExportAccessRequests(bool outstandingOnly)
        {
            var requests = await _dbService.GetAccessRequestsAsync(outstandingOnly);
            var excelData = _excelService.ExportAccessRequestsToExcel(requests);
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AccessRequests_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> SchedulesDashboard()
        {
            ViewData["SubpageTitle"] = "Schedules dashboard";
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

        [HttpPost]
        public async Task<IActionResult> RefreshSchedules(bool todayOnly, string filter)
        {
            var (schedules, holidays) = await _dbService.GetSchedulesAndHolidaysAsync();
            var clientRegions = await _dbService.GetClientRegionsAsync();
            
            foreach (var schedule in schedules)
            {
                schedule.NextRunDate = CalculateNextRunDate(schedule, holidays, clientRegions);
            }
            
            if (todayOnly)
            {
                schedules = schedules.Where(s => s.NextRunDate?.Date == DateTime.Today).ToList();
            }
            
            if (filter != "All")
            {
                schedules = filter switch
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
                TodayOnly = todayOnly,
                Filter = filter
            };
            
            UpdateSummary(viewModel);
            
            return Json(new { success = true, message = "Table has been refreshed", data = viewModel });
        }

        [HttpPost]
        public async Task<IActionResult> ExportSchedules(bool todayOnly, string filter)
        {
            var (schedules, holidays) = await _dbService.GetSchedulesAndHolidaysAsync();
            var clientRegions = await _dbService.GetClientRegionsAsync();
            
            foreach (var schedule in schedules)
            {
                schedule.NextRunDate = CalculateNextRunDate(schedule, holidays, clientRegions);
            }
            
            if (todayOnly)
            {
                schedules = schedules.Where(s => s.NextRunDate?.Date == DateTime.Today).ToList();
            }
            
            if (filter != "All")
            {
                schedules = filter switch
                {
                    "Errors" => schedules.Where(s => s.LastRunState == "Error").ToList(),
                    "Running" => schedules.Where(s => s.LastRunState == "Running").ToList(),
                    "To Go" => schedules.Where(s => s.NextRunDate?.Date >= DateTime.Today && s.LastRunState != "Running").ToList(),
                    _ => schedules
                };
            }
            
            var excelData = _excelService.ExportSchedulesToExcel(schedules);
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Schedules_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
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

        private void UpdateSummary(ScheduleDashboardViewModel viewModel)
        {
            var todaySchedules = viewModel.Schedules.Where(s => s.NextRunDate?.Date == DateTime.Today).ToList();
            viewModel.DoneCount = todaySchedules.Count(s => s.LastRunState == "Success" && s.LastRunDate.Date == DateTime.Today);
            viewModel.ToGoCount = todaySchedules.Count(s => s.LastRunState != "Running" && (s.LastRunDate.Date < DateTime.Today || s.LastRunState != "Success"));
            viewModel.ErrorCount = todaySchedules.Count(s => s.LastRunState == "Error");
        }
    }
}