using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HRMS_System.Pages.Hrms.DailyLogs.DailyLogsUpload
{
    // [Authorize(Roles = "Supervisor")] // adjust to your roles system
    public class DailyLogsUploadPageModel : PageModel
    {
        // ===== Upload Inputs =====
        [BindProperty, Required(ErrorMessage = "Please choose an Excel file.")]
        public IFormFile? UploadFile { get; set; }

        [BindProperty] public DateTime? DateFrom { get; set; }
        [BindProperty] public DateTime? DateTo { get; set; }
        [BindProperty] public int? DepartmentId { get; set; }

        [BindProperty] public string ImportMode { get; set; } = "ValidateOnly";
        [BindProperty] public bool OverwriteDuplicates { get; set; } = false;

        // ===== UI Data =====
        public List<SelectListItem> DepartmentOptions { get; set; } = new();
        public List<PreviewRow> PreviewRows { get; set; } = new();
        public List<string> Errors { get; set; } = new();

        public int SummaryParsed { get; set; }
        public int SummaryValid { get; set; }
        public int SummaryImported { get; set; }
        public int SummaryErrors { get; set; }

        public List<UploadHistoryRow> History { get; set; } = new();

        // TODO: inject your DbContext via constructor
        // private readonly AppDbContext _db;
        // public DailyLogsUploadPageModel(AppDbContext db) => _db = db;

        public void OnGet()
        {
            LoadDepartments();
            LoadHistory();
        }

        public IActionResult OnPostParse()
        {
            LoadDepartments();
            LoadHistory();

            if (!ModelState.IsValid || UploadFile == null)
                return Page();

            if (!UploadFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(UploadFile), "Only .xlsx files are allowed.");
                return Page();
            }

            // 1) Parse Excel
            //    - read rows
            //    - map to PreviewRow
            // 2) Apply filters (DateFrom/DateTo/DepartmentId)
            // 3) Validate rows

            // Example placeholders:
            var parsed = new List<PreviewRow>(); // result from excel parser

            // TODO: Replace with real parsing
            // parsed = ExcelParser.ParseDailyLogs(UploadFile.OpenReadStream());

            // Validate example:
            foreach (var r in parsed)
            {
                // TODO: employee lookup, date checks, time checks, duplicates checks
                // if error: Errors.Add("Row X: ...");
            }

            SummaryParsed = parsed.Count;
            SummaryErrors = Errors.Count;
            SummaryValid = Math.Max(0, SummaryParsed - SummaryErrors);

            PreviewRows = parsed.Take(200).ToList(); // keep it lightweight

            // Store preview in TempData/Session if you want ConfirmImport to reuse it safely
            // TempData["PreviewJson"] = JsonSerializer.Serialize(PreviewRows);

            return Page();
        }

        public IActionResult OnPostConfirmImport()
        {
            LoadDepartments();
            LoadHistory();

            // In real implementation, retrieve parsed preview from TempData/Session
            // if (TempData["PreviewJson"] == null) { Errors.Add("No preview found."); return Page(); }

            if (ImportMode == "ValidateOnly")
            {
                Errors.Add("Import Mode is set to Validate Only. Change it to Import to save records.");
                SummaryErrors = Errors.Count;
                return Page();
            }

            // TODO:
            // 1) Loop preview rows
            // 2) Insert/update attendance records
            // 3) Respect OverwriteDuplicates
            // 4) Save changes
            // 5) Write UploadHistory record

            SummaryImported = SummaryValid; // placeholder

            return RedirectToPage(); // reload clean after import
        }

        private void LoadDepartments()
        {
            // TODO: load from db
            DepartmentOptions = new List<SelectListItem>
            {
                new("All Departments", ""),
                new("HR", "1"),
                new("IT", "2"),
                new("Operations", "3")
            };
        }

        private void LoadHistory()
        {
            // TODO: load from db (latest 10)
            History = new List<UploadHistoryRow>();
        }

        // ===== View Models =====
        public class PreviewRow
        {
            public string EmployeeIdNumber { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public DateTime AttendanceDate { get; set; }

            public TimeSpan? TimeIn { get; set; }
            public TimeSpan? TimeOut { get; set; }

            public string Status { get; set; } = "On-Time";
            public string StatusClass { get; set; } = "pill-ok";
            public string Note { get; set; } = "";
        }

        public class UploadHistoryRow
        {
            public DateTime UploadedAt { get; set; }
            public string FileName { get; set; } = "";
            public string UploadedBy { get; set; } = "";
            public int ParsedCount { get; set; }
            public int ImportedCount { get; set; }
            public int ErrorCount { get; set; }
            public bool IsSuccess { get; set; }
        }
    }
}