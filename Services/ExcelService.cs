using ClosedXML.Excel;
using CompanySite.Models;
using System.IO;
using Microsoft.Extensions.Logging;

namespace CompanySite.Services
{
    public class ExcelService
    {
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(ILogger<ExcelService> logger)
        {
            _logger = logger;
        }

        public byte[] ExportReportsToExcel(List<Report> reports)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Reports");
                
                // Add headers
                worksheet.Cell(1, 1).Value = "Title";
                worksheet.Cell(1, 2).Value = "Reference";
                worksheet.Cell(1, 3).Value = "Description";
                worksheet.Cell(1, 4).Value = "Report Exe";
                worksheet.Cell(1, 5).Value = "Updated";
                
                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 5);
                headerRange.Style.Fill.BackgroundColor = XLColor.Blue;
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Font.Bold = true;
                
                // Add data
                int row = 2;
                foreach (var report in reports)
                {
                    worksheet.Cell(row, 1).Value = report.Title ?? "";
                    worksheet.Cell(row, 2).Value = report.Reference ?? "";
                    worksheet.Cell(row, 3).Value = report.Description ?? "";
                    worksheet.Cell(row, 4).Value = report.ReportExe ?? "";
                    worksheet.Cell(row, 5).Value = report.Updated.ToString("yyyy-MM-dd HH:mm:ss");
                    
                    // Alternate row colors
                    if (row % 2 == 0)
                    {
                        worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                    row++;
                }
                
                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
                
                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Excel file for reports");
                throw new InvalidOperationException("Failed to generate Excel file. Please try again.", ex);
            }
        }
        
        public byte[] ExportDatabaseStatusToExcel(List<DatabaseStatus> statuses)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Database Status");
                
                // Add headers
                worksheet.Cell(1, 1).Value = "Company";
                worksheet.Cell(1, 2).Value = "Region";
                worksheet.Cell(1, 3).Value = "Database";
                worksheet.Cell(1, 4).Value = "Refresh Date";
                worksheet.Cell(1, 5).Value = "Expected Refresh Date";
                worksheet.Cell(1, 6).Value = "Status";
                
                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 6);
                headerRange.Style.Fill.BackgroundColor = XLColor.Green;
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Font.Bold = true;
                
                // Add data
                int row = 2;
                foreach (var status in statuses)
                {
                    worksheet.Cell(row, 1).Value = status.Company ?? "";
                    worksheet.Cell(row, 2).Value = status.Region ?? "";
                    worksheet.Cell(row, 3).Value = status.Database ?? "";
                    worksheet.Cell(row, 4).Value = status.RefreshDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                    worksheet.Cell(row, 5).Value = status.ExpectedRefreshDate.ToString("yyyy-MM-dd");
                    worksheet.Cell(row, 6).Value = status.Status ?? "";
                    
                    // Alternate row colors
                    if (row % 2 == 0)
                    {
                        worksheet.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                    row++;
                }
                
                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
                
                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Excel file for database status");
                throw new InvalidOperationException("Failed to generate Excel file. Please try again.", ex);
            }
        }
        
        public byte[] ExportAccessRequestsToExcel(List<UserAccessRequest> requests)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Access Requests");
                
                // Add headers
                worksheet.Cell(1, 1).Value = "Ref";
                worksheet.Cell(1, 2).Value = "Status";
                worksheet.Cell(1, 3).Value = "Category";
                worksheet.Cell(1, 4).Value = "Requestor Name";
                worksheet.Cell(1, 5).Value = "Date of Request";
                worksheet.Cell(1, 6).Value = "Details";
                
                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 6);
                headerRange.Style.Fill.BackgroundColor = XLColor.Orange;
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Font.Bold = true;
                
                // Add data
                int row = 2;
                foreach (var request in requests)
                {
                    worksheet.Cell(row, 1).Value = request.Id;
                    worksheet.Cell(row, 2).Value = request.Status ?? "";
                    worksheet.Cell(row, 3).Value = request.RequestType == "A" ? "Add access request" : "Remove access request";
                    worksheet.Cell(row, 4).Value = request.RequestorName ?? "";
                    worksheet.Cell(row, 5).Value = request.CreateDate.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 6).Value = $"{request.UserName ?? ""} - {request.RequestDetails ?? ""}";
                    
                    // Alternate row colors
                    if (row % 2 == 0)
                    {
                        worksheet.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                    row++;
                }
                
                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
                
                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Excel file for access requests");
                throw new InvalidOperationException("Failed to generate Excel file. Please try again.", ex);
            }
        }
        
        public byte[] ExportSchedulesToExcel(List<Schedule> schedules)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Schedules");
                
                // Add headers
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 2).Value = "Machine";
                worksheet.Cell(1, 3).Value = "Last Run Date";
                worksheet.Cell(1, 4).Value = "Next Run Date";
                worksheet.Cell(1, 5).Value = "Last Run Status";
                worksheet.Cell(1, 6).Value = "Report";
                worksheet.Cell(1, 7).Value = "DB";
                worksheet.Cell(1, 8).Value = "Server";
                worksheet.Cell(1, 9).Value = "Frequency";
                worksheet.Cell(1, 10).Value = "Day or Date";
                worksheet.Cell(1, 11).Value = "Output Directory";
                
                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 11);
                headerRange.Style.Fill.BackgroundColor = XLColor.Purple;
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Font.Bold = true;
                
                // Add data
                int row = 2;
                foreach (var schedule in schedules)
                {
                    worksheet.Cell(row, 1).Value = schedule.Id;
                    worksheet.Cell(row, 2).Value = schedule.Server ?? "";
                    worksheet.Cell(row, 3).Value = schedule.LastRunDate.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 4).Value = schedule.NextRunDate?.ToString("yyyy-MM-dd") ?? "N/A";
                    worksheet.Cell(row, 5).Value = schedule.LastRunState ?? "";
                    worksheet.Cell(row, 6).Value = schedule.ReportName ?? "";
                    worksheet.Cell(row, 7).Value = schedule.ClientDatabase ?? "";
                    worksheet.Cell(row, 8).Value = schedule.ClientServer ?? "";
                    worksheet.Cell(row, 9).Value = schedule.Frequency ?? "";
                    worksheet.Cell(row, 10).Value = schedule.DayOrDate ?? "";
                    worksheet.Cell(row, 11).Value = schedule.OutputDirectory ?? "";
                    
                    // Alternate row colors
                    if (row % 2 == 0)
                    {
                        worksheet.Range(row, 1, row, 11).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                    row++;
                }
                
                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
                
                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Excel file for schedules");
                throw new InvalidOperationException("Failed to generate Excel file. Please try again.", ex);
            }
        }
    }
}